# Building for production

This guide will walk you through adding `scripts` to `package.json` that will [build](#build-scripts) the parts of a Phoria solution that are required when running in production:

* Phoria Islands (client, SSR and Phoria Server bundles)
* Phoria Web App

Also included in this guide are instructions for adding [preview](#preview-scripts) `scripts` so that you can run the production build in the local environment for testing purposes.

> [!TIP]
> If you cloned an example project to get started you should already have `build` and `preview` scripts included in your `package.json`. This guide is for those who want to add these scripts manually to their solution, or for those who want to understand more about what the scripts do.

## Build scripts

These are the `scripts` that you will need to add to build your Phoria solution for production:

```json
{
  "scripts": {
    "build": "concurrently \"pnpm:build:*\"",
    "build:islands": "vite build --app",
    "build:webapp": "dotnet build --configuration Release"
  }
}
```

The next sections of the guide will describe each script in turn and any dependencies that you will need to add to your project to support them.

When you have added the scripts and all required dependencies you can build your Phoria solution for production by running:

```shell
pnpm run build
```

### `build`

This script is a convenience script that uses the [`concurrently`](https://github.com/open-cli-tools/concurrently) package to run the other two build scripts in parallel.

```shell
pnpm add -D concurrently
```

> [!NOTE]
> The `pnpm:build:*` shorthand runs every `build:*` script in parallel via `pnpm run`. `concurrently` is not required and you can use some other package or tool or shell feature (e.g. `&`) to do the same thing, if you prefer. The reason it is used here is because `&` doesn't work consistently on Windows and we want our scripts to be platform-agnostic.

### `build:islands`

This script uses [Vite's Environment API](https://vite.dev/guide/api-environment.html) and the [`builder.buildApp`](https://vite.dev/guide/api-environment.html#buildapp-hook) hook to build the client, SSR and Phoria Server bundles as three separate Vite environments, in that order:

* The client bundles are used by Phoria Islands to load component assets in the browser
* The SSR bundles are used by the Phoria Server to render Islands to markup on the server
* The Phoria Server bundle is the [h3](https://h3.unjs.io/) server that Phoria runs as a sidecar process
* A [manifest](https://main.vite.dev/config/build-options.html#build-manifest) is also produced, which the Phoria Web App uses to [generate preload directives](https://main.vite.dev/guide/ssr#generating-preload-directives)

The `phoria` Vite plugin builds the client environment first because it emits the `ssr-manifest.json` that the Phoria Server and Phoria Web App rely on, then the `ssr` environment, then the `server` environment — so you don't need a separate Vite config file or build script for the Phoria Server.

The build configuration for Vite is provided via your Vite config file (e.g. `vite.config.ts`), and the configuration for Phoria specifically is provided via the `phoria*` Vite plugins.

By default, the build output will be placed in the `<Vite root>/dist` directory, with the Phoria Server bundle at `<Vite root>/dist/server/server.js`.

> [!TIP]
> By default, the `phoria` plugin looks for the Phoria Server entry at `<Vite root>/src/server.ts`. If your entry lives elsewhere, or you don't want Phoria to build the Phoria Server at all, set the `serverEntry` plugin option:
>
> ```ts
> phoria({ serverEntry: "src/my-server.ts" })
> // or
> phoria({ serverEntry: false })
> ```

### `build:webapp`

This script uses the [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) to build the Phoria Web App in its `Release` configuration.

By default, the build output will be placed in the `<WebApp root>/bin/Release/<Target framework>` directory.

> [!WARNING]
> You may need to [adjust this command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build#arguments) depending on the structure of your project to point to a specific solution (`.sln`) or project (`.csproj`) file.

## Preview scripts

These are the `scripts` that you will need to preview the production build of our Phoria solution locally:

```json
{
  "scripts": {
    "preview": "aspire start --environment Preview"
  }
}
```

The next sections of the guide will describe each script in turn and any dependencies that you will need to add to your project to support them.

When you have added the scripts and all required dependencies you can preview your production build by running:

```shell
# Build the Phoria solution
pnpm run build

# Preview the Phoria solution
pnpm run preview
```

### Aspire preview

The `preview` script starts the Aspire AppHost. The AppHost starts the Web App, the compiled Phoria Server as a sibling process, and the Aspire dashboard. The AppHost runs the WebApp package's `preview:server` script, so the Node command and arguments are defined with the example's package scripts rather than duplicated in `appsettings.Preview.json`.

Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/) and run the build before starting the preview:

```shell
# Build the Phoria solution
pnpm run build

# Start the AppHost and dashboard
pnpm run preview
```

The AppHost sets `DOTNET_ENVIRONMENT=Preview` for the Web App and `NODE_ENV=production` for the Phoria Server. Use the dashboard URL printed by `aspire start` to inspect both resources and their OpenTelemetry logs. When you are done, stop the preview with `aspire stop` so that no managed resources are left running.

Because ASP.NET Core only auto-loads static web assets in the `Development` environment, a Preview app must opt in for `MapStaticAssets` to serve the assets that live in the build output rather than `wwwroot` (e.g. the generated `<app>.styles.css` and fingerprinted files). Call `builder.WebHost.UseStaticWebAssets()` guarded by the Preview environment before the app is configured — as the example apps' `Program.cs` do — otherwise those requests fail with `Could not find file` errors.

## Next steps

If you would now like to try deploying your production build you can check out the [deployment](./deployment.md) guide.
