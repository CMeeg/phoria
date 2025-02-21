# Building for production

This guide will walk you through adding `scripts` to `package.json` that will [build](#build-scripts) the three parts of a Phoria solution that are required when running in production:

* Phoria Islands
* Phoria Web App
* Phoria Server

Also included in this guide are instructions for adding [preview](#preview-scripts) `scripts` so that you can run the production build in the local environment for testing purposes.

> [!TIP]
> If you cloned an example project to get started you should already have `build` and `preview` scripts included in your `package.json`. This guide is for those who want to add these scripts manually to their solution, or for those who want to understand more about what the scripts do.

## Build scripts

These are the `scripts` that you will need to add to build your Phoria solution for production:

```json
{
  "scripts": {
    "build": "run-p build:* -c",
    "build:islands": "vite build --app",
    "build:webapp": "dotnet build --configuration Release",
    "build:server": "vite build --config vite.server.config.ts"
  }
}
```

The next sections of the guide will describe each script in turn and any dependencies that you will need to add to your project to support them.

When you have added the scripts and all required dependencies you can build your Phoria solution for production by running:

```shell
pnpm run build
```

### `build`

This script is a convenience script that uses the [`npm-run-all`](https://github.com/mysticatea/npm-run-all) package to run the other three build scripts in parallel.

```shell
pnpm add -D npm-run-all
```

> [!NOTE]
> `npm-run-all` is not required and you can use some other package or tool or shell feature (e.g. `&`) to do the same thing, if you prefer. The reason it is used here is because `&` doesn't work consistently on Windows and we want our scripts to be platform-agnostic.

### `build:islands`

This script uses [Vite's Environment API](https://vite.dev/guide/api-environment.html) to build the optimised CSR and SSR bundles that will be used in production and it also produces a [manifest](https://main.vite.dev/config/build-options.html#build-manifest) for Phoria to use:

* The CSR bundles are used by Phoria Islands to load component assets on the client
* The SSR bundles are used by the Phoria Server to load component assets on the server
* The manifest is used by the Phoria Web App to [generate preload directives](https://main.vite.dev/guide/ssr#generating-preload-directives)

The build configuration for Vite is provided via your Vite config file (e.g. `vite.config.ts`), and the configuration for Phoria specifically is provided via the `phoria*` Vite plugins.

By default, the build output will be placed in the `<Vite root>/dist` directory.

### `build:webapp`

This script uses the [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) to build the Phoria Web App in its `Release` configuration.

By default, the build output will be placed in the `<WebApp root>/bin/Release/<Target framework>` directory.

> [!WARNING]
> You may need to [adjust this command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build#arguments) depending on the structure of your project to point to a specific solution (`.sln`) or project (`.csproj`) file.

### `build:server`

This script uses Vite to build the Phoria Server. It uses a different Vite config file (`vite.server.config.ts`) because it does not share its configuration with Phoria Islands and needs specific configuration options.

Below is an example of a `vite.server.config.ts` that you can use:

```ts
import { join } from "node:path"
import { parsePhoriaAppSettings } from "@phoria/phoria/server"
import { type UserConfig, defineConfig } from "vite"

export default defineConfig(async () => {
  const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
  const appsettings = await parsePhoriaAppSettings({
    environment: dotnetEnv,
    cwd: join(process.cwd(), "WebApp")
  })

  return {
    root: appsettings.root,
    base: appsettings.base,
    build: {
      ssr: true,
      target: "es2022",
      copyPublicDir: false,
      emptyOutDir: true,
      outDir: `${appsettings.build.outDir}/server`,
      rollupOptions: {
        input: `${appsettings.root}/src/server.ts`
      }
    }
  } satisfies UserConfig
})
```

If you use the configuration above, the build output will be placed in the `<Vite root>/dist/server` directory.

> [!TIP]
> The `parsePhoriaAppSettings` function is provided by the `@phoria/phoria` package and is used to read the `appsettings` file(s) in the `WebApp` project and provide the configuration to Vite. This is useful for sharing configuration between the Phoria Server and the Phoria Web App.

> [!NOTE]
> You don't need to use Vite to build the Phoria Server. You could use something like [`tsup`](https://github.com/egoist/tsup) or any other TypeScript to JavaScript transpiler or bundler if you prefer. It is just convenient to use Vite because we are already using it to build our Phoria Islands and means that we don't need to add another dependency.
>
> You could also decide to just use JavaScript for your Phoria Server and avoid a build step entirely if that's what you want to do.

## Preview scripts

These are the `scripts` that you will need to preview the production build of our Phoria solution locally:

```json
{
  "scripts": {
    "preview": "run-p preview:* -c",
    "preview:webapp": "cross-env DOTNET_ENVIRONMENT=Preview dotnet run --project ./WebApp/WebApp.csproj -c Release --launch-profile Preview",
    "preview:server": "cross-env NODE_ENV=production DOTNET_ENVIRONMENT=Preview node ./WebApp/ui/dist/server/server.js"
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

### `preview`

This script is a convenience script that uses the [`npm-run-all`](https://github.com/mysticatea/npm-run-all) package to run the other three preview scripts in parallel.

> [!NOTE]
> This guide will assume that you are using `npm-run-all` in your `build` script so there is no need to install it again, but if you did choose to use something else for your `build` script you will need to make the same adjustments here.

### `preview:webapp`

This script uses the [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run) to run the Phoria Web App produced by the [`build:webapp`](#buildwebapp) script.

The [`cross-env`](https://github.com/kentcdodds/cross-env) package is used to set the `DOTNET_ENVIRONMENT` environment variable so you can use specific configuration or conditional branching in your code etc that targets the `Preview` environment if you wish.

```shell
pnpm add -D cross-env
```

> [!NOTE]
> `cross-env` is used because we want our scripts to be platform-agnostic, but is not required if you do not need to support Windows environments.

This script also uses a launch profile named `Preview`, which you can add to your `launchSettings.json` file:

```json
{
  "profiles": {
    "Preview": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5245",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Preview"
      }
    }
  }
}
```

> [!WARNING]
> You may need to adjust this command depending on the structure of your project to point to the actual location of your Phoria Web App's `.csproj` file.

### `preview:server`

This script uses `node` to run the Phoria Server produced by the [`build:server`](#buildserver) script. `cross-env` is used again to set environment variables used by the script.

> [!WARNING]
> You may need to adjust this command depending on your configuration to point to the location of the Phoria Server output produced by the `build:server` script.

## Next steps

If you would now like to try deploying your production build you can check out the [deployment](./deployment.md) guide.
