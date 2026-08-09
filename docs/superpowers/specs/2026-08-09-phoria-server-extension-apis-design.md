# Design: Phoria Server Extension APIs

Date: 2026-08-09

## Objective

Make the Phoria Server extension seams explicit without coupling the core `@phoria/phoria` package to OpenTelemetry. The core package owns the logger contract and console fallback, while `@phoria/opentelemetry` supplies the OTel implementation, observability settings binding, and request instrumentation wrapper.

## Core Logger Contract

Move `PhoriaLogger` out of `packages/phoria-islands/src/server/routing.ts` into a focused core server logger module. Export:

- `PhoriaLogger`, with `info`, `warn`, and `error` methods accepting an optional `Record<string, unknown>` payload.
- `phoriaConsoleLogger`, the default implementation delegating to `console.info`, `console.warn`, and `console.error`.

The CSR and SSR handler factories use `phoriaConsoleLogger` when no logger is supplied. Their existing optional logger behavior and public exports remain compatible. The core package has no OpenTelemetry dependency.

## Generic App Settings

Make the parsed Phoria section extensible through a generic additional-property shape:

```ts
interface PhoriaAppSettings<TAdditional extends Record<string, unknown> = Record<string, never>>
  extends PhoriaBaseAppSettings,
    TAdditional {}
```

`parsePhoriaAppSettings<TAdditional>()` returns `Promise<PhoriaAppSettings<TAdditional>>`. The parser continues to merge arbitrary `phoria` JSON properties at runtime, but the core package does not name or interpret extension properties.

The OTel package supplies the extension type at its boundary:

```ts
type PhoriaOtelAppSettings = PhoriaAppSettings<{
  observability?: Partial<PhoriaObservabilityAppSettings>
}>
```

This lets future integrations extend appsettings without modifying the core package for each integration.

## OTel Adapters

### Settings binding

Export `bindPhoriaObservabilityAppSettings(appsettings)` from `@phoria/opentelemetry`. It accepts the parsed generic Phoria settings object and maps its optional `observability` property onto the existing fully-defaulted `PhoriaObservabilityAppSettings` result. It is synchronous because file I/O has already completed in `parsePhoriaAppSettings`.

The existing standalone observability file parser is removed. `server.ts` performs one appsettings read:

```ts
const appsettings = await parsePhoriaAppSettings<PhoriaOtelAppSettings>({
  environment: dotnetEnv,
  cwd: __dirname
})
const observabilitySettings = bindPhoriaObservabilityAppSettings(appsettings)
```

### Logger implementation

The existing OTel logger factory remains the public `createPhoriaLogger` API and returns the core `PhoriaLogger` contract. When logging is disabled it delegates to the core console logger; when enabled it emits OTel log records. The implementation may import the core logger type/value as a package dependency if needed, but the core package never imports OTel.

### Instrumentation wrapper

Export `withPhoriaOtelInstrumentation(appsettings)`, accepting the generic parsed settings shape and returning the h3 app options object. It delegates internally to:

```ts
createPhoriaRequestSpanHook({ base: appsettings.base, ssrBase: appsettings.ssrBase })
```

The lower-level request-span hook remains exported for focused tests and advanced consumers. Example code becomes:

```ts
const app = createApp(withPhoriaOtelInstrumentation(appsettings))
```

## Dependency Catalog

Move `defu` to the workspace catalog at `^6.1.7`. Both `@phoria/phoria` and `@phoria/opentelemetry` use the catalog protocol. No runtime behavior changes.

## Testing

- Core tests verify the exported console logger shape and existing handler defaults remain functional.
- Core appsettings tests verify generic parsing retains extension properties and preserves existing defaults/validation.
- OTel appsettings tests verify binding from a parsed settings object, defaults, and environment override values.
- OTel logger tests verify the returned value conforms to the core logger contract and preserves console fallback/OTel emission behavior.
- OTel request-span tests retain existing behavior coverage and add wrapper coverage proving it delegates the configured base and SSR base.
- Both example `server.ts` files use the single parser call, the binder, and `createApp(withPhoriaOtelInstrumentation(appsettings))`.

Verification remains the repository CI sequence: root build, lint, check, JS tests, and .NET tests, plus both example checks/builds using local links where the new package is unpublished.

## Compatibility

Existing exported names remain available unless they were previously private. `createPhoriaLogger` is retained. `createPhoriaRequestSpanHook` is retained. The change adds generic type capability and new named helpers without requiring OTel consumers to use the lower-level hook directly.
