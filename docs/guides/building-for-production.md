# Building for production

There are three parts to a Phoria solution that need to be built for production:

* Phoria Islands
* Phoria Web App
* Phoria Server

We are going to setup `scripts` in `package.json` to [build](#build-scripts) all of these parts, and then add some other `scripts` that we can use to [preview](#preview-scripts) a production build in the local environment.

## Build scripts

These are the `scripts` we will add to build our Phoria solution:

```json
{
  "scripts": {
    "build": "run-p build:* -c",
    "build:app": "vite build --app",
    "build:dotnet": "dotnet build --configuration Release",
    "build:server": "vite build --config vite.server.config.ts"
  }
}
```

Read on if you would like more info about each of the scripts, or you can feel free to skip to the [preview](#preview-scripts) scripts.

### `build`

We are using the [`npm-run-all`](https://github.com/mysticatea/npm-run-all) package to run the other three build scripts in parallel.

> [!NOTE]
> `npm-run-all` is not required and you can use some other package or tool or shell feature (e.g. `&`) to do the same thing. The reason we are using it is because `&` doesn't work consistently on Windows.

### `build:app`

This script uses Vite to build the optimised CSR and SSR bundles that will be used in production and produces a [manifest](https://main.vite.dev/config/build-options.html#build-manifest):

* The CSR bundles are used by Phoria Islands to load component assets on the client
* The SSR bundles are used by the Phoria Server to load component assets on the server
* The manifest is used by the Phoria Web App to [generate preload directives](https://main.vite.dev/guide/ssr#generating-preload-directives)

The build configuration for Vite is provided via your Vite config file (e.g. `vite.config.ts`), and the configuration for Phoria specifically is provided via the `phoria*` plugins.

### `build:dotnet`

This script uses the [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) to build the Phoria Web App in its `Release` configuration.

> [!WARNING]
> You may need to adjust this command depending on the structure of your project to point to a specific solution (`.sln`) or project (`.csproj`) file.

### `build:server`

This script uses Vite to build the Phoria Server. It uses a different Vite config file (`vite.server.config.ts`) because it does not share its configuration with Phoria Islands and needs specific configuration options.

Below is an example of a `vite.server.config.ts` that you can use:

```ts
import { parsePhoriaAppSettings } from "@phoria/phoria/server"
import { type UserConfig, defineConfig } from "vite"

export default defineConfig(async () => {
  const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
  const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv })

  // https://vite.dev/config/
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

> [!NOTE]
> You don't need to use Vite to build the Phoria Server. You could use something like [`tsup`](https://github.com/egoist/tsup) or any other TypeScript to JavaScript transpiler or bundler if you prefer. We are using Vite because we are already using it so its convenient.
>
> Equally you could decide to just use JavaScript for your Phoria Server and avoid a build step entirely though we don't recommend it.

## Preview scripts

These are the scripts we will add to preview the production build of our Phoria solution locally:

```json
{
  "scripts": {
    "preview": "run-p preview:* -c",
    "preview:dotnet": "cross-env DOTNET_ENVIRONMENT=Preview dotnet run ./WebApp/bin/Release/net9.0/WebApp.dll --launch-profile Preview",
    "preview:phoria": "cross-env NODE_ENV=production DOTNET_ENVIRONMENT=Preview node ./ui/dist/server/server.js"
  }
}
```

Read on if you would like more info about each of the scripts.

### `preview`

We are using `npm-run-all` again here, but the same applies as with the `build` script if you want to use something else.


### `preview:dotnet`

We are using the [dotnet CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run) to run the Phoria Web App produced by the `build:dotnet` script.

The [`cross-env`](https://github.com/kentcdodds/cross-env) package is used to set the `DOTNET_ENVIRONMENT` so you can use specific configuration or conditional branching in your code etc that targets the `Preview` environment. This script also assumes that you have a launch profile named `Preview`.

> [!WARNING]
> You may need to adjust this command depending on the structure of your project to point to the actual `.dll` of your Phoria Web App produced by the `build:dotnet` script.

### `preview:phoria`

This script uses `node` to run the Phoria Server produced by the `build:server` script.

> [!WARNING]
> You may need to adjust this command depending on your configuration to point to the Phoria Server script produced by the `build:server` script.
