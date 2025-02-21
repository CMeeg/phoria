# Getting started with Phoria

There are two ways you can get started:

1. [Clone an example project](#clone-an-example-project)
2. [Manually add Phoria to an existing dotnet project](#manually-add-phoria-to-an-existing-dotnet-project)

## Clone an example project

You can use [giget](https://unjs.io/packages/giget) to quickly clone an example project:

```shell
npx giget@latest gh:cmeeg/phoria-examples/examples/<example_name> <target_dir>
```

> [!IMPORTANT]
> You will need to replace:
> * `<example_name>` with the directory name of the [example project you want to clone](https://github.com/CMeeg/phoria-examples/tree/main/examples)
> * `<target_dir>` with the name of the local directory you want to clone the example project to

## Manually add Phoria to an existing dotnet project

Phoria can be added to any dotnet (>= v8) MVC or Razor Pages web app. We assume that you already have an existing dotnet web app that you want to add Phoria to, but for the purpose of this guide we will create a new web app and solution (imaginatively) called "Getting Started" and use that as our "existing project".

You can substitute "Getting Started" for your own project / solution name wherever you see that referenced in the guide.

> [!NOTE]
> This guide will not cover some aspects of setting up a new project such as configuring Git or VS Code or linting or testing tools as it is assumed that you will add and configure these things as you go or when you're ready based on your own preferences.
>
> You can use the [getting-started](https://github.com/CMeeg/phoria-examples/tree/main/examples/getting-started) example project as a reference if you wish, which is a complete example of a project created using this guide.

### Prerequisites

There is some prerequisite software you will need to have installed before you go any further:

* **dotnet** - `v8` or higher
* **Node.js** - `v18.17.1` or `v20.3.0`, `v22.0.0` or higher
  * If you're not already, we recommend using [fnm](https://github.com/Schniz/fnm) or [nvm](https://github.com/nvm-sh/nvm) to manage Node installations
* **Node package manager** - We recommend [pnpm](https://pnpm.io/), but npm will work just as well
* **Code editor** - We recommend [VS Code](https://code.visualstudio.com/), but you can use any code editor you like
* **Terminal** - You will need to be able to run CLI tools through a terminal of your choice

You will also need an existing dotnet web app. If you do not already have an existing dotnet web app then we recommend [cloning an example project](#clone-an-example-project) rather than following the rest of this guide, but if you still want to proceed you can create a new web app using the dotnet CLI:


```shell
# Create a dotnet web app
# Phoria supports dotnet 8 and 9
dotnet new webapp --name WebApp --no-restore --framework net9.0 --output ./WebApp

# Create a solution file
dotnet new sln --name GettingStarted --output .

# Add the web app project to the solution
dotnet sln add ./WebApp/WebApp.csproj
```

> [!IMPORTANT]
> You will need to substitute the solution and project names used in the rest of this guide with the names of your own solution and web app project. You may also need to adjust paths depending on the file structure of your projects and solution.

### Add Vite

The first thing you will need to do is add [Vite](https://vite.dev/) to the repo and configure Phoria via Vite plugins. The plugins configure Vite's `client` and `ssr` [environments](https://vite.dev/guide/api-environment.html) and register specific Vite plugins for the UI framework(s) that you want to use.

> [!NOTE]
> In this guide we have chosen React as the UI framework, but you can choose any [supported UI framework](./supported-ui-frameworks.md).
> 
> We used pnpm, fnm and VS Code, but feel free to substitute the tools (and therefore commands used) based on your preferences.
> 
> The guide was written against code samples developed on a Windows machine with PowerShell so some shell commands may need adapting to your OS or shell of choice also, but an attempt has been made to make the guide as platform-agnostic as possible.

```shell
# Create an `.nvmrc` file and use the Node version specified
"v22.x" > .nvmrc

fnm use

# Create a `package.json` file
npm init

# Set pnpm as the package manager
corepack enable pnpm

corepack use pnpm

# Add dependencies
pnpm add @phoria/phoria @phoria/phoria-react react react-dom

pnpm add -D @phoria/vite-plugin-dotnet-dev-certs @types/react @types/react-dom @vitejs/plugin-react typescript vite vite-tsconfig-paths
```

Then you will need to make some manual adjustments to the generated `package.json` file:

* Add `"private": true`
* Add `"type": "module"`
* Remove `"main": "index.js"`

Then add a `vite.config.ts` file to the root of your repo:

```ts
import { phoriaReact } from "@phoria/phoria-react/vite"
import { phoria } from "@phoria/phoria/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"

export default defineConfig({
  publicDir: "public",
  plugins: [
    tsconfigPaths({ root: "../../" }),
    dotnetDevCerts(),
    phoria({ cwd: "WebApp" }),
    phoriaReact()
  ]
})
```

And a [Vite env type declaration](https://vite.dev/guide/env-and-mode#intellisense-for-typescript) file at `WebApp/ui/src/vite-env.d.ts`:

```ts
/// <reference types="vite/client" />
```

And finally a `tsconfig.json` file to the root of your repo:

```json
{
  "compilerOptions": {
    "allowImportingTsExtensions": true,
    "baseUrl": ".",
    "esModuleInterop": true,
    "isolatedModules": true,
    "jsx": "react-jsx",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleDetection": "force",
    "moduleResolution": "Bundler",
    "noEmit": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedSideEffectImports": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "paths": {
      "~/*": ["WebApp/ui/src/*"]
    },
    "resolveJsonModule": true,
    "skipLibCheck": true,
    "sourceMap": true,
    "strict": true,
    "target": "ES2022",
    "useDefineForClassFields": true,
    "verbatimModuleSyntax": true
  },
  "include": [
    "WebApp/ui/src/**/*.ts",
    "WebApp/ui/src/**/*.d.ts",
    "WebApp/ui/src/**/*.tsx"
  ]
}
```

### Add a UI component

You will need to add a UI component to your repo that will eventually render inside a Phoria Island. For the purpose of this guide we are using a simple React "Counter" component, but you can substitute that for something else if you want.

We will create the component in the file `WebApp/ui/src/components/Counter/Counter.tsx`:

```tsx
import { useState } from "react"

interface CounterProps {
  startAt?: number
}

function Counter({ startAt }: CounterProps) {
  const [count, setCount] = useState(startAt ?? 0)

  return (
    <div>
      <button type="button" onClick={() => setCount((value) => value + 1)}>
        count is {count}
      </button>
    </div>
  )
}

export { Counter }
```

You will also need to [register the component](./component-register.md) so that Phoria knows where to find it and how to render it. Create the file `WebApp/ui/src/components/register.ts`:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: {
      module: () => import("./Counter/Counter.tsx"),
      component: (module) => module.Counter
    },
    framework: "react"
  }
})
```

> [!NOTE]
> You may have noticed/guessed that the `WebApp/ui` folder is the [root](https://vite.dev/config/shared-options.html#root) folder for Vite - this is where we will be placing all of the code related to our UI components. The name and location of the folder is a default used by Phoria that [can be changed](./configuration.md). [Workspaces](./workspaces.md) (e.g. pnpm workspaces) are also supported by Phoria, which can help simplify configuration and may be a better fit for a typical dotnet solution/project directory structure.

### Add Phoria Server

The Phoria Server is a [h3](https://h3.unjs.io/) server that effectively runs as a "sidecar" to your web app by running inside its own `node` process. It delegates requests to CSR, SSR or static file handlers as required, which use Vite's Dev Server in development and the build output from Vite when in production.

First you will need to add some more dependencies to your repo:

```shell
pnpm add h3 listhen

pnpm add -D @types/node tsx
```

And add a separate TypeScript config file for the Phoria Server at the root of the repo in `tsconfig.server.json`:

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "esModuleInterop": true,
    "isolatedModules": true,
    "lib": ["ES2022"],
    "module": "NodeNext",
    "moduleResolution": "NodeNext",
    "noEmit": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedSideEffectImports": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "resolveJsonModule": true,
    "skipLibCheck": true,
    "strict": true,
    "sourceMap": true,
    "target": "ES2022"
  },
  "include": [
    "WebApp/ui/src/server.ts",
    "vite.config.ts"
  ]
}
```

Then you will need to create the Phoria Server script at `WebApp/ui/src/server.ts`:

```ts
import { dirname } from "node:path"
import { fileURLToPath } from "node:url"
import {
  createPhoriaCsrRequestHandler,
  createPhoriaDevCsrRequestHandler,
  createPhoriaDevSsrRequestHandler,
  createPhoriaSsrRequestHandler,
  parsePhoriaAppSettings
} from "@phoria/phoria/server"
import { createApp, toNodeListener } from "h3"
import { type ListenOptions, listen } from "listhen"

// Get environment and appsettings

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

const nodeEnv = process.env.NODE_ENV ?? "development"
const isProduction = nodeEnv === "production"

const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv, cwd: __dirname })

// Create Vite dev server if not in production environment

const viteDevServer = isProduction
  ? undefined
  : await import("vite").then((vite) =>
      vite.createServer({
        appType: "custom",
        server: {
          middlewareMode: true
        }
      })
    )

// Create http server

const app = createApp()

if (viteDevServer) {
  // Let the Vite dev server handle CSR requests, HMR and SSR

  app.use(createPhoriaDevCsrRequestHandler(viteDevServer))

  app.use(createPhoriaDevSsrRequestHandler(viteDevServer, appsettings))
} else {
  // Configure the server to handle CSR and SSR requests

  app.use(createPhoriaCsrRequestHandler(appsettings))

  app.use(createPhoriaSsrRequestHandler(appsettings))
}

// Handle errors

app.options.onError = (error) => {
  const err = error instanceof Error ? error : new Error("Unknown error", { cause: error })
  viteDevServer?.ssrFixStacktrace(err)

  console.log({
    message: err.message,
    stack: err.stack,
    cause: {
      message: err.cause
    }
  })
}

// Start server

const listenOptions: Partial<ListenOptions> = {
  https: false,
  isProd: isProduction,
  qr: false,
  tunnel: false
}

if (viteDevServer) {
  // In dev, we will source the listener options from the vite dev server config

  listenOptions.hostname =
    typeof viteDevServer.config.server.host === "boolean"
      ? viteDevServer.config.server.host
        ? "0.0.0.0"
        : undefined
      : viteDevServer.config.server.host

  listenOptions.port = viteDevServer.config.server.port

  if (viteDevServer.config.server?.https) {
    listenOptions.https = {
      cert: viteDevServer.config.server.https.cert?.toString(),
      key: viteDevServer.config.server.https.key?.toString()
    }
  }
} else {
  // In production, we will source the listener options from appsettings

  listenOptions.hostname = appsettings.server.host
  listenOptions.port = appsettings.server.port ?? 5173

  // NOTE: If using https in production, you will need to source and pass the https options to the listener
}

const listener = await listen(toNodeListener(app), listenOptions)

// Handle server shutdown

function shutdown(signal: NodeJS.Signals) {
  console.log(`Received signal ${signal}. Shutting down server.`)

  void listener.close().then(() => {
    console.log("Server listener closed.")

    process.exit(0)
  })

  // Force shutdown after 5 seconds

  setTimeout(() => {
    console.error("Could not shutdown gracefully. Forcefully shutting down server.")

    process.exit(1)
  }, 5000)
}

process.on("SIGTERM", (signal) => shutdown(signal))
process.on("SIGINT", (signal) => shutdown(signal))

export { app, listener }
```

> [!TIP]
> Please feel free to read through the comments in the server script to get a feel for what it does.

Finally you will need to add an entry to your `scripts` in `package.json` to run the Phoria Server in your development environment:

```json
"scripts": {
  "dev": "tsx ./WebApp/ui/src/server.ts"
}
```

### Add Client Entry

The [Client Entry](./client-entry.md) is the entrypoint for the browser and is responsible for hydrating your registered Phoria Island components using the appropriate CSR strategy provided by your chosen UI framework(s).

You can also choose to initialise other client-side code here, or import "global" CSS, if you wish.

> [!NOTE]
> Phoria supports client-only, server-only and "isomorphic" rendering of components in Islands. Server-only is the default, but you can opt-in to client-side rendering on an Island-by-Island basis using one or more client [directives](./directives.md) such as "client only", "on load" etc. If an Island is server-only then it will not hydrate on the client and therefore does not request any additional JavaScript.

Add a Client Entry file at `WebApp/ui/src/entry-client.ts`:

```ts
import "@phoria/phoria-react/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"

PhoriaIsland.register()
```

> [!NOTE]
> `PhoriaIsland` is a custom HTML element that is used to hydrate the components that you have [registered](#add-a-ui-component) in your component registration file (i.e. `./components/register`) for Islands that you have opted-in to client-side rendering.

### Add Server Entry

The [Server Entry](./server-entry.md) is the entrypoint for the [Phoria Server](#add-phoria-server) and is responsible for generating the markup of your registered Phoria Island components using the appropriate SSR strategy provided by your chosen UI framework(s). The Phoria Server SSR response is proxied back to the web app to be appended to the HTTP response stream.

Add a Server Entry file at `WebApp/ui/src/entry-server.ts`:

```ts
import "@phoria/phoria-react/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"

async function renderPhoriaIsland(island: PhoriaIsland) {
	return await island.render()
}

export { renderPhoriaIsland }
```

> [!NOTE]
> `island.render()` will call the associated UI framework plugin's default render strategy, which in the case of React is [`renderToReadableStream`](https://react.dev/reference/react-dom/server/renderToReadableStream).
>
> The `island.render()` function does accept a custom render strategy if you need further control over it, for example if you are using a library like [styled components](https://styled-components.com/docs/advanced#server-side-rendering).

### Add Phoria to the dotnet web app

Now you can add Phoria to your dotnet web app. From this point on we will refer to it as the [Phoria Web App](./phoria-web-app.md), which is just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured.

First you will need to add the Phoria NuGet package to the web app:

```shell
dotnet add WebApp/WebApp.csproj package Phoria
```

Then add the following section to `WebApp/appsettings.json`:

```json
{
  "phoria": {
    "root": "WebApp/ui",
    "entry": "src/entry-client.ts",
    "ssrEntry": "src/entry-server.ts"
  }
}
```

> [!NOTE]
> Any [configuration](./configuration.md) option that is not set explicitly will fallback to a default value except for `entry` and `ssrEntry` because there are no sensible defaults for these options.

And the following to `WebApp/appsettings.Development.json`:

```json
{
  "phoria": {
    "server": {
      "https": true
    }
  }
}
```

> [!NOTE]
> The `https` option will default to `false` so we are setting it here because we want our Phoria Server to use `https` in development.

Then you will need to modify `WebApp/Program.cs` to register Phoria services. Add the following code in the appropriate places (i.e. the below does not represent a complete `Program.cs` file, just the parts you need to add to configure Phoria):

```csharp
// Add a using statement
using Phoria;

// Add Phoria services
builder.Services.AddPhoria();

// WebSockets support is required for Vite HMR (hot module reload)
if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
}

// The order of the Phoria middleware matters so we will place this just before `app.Run()`
app.UsePhoria();
```

> [!TIP]
> Phoria can be configured programmatically via `AddPhoria`, but it's recommended to use `appsettings` because then the `phoria` Vite plugin and the Phoria Server can read that same configuration from the `appsettings.*.json` files rather than having to duplicate it in each place.

### Add the Phoria Tag Helpers

The `Phoria` NuGet package contains a set of [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro) for:

* Rendering script and style tags from your [Client Entry](#add-client-entry) script
* Rendering [preload directives](https://vite.dev/guide/ssr#generating-preload-directives) for your scripts and styles to avoid a "Flash Of Unstyled Content" (FOUC)

Add the following to `WebApp/Pages/_ViewImports.cshtml`:

```html
@using Phoria.Islands
@addTagHelper *, Phoria
```

Add the following Tag Helpers to `WebApp/Pages/Shared/_Layout.cshtml` just before the closing `</head>` tag:

```html
<phoria-island-styles />
<phoria-island-preload />
```

And the following Tag Helper to `WebApp/Pages/Shared/_Layout.cshtml` just before the closing `</body>` tag:

```html
<phoria-island-scripts />
```

### Add a Phoria Island

The `Phoria` NuGet package contains a Tag Helper for rendering Phoria Islands. The Tag Helper will take care of everything to do with rendering the component:

* Serialising data passed inline or from the view model as `props` for the component
* Requesting an SSR response from the Phoria Server (if required, Islands can be CSR only)
* Hydrating the component on the client (if required, Islands can be SSR only)

For the purpose of this guide we will render the `Counter` component that we [added and registered](#add-a-ui-component) earlier. You will need to adapt the below if you registered a different component.

Add the following Tag Helper to `WebApp/Pages/Index.cshtml` at the bottom of the file:

```html
<phoria-island component="Counter" client="Client.Load" props="new { StartAt = 5 }"></phoria-island>
```

> [!TIP]
> The value of `component` must match a key in your [component register](#add-a-ui-component). In our case we registered the `Counter` component using the key `Counter`.
> 
> The value of `client` is set to `Client.Load` because this component uses React state and we want this component to hydrate on page load. You can try using other client [directives](./directives.md) if you want to see how they work.
>
> The value of `props` is an anonymous object that will be serialised and passed to the component as `props` to demonstrate how you can pass data from your dotnet web app to your Islands. The data is hardcoded here, but can come from any source and passed through your view model.

### Run the Phoria Web App

Now you can run your app and see Phoria in action.

From your terminal run:

```shell
# Add dev certs
dotnet dev-certs https --trust

# Start the Phoria Server
pnpm run dev

# Start the Phoria Web App
# You will need to run this in a separate terminal instance/tab to the Phoria Server
dotnet run --project WebApp/WebApp.csproj --launch-profile https
```

> [!TIP]
> The `dotnet run` command doesn't automatically launch the browser unfortunately, but you can find the URL for the web app in the terminal output or by looking in your `WebApp/Properties/launchSettings.json` file.

Now you will be able to navigate to the web app in your browser and:

* See the `Counter` component rendered via React
* View the page source to see the SSR response from the Phoria Server
* Click on the button to see that the component has hydrated and is reacting to state changes
  * The first time you run the app Vite may take a couple of seconds to optimise dependencies so you may see a delay before the component hydrates - you can see this happening in the terminal where you started the Phoria Server
* Make a change to the `Counter.tsx` component to see HMR working

### Next steps

If you're curious about how Phoria works in a production environment you can check out the [building for production](./building-for-production.md) guide.
