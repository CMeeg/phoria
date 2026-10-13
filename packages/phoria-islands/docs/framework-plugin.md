# Adding a Phoria framework package

An island is written in a framework, and the server has to render it in that same framework. A framework package is what bridges the two: a Vite plugin that finds your components, and a pair of services that render them and mount them in the browser. Each one is a thin composer over a single shared factory, so writing one is mostly configuration plus two small service modules — not a new Vite plugin.

The example this document walks through is `packages/phoria-react`, because it exercises every part of the shape. It is an example, not a template. Other adapters fill the same slots and make the same decisions differently; the ones they made are collected in [worked examples](#worked-examples-the-decisions) so you can see which decisions are forced and which are free before you commit to either.

The work divides into three decisions and one wiring step. The decisions are which modules are pre-bundled, which are externalised from the `ssr` environment, and how your framework mounts a component. The wiring is making four source files become four exported entries, which is the part that is easy to miss and produces a package that builds but cannot be imported.

The six steps below are the whole job. Everything under [Reference](#reference) is there to explain a decision you have already made, and you do not need it to get through the steps.

## 1. The four source files

A framework package exposes four entries, one per purpose. Each has one source file, and nothing about one entry affects another:

| Entry | Source | Contents |
| --- | --- | --- |
| `.` | `src/main.ts` | the framework name, exported |
| `./client` | `src/client/main.ts` | `registerCsrService(framework.name, service)` |
| `./server` | `src/server/main.ts` | `registerSsrService(framework.name, service)`, plus re-exports of the render helpers and an island type guard |
| `./vite` | `src/vite/plugin.ts` | the framework Vite plugin |

`src/main.ts` is the smallest of the four, and it is the whole contract of the `.` entry:

```ts
const framework = {
	name: "react"
} as const

export { framework }
```

The service entries import it through the package's `~` path alias, as `~/main`. That alias is why step 2 exists.

## 2. The wiring

Four source files are not four entries. Each entry is three things at once — a build target, a package export, and a path that resolves to it — and all three have to name it. This is the step most likely to be missed, because a package that gets it wrong still builds.

Work from `packages/phoria-react/vite.config.ts`, `tsconfig.json` and `package.json` and substitute your package name.

**Build targets**, in `vite.config.ts`. The keys are internal names, and `main` is the one that becomes `.`:

```ts
build: {
	lib: {
		entry: {
			main: "src/main.ts",
			client: "src/client/main.ts",
			server: "src/server/main.ts",
			vite: "src/vite/plugin.ts"
		},
		name: "phoria-react"
	}
}
```

**The path alias**, in `tsconfig.json` — and Vite has to be told to honour it, or it will not know what `~/main` means:

```jsonc
// tsconfig.json
{
	"compilerOptions": {
		"paths": {
			"~/*": ["./src/*"]
		}
	}
}
```

```ts
// vite.config.ts
export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	// ...
})
```

Without `resolve.tsconfigPaths` there is nothing for Vite to resolve `~/main` against, so the build stops on an unresolvable import rather than producing a package that looks fine.

**Exports**, in `package.json`. Each entry needs `types`, `import` and `require`. The `./client` and `./server` entries are the ones to watch: their types come from `dist/<name>/main.d.ts` while their code comes from `dist/<name>.js`, so the two paths do not share a stem, and an export map that assumes they do type-checks cleanly and fails at the first import.

```jsonc
"exports": {
	".":        { "types": "./dist/main.d.ts",       "import": "./dist/main.js",   "require": "./dist/main.cjs" },
	"./client": { "types": "./dist/client/main.d.ts", "import": "./dist/client.js", "require": "./dist/client.cjs" },
	"./server": { "types": "./dist/server/main.d.ts", "import": "./dist/server.js", "require": "./dist/server.cjs" },
	"./vite":   { "types": "./dist/vite/plugin.d.ts", "import": "./dist/vite.js",   "require": "./dist/vite.cjs" }
}
```

## 3. The Vite plugin

`src/vite/plugin.ts` wraps the framework's own Vite plugin, composes the shared factory, and returns both. Read `packages/phoria-react/src/vite/plugin.ts` alongside this — it is about forty lines and every line maps to something below.

The option type is where the package's opinion lives. The factory has seven options; you expose four and keep three, enforced by the type rather than by convention:

```ts
interface PhoriaReactPluginOptions extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	react?: ReactOptions | false
}
```

The factory call is the one thing to get exactly right. It takes all seven values, and the framework's own plugin is composed in alongside it rather than merged into it:

```ts
plugins.push(
	createPhoriaFrameworkPlugin({
		name: pluginName,
		include: opts.include,
		exclude: opts.exclude,
		cwd: opts.cwd,
		workspacePackages: opts.workspacePackages,
		optimizeDeps: ["react", "react-dom/client"],
		ssrExternal: ["@phoria/phoria-react/server"]
	})
)
```

That is an excerpt, not the whole function: the line above it builds `plugins` from the framework's own plugin, `opts` is `{ ...defaultOptions, ...options }`, and `return plugins` follows. The shape to copy is that the four caller fields come from the merged options, so an application can override them, while the three package-set fields are the package's own opinion and cannot be overridden at all.

Five decisions the factory cannot make for you:

1. **`false` is the opt-out, and it is per-plugin.** `options?.react !== false` decides whether the framework's own Vite plugin is included at all; `options?.react` then forwards that plugin's own options. Every adapter does this, and it is what lets an application that already configures the plugin itself avoid a double transform.
2. **Spread the framework plugin's return value, matching its shape.** Some plugins return an array and can be spread (`[...react(...)]`); others return a single plugin and have to be wrapped (`[vue(...)]`). Copy whichever matches the plugin you are adding — `PluginOption` accepts both, and the return shape is not something the factory can normalise for you.
3. **How much of the framework plugin's option type you re-expose is your call, and adapters differ.** The React adapter narrows it to `Pick<ViteReactPluginOptions, "include" | "exclude">` and exports that as `ReactOptions`; others pass their Vite plugin's whole options type through unchanged. Narrowing is the safer default, because a framework plugin option that silently does not reach the plugin is hard to diagnose.
4. **`optimizeDeps` names the runtime entries the framework needs pre-bundled.** The React adapter, for example, uses `["react", "react-dom/client"]`.
5. **`ssrExternal` names the framework's own server entry — and sometimes the framework itself.** Every adapter externalises `@phoria/phoria-<framework>/server`. Svelte's also externalises `svelte`, so the Vite-transformed component and the renderer share one `svelte/internal/server` instance; a second copy means two copies of module-level `ssr_context`, and `push_element` crashes reading `null`. That is a Svelte-specific hazard, not a general rule, but it is the reason to read your framework's SSR path before fixing this list.

## 4. The two services

**`./server`** exports a single `render` function, typed `PhoriaIslandComponentSsrService<F, T>`, in `src/server/ssr.tsx`. Read `packages/phoria-react/src/server/ssr.tsx` — it is short, and the render helpers it delegates to are in a second file beside it.

Four obligations, in order:

- **Reject** a component registered for another framework, by comparing `component.framework` against `framework.name`.
- **Import** it via `importComponent`, which resolves the loader and the component for you.
- **Render** it by delegating to a render helper, which receives the island and the props.
- **Return** `{ framework, componentPath, html }`, where `html` is a string or a `ReadableStream`.

`componentPath` is what the preload chain depends on — returning the island's own `componentPath` unchanged is what makes preload links resolve. Each framework's render helpers are its own file, `src/server/ssr.ts` (`.tsx` for React, `.ts` for the others), with the type `RenderPhoriaIslandComponent<F, C, P>` returning `string | Promise<string | ReadableStream>`. A framework that can only produce a string simply has one helper and makes it the default.

**`./client`** exports a single `mount` function, typed `PhoriaIslandComponentCsrService<F, T>`, in `src/client/csr.tsx`. Two rules:

- **Import the framework's runtime dynamically** (`await import("react")`), so a server-rendered-only island does not pull the framework into the client's first load.
- **Default to hydrate, not mount** — `options?.mode ?? csrMountMode.hydrate` — because the usual case is markup that is already in the document. `csrMountMode` has exactly two values, `render` and `hydrate`.

One thing to know before you rely on it: every adapter does the dynamic imports and the mount inside a `Promise.all(...).then(...)` that `mount` does not await. `mount` is typed `Promise<void>`, so nothing breaks, but awaiting it does not mean the island is hydrated. Call it without awaiting and nothing fails visibly.

A framework with no hydrate/render distinction can ignore `options` entirely — the Vue adapter's `mount` takes no `options` parameter and mounts unconditionally.

## 5. Registering the adapter

Both entries must import their framework's service, and **the import order is load-bearing**. `registerComponent` looks up the framework at registration time and throws if it is not there yet:

```
Cannot register component "ReactCounter" because the "react" framework has not been registered.
```

So the framework services come first, then the register, then the call. From `examples/framework-multiple/WebApp/ui/src/entry-client.ts`:

```ts
import "@phoria/phoria-react/client"
import "@phoria/phoria-svelte/client"
import "@phoria/phoria-vue/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"
import "~/styles/global.css"

PhoriaIsland.register()
```

`entry-server.ts` beside it is the same shape against the `/server` entries, followed by the application's own `renderPhoriaIsland` function.

A component's `loader` is one of two shapes, and the shape has to match how the component is exported. The function form imports the module and takes its **default export**; the object form names the export explicitly, and is required whenever the component is a named export. `Counter` is a named export and the other two are default exports, which is why the register below uses the object form for one and the function form for the others:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  ReactCounter: {
    loader: {
      module: () => import("./counter/counter.tsx"),
      component: (module) => module.Counter,
    },
    framework: "react",
  },
  VueCounter: {
    loader: () => import("./counter/counter-button.vue"),
    framework: "vue",
  },
  SvelteCounter: {
    loader: () => import("./counter/counter.svelte"),
    framework: "svelte",
  },
})
```

Framework names are matched case-insensitively — `registerComponent` lowercases both the component name and the framework — so `framework: "react"` and `"React"` are the same key.

## 6. Proving it works

Add an example under `examples/` that renders its islands when run with Docker, following the same steps as the getting-started example. This is the only step that tells you the other five worked: every one of them can be wrong in a way that only shows up as a blank island.

## Checklist

One item per obligation, in the order you would meet them.

**1. The four source files**

- [ ] `src/main.ts` exports a `framework` constant, and the two service entries import it as `~/main`

**2. The wiring**

- [ ] `tsconfig.json` maps `~/*` to `./src/*` and `vite.config.ts` sets `resolve.tsconfigPaths`
- [ ] `vite.config.ts` declares all four `build.lib.entry` targets, with `main` becoming `.`
- [ ] `package.json` exports all four entries, with `types` resolving for `./client` and `./server`

**3. The Vite plugin**

- [ ] The option type omits `name`, `optimizeDeps` and `ssrExternal`, and re-adds only the framework plugin's own options with a `false` opt-out
- [ ] `createPhoriaFrameworkPlugin` configured with all seven values, `include` and `exclude` defaulted to the framework's own component extensions
- [ ] Framework's own Vite plugin wrapped, spread or not to match the array or single plugin it returns, as `PluginOption` allows either
- [ ] `optimizeDeps` includes the framework runtime
- [ ] `ssrExternal` includes the framework's own server entry, plus the framework itself if its SSR path holds module-level state
- [ ] `workspacePackages` settable, defaulting to `[]`

**4. The two services**

- [ ] `registerCsrService` and `registerSsrService` called with `framework.name`
- [ ] `render` rejects foreign frameworks, imports via `importComponent`, honours `options.renderComponent`, and returns `componentPath` unchanged
- [ ] `mount` imports the runtime dynamically and defaults to hydrate

**5–6. Registering, and proving it**

- [ ] Both entries import the framework's service before the register, and the register before the `PhoriaIsland.register()` call
- [ ] An example added under `examples/` that renders its islands when run with Docker, following the same steps as the getting-started example

## Reference

Everything here explains a decision the steps above already forced you to make.

### The factory's seven options

`createPhoriaFrameworkPlugin`, its `PhoriaFrameworkPluginOptions` type, and `resolvePackageDir` are exported from `@phoria/phoria/vite`. The factory owns the `__phoriaComponentPath` transform, the `applyToEnvironment` guard limiting that transform to the `client` and `ssr` environments, the `ssr` environment registration, and the merge of your `optimizeDeps` and `ssrExternal` entries. It returns a plain Vite `Plugin`.

| Option | Who sets it | What it is |
| --- | --- | --- |
| `name` | package | the plugin name; use the framework package's name, e.g. `phoria-react` |
| `include` | **caller** | a `createFilter` include pattern — glob, `RegExp`, or an array of either; defaults to the framework's own component extensions, e.g. `["**/*.jsx", "**/*.tsx"]` |
| `exclude` | **caller** | a `createFilter` exclude pattern; defaults to `"node_modules/**"` |
| `cwd` | **caller** | the directory `workspacePackages` names are resolved from, and the initial root the factory assumes before Vite reports the real one; defaults to `process.cwd()` |
| `workspacePackages` | **caller** | workspace packages whose modules are application-owned and must be transformed like your own; defaults to `[]` |
| `optimizeDeps` | package | pre-bundled runtime entries, e.g. `["react", "react-dom/client"]` |
| `ssrExternal` | package | modules externalised from the `ssr` environment |

The four caller fields have no defaults of their own — the factory takes them exactly as given, and it is each framework package that supplies the `include`, `exclude`, `cwd` and `workspacePackages` defaults in its own `defaultOptions`.

The two package-set fields are properties of the framework's own module layout, which is why they sit on the package side. An `optimizeDeps` entry is pre-bundled into the environment's dependency cache, so what the `ssr` environment imports is that pre-bundled build rather than the framework's source. An `ssrExternal` entry is handed to Node's own loader instead of being part of the module graph Vite builds. Neither is something an application can get right for a framework it does not own, and each is the difference between one module instance and two.

### The five hooks

The returned plugin is a `name` property plus five hooks. `name` is not a hook, and is set from `options.name`:

| Member | What it does |
| --- | --- |
| `config` | creates the `ssr` environment if absent, and merges your `optimizeDeps` into `optimizeDeps.include`, de-duplicated |
| `configEnvironment` | for the `ssr` environment only, merges your `ssrExternal` into `resolve.external` |
| `configResolved` | records Vite's resolved `root`, replacing the one `cwd` seeded, and it is that root component paths are computed relative to |
| `applyToEnvironment` | limits the plugin to the `client` and `ssr` environments |
| `transform` | appends the `__phoriaComponentPath` export to each in-scope module |

`configEnvironment` merges into whatever `resolve.external` already is, and handles every shape Vite accepts: an array, string or `RegExp` is extended and de-duplicated, a function is wrapped so both run, `undefined` is simply your entries, `false` becomes a function that matches only your entries, and `true` is left alone. Any other value throws `Unsupported resolve.external value: …` at config time, so a project that has set it to something exotic fails there rather than quietly losing your entries.

`transform` skips CSS requests, and independently skips any module resolving under `node_modules`. That second rule is what `workspacePackages` exists to work around: a workspace package is symlinked into `node_modules`, so by path alone it is indistinguishable from a registry dependency — a component imported from one would be skipped, its island would render blank, and nothing would say why. The names are resolved eagerly, when the plugin is created, by walking up from `cwd` looking for `node_modules/<package>`. A name that does not resolve throws `Unable to resolve workspace package "<name>" from cwd "<cwd>".` at config time — before Vite has started — so a typo fails fast and names itself. Leave `workspacePackages` empty for a single-package app.

### The loader, and when the object form is required

A component's `loader` in the register is one of two shapes, and the shape you pick has to match how the component is exported:

```ts
type PhoriaIslandComponentLoader<M, T> =
	| PhoriaIslandComponentModuleLoader<M, T>      // { module, component }
	| PhoriaIslandComponentDefaultModuleLoader<T>  // () => import("./x")
```

The **function** form imports the module and takes its **default export**. The **object** form names the export explicitly, and is required whenever the component is a named export. The `Counter` in step 5 is a named export and the `.vue` and `.svelte` components are default exports.

### Worked examples: the decisions

These are examples, not a specification. Every row below is a decision your adapter also has to make, and the value shown is one adapter's answer to it rather than the value yours must use. Reading across a row tells you what the choice is; comparing the columns tells you which decisions were forced and which were free. They are collected together because the comparison is faster than any single column.

| Decision | React | Svelte | Vue |
| --- | --- | --- | --- |
| `include` default | `["**/*.jsx", "**/*.tsx"]` | `["**/*.svelte"]` | `["**/*.vue"]` |
| `optimizeDeps` | `["react", "react-dom/client"]` | `["svelte"]` | `["vue"]` |
| `ssrExternal` | own server entry | own server entry, plus `svelte` | own server entry |
| Framework plugin return | `[...react(…)]` | `[...svelte(…)]` | `[vue(…)]` |
| SSR helpers | `renderComponentToStream` (default) and `renderComponentToString`, both in `<StrictMode>` | `renderComponentToString` only — `render()` from `svelte/server`, returns `html.body`, passed a fresh `context` map | `renderComponentToStream` (default) and `renderComponentToString`, both via `createSSRApp` |
| CSR helpers | `hydrateRoot` / `createRoot`, in `<React.StrictMode>`; runtime imported dynamically; in dev a shim defines `window.$RefreshReg$`/`$RefreshSig$` because Vite's react-refresh preamble never runs | `Svelte.hydrate` / `Svelte.mount`; props passed only when `typeof props === "object"` | `createApp` then `mount`; no hydrate/render distinction |

Two of these are worth calling out because they are invisible until they break. React's `$RefreshReg$`/`$RefreshSig$` shim exists because Phoria serves HTML from the .NET host, so Vite's dev-only react-refresh preamble — normally injected via `transformIndexHtml` — never runs, and the refresh transform throws "can't detect preamble" the first time a component module evaluates. If your framework's Vite plugin injects a preamble the same way, your adapter needs the same shim, and the first person to reload the page in development is the one who finds out.

React's stream helper uses `renderToReadableStream` from `react-dom/server.edge` rather than `react-dom/server`, for reasons React documents on [issue 26906](https://github.com/facebook/react/issues/26906); the source cites the workaround it implements. Check the equivalent for your framework rather than assuming `server` is the right entry point.
