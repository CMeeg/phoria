# Getting started with Phoria

There are two ways you can get started:

1. [Clone an example project](#clone-an-example-project)
2. [Manually add Phoria to an existing dotnet project](#manually-add-phoria-to-an-existing-dotnet-project)

## Clone an example project

You can use [giget](https://unjs.io/packages/giget) to quickly clone an example project:

`npx giget@latest gh:cmeeg/phoria-examples/examples/{example-name} {dir}`

> [!NOTE]
> You will need to replace:
> 
> * `{example-name}` with the folder name of the [example project you want to clone](https://github.com/CMeeg/phoria-examples/tree/main/examples)
> * `{dir}` with the name of the local directory you want to clone the example project to

## Manually add Phoria to an existing dotnet project

Phoria can be added to any dotnet (>= v8) MVC or Razor Pages web app. We assume that you already have an existing dotnet web app that you want to add Phoria to, but for the purpose of this guide we will create a new web app and solution (imaginatively) called "Getting Started". You can substitute "Getting Started" for your own project / solution name wherever you see that referenced in the guide.

> [!NOTE]
> This guide will not cover some aspects of setting up a new project such as configuring git or VS Code or linting or testing tools as it is assumed that you will add and configure these things as you go or when you're ready based on your own preferences.
>
> You can use the [getting-started](https://github.com/CMeeg/phoria-examples/tree/main/examples/getting-started) example project as a reference if you wish, which is a complete example of a project created using this guide.

### Prerequisites

There is some prerequisite software you will need to have installed before we go any further:

* **dotnet** - `v8` or higher
* **Node.js** - `v18.17.1` or `v20.3.0`, `v22.0.0` or higher
  * We recommend using [fnm](https://github.com/Schniz/fnm) or [nvm](https://github.com/nvm-sh/nvm) to manage Node installations
* **Node package manager** - We recommend [pnpm](https://pnpm.io/), but npm will work just as well
* **Code editor** - We recommend [VS Code](https://code.visualstudio.com/)
* **Terminal** - You will need to be able to run CLI tools through a terminal of your choice

You will also need an existing dotnet web app. If you do not already have an existing dotnet web app then we recommend [cloning an example project](#clone-an-example-project) rather than following this manual process, but if you still want to proceed you can create a new web app using the dotnet CLI:


```shell
# Create a dotnet web app
# Phoria supports dotnet 8 and 9
dotnet new webapp --name WebApp --no-restore --framework net9.0 --output ./WebApp

# Create a solution file
dotnet new sln --name GettingStarted --output .

# Add the web app project to the solution
dotnet sln add ./WebApp/WebApp.csproj
```

> [!NOTE]
> You will need to substitute the solution and project names used in the rest of this guide with the names of your own solution and web app project.

### Add Vite

The first thing we are going to do is add [Vite](https://vite.dev/) to the repo and configure Phoria via Vite plugins. The plugins configure Vite's `client` and `ssr` [environments](https://vite.dev/guide/api-environment.html) and register specific Vite plugins for the UI framework(s) that you want to use.

> [!NOTE]
> In this guide we will choose React as our UI framework, but you can choose any [supported UI framework](./supported-ui-frameworks.md).
> 
> We will also be using pnpm, fnm and VS Code, but feel free to substitute the tools (and therefore commands used) based on your preferences.
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

Then we will need to make some manual adjustments to the generated `package.json` file:

* Add `"private": true`
* Add `"type": "module"`
* Remove `"main": "index.js"`

Next add a `vite.config.ts` file to the root of your repo:

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

We need to add a UI component to our repo that we will render in a Phoria Island. For the purpose of this guide we will use a simple React "Counter" component, but you can substitute that for something else if you want.

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

We also need to [register the component](./component-register.md) so that Phoria knows where to find it and how to render it. We will do this in the file `WebApp/ui/src/components/register.ts`:

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
> You may have noticed/guessed that the `ui` folder is the [root](https://vite.dev/config/shared-options.html#root) folder for Vite - this is where we will be placing all of our code related to our UI components. The name and location of the folder is a default used by Phoria that [can be changed](./configuration.md) and you can optionally use workspaces should you wish to do so.

### Add Phoria Server

The Phoria Server is a [h3](https://h3.unjs.io/) server that effectively runs as a "sidecar" to your web app by running inside its own `node` process. It delegates requests to CSR, SSR or static file handlers as required, which use Vite's Dev Server in development and the build output from Vite when in production.

First we need to add some more dependencies to our repo:

```shell
pnpm add h3 listhen

pnpm add -D @types/node tsx
```

And add a separate TypeScript config file for the Phoria Server at the root of the repo `tsconfig.server.json`:

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

Then we will create the Phoria Server script at `WebApp/ui/src/server.ts`:

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

Finally we will add an entry to our `scripts` in `package.json` to run the Phoria Server in our development environment:

```json
"scripts": {
  "dev": "tsx ./WebApp/ui/src/server.ts"
}
```

### Add Client Entry

The [Client Entry](./client-entry.md) is the entrypoint for the browser and is responsible for hydrating your registered Phoria Island components using the appropriate CSR strategy provided by your chosen UI framework(s).

You can also choose to initialise other client-side code here, or import "global" CSS, if you wish.

> [!NOTE]
> Phoria supports client-only, server-only and "isomorphic" rendering of components in Islands. Server-only is the default, but you can opt-in to client-side rendering on an Island-by-Island basis using one or more client directives such as "client only", "on load", "on idle", "on visible", "on match media". If an Island is server-only then it will not hydrate on the client and therefore does not request any JavaScript.

Add a Client Entry file at `WebApp/ui/src/entry-client.ts`:

```ts
import "@phoria/phoria-react/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"

PhoriaIsland.register()
```

> [!NOTE]
> `PhoriaIsland` is a custom HTML element that is used to hydrate the components that you have registered in your component registration file (i.e. `./components/register`) for Islands that you have opted-in to client-side rendering.

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

### Configure the Phoria Web App

TODO

### Add Phoria Islands

TODO

### Run the Phoria Web App

TODO

The Phoria Server is a Node script that starts a [h3](https://h3.unjs.io/) server and listens for CSR (Client Side Rendering) and SSR (Server Side Rendering) requests that are proxied through to it from the Phoria web app.

> [!NOTE]
> The Phoria libraries that you are about to install expose request handlers that are consumed by the server, but you own the server so feel free to extend it and use it for other things should you wish.























Next we will make some minor amends to the Scaffold output:

```shell
# Delete the following files
src
├── index.css
├── main.tsx
index.html

# Move the `public` and `src` folders under a `ui` folder
ui
├── public
└── src

```















```shell
# Add the Phoria NuGet package to the web app
dotnet add WebApp.csproj package Phoria
```



TODO: Mention that recommendation is to use workspaces, but Getting Started app will not because no assumption will be made that you are or want to use workspaces. Include an optional section to add workspaces at the end.









### Phoria Web App

Now we will set up the [Phoria Web App](./phoria-web-app.md), which is just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured.



#### Configure the web app

Phoria is added to your app via the `WebApplicationBuilder` and configured in `appsettings.json`.

> [!TIP]
> Phoria can be configured programmatically via the `WebApplicationBuilder`, but it's recommended to use `appsettings.json` because then the `phoria` Vite plugin and the Phoria `server.ts` can read and share that configuration.

Either copy and paste the code below or edit your `Program.cs` file to add the lines that are preceded by a comment:

```csharp
// Add a using statement
using Phoria;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Add Phoria services
builder.Services.AddPhoria();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages().WithStaticAssets();

// WebSockets are required for the Vite Dev Server's HMR (Hot Module Reload) feature
if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
}

// The order of the Phoria middleware matters so we will place it last
app.UsePhoria();

app.Run();
```

Add the following section to your `appsettings.json` file:

```json
{
  // ... Existing settings ...

  // Add the Phoria section
  "phoria": {
    "entry": "src/entry-client.ts",
    "ssrEntry": "src/entry-server.ts"
  }
}
```

And the following to your `appsettings.Development.json` file:

```json
{
  // ... Existing settings ...

  // Add the Phoria section
  "phoria": {
    "server": {
      "https": true
    }
  }
}
```

> [!NOTE]
> Any [configuration](./configuration.md) option that is not set explicitly will fallback to a default value except for `entry` and `ssrEntry` because there are no sensible defaults for these options. The `https` option will default to `false` so we are setting it here because we want our Phoria Server to use `https` in development.

#### Add the Phoria Tag Helpers

The `Phoria` NuGet package contains a set of [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro) for:

* Rendering script and style tags from your [Client Entry](#add-client-entry) script
* Rendering [preload directives](https://vite.dev/guide/ssr#generating-preload-directives) for your scripts and styles to avoid a "Flash Of Unstyled Content" (FOUC)
  * This only applies to the [production](./build-for-production.md) build

We will add these Tag Helpers to our Layout:

* Edit `Pages/Shared/_ViewImports.cshtml`
* Add the following lines and save:

```html
@using Phoria.Islands
@addTagHelper *, Phoria
```

* Edit `Pages/Shared/_Layout.cshtml`
* Add the following Tag Helpers just before the closing `</head>` tag:

```html
<phoria-island-styles />
<phoria-island-preload />
```

* Add the following Tag Helper just before the closing `</body>` tag:

```html
<phoria-island-scripts />
```

Phoria is now setup and ready and you can start rendering Phoria Islands.

### Adding Phoria Islands

The `Phoria` NuGet package contains a Tag Helper for rendering Phoria Islands. The Tag Helper will take care of everything to do with rendering the component:

* Serialising data passed inline or from the view model as `props` for the component
* Requesting an SSR response from the Phoria Server, if required (Islands can be CSR only)
* Hydrating the component on the client, if required (Islands can be SSR only)

For the purpose of this guide we will render the `Counter` component that we created from the Vite Project Scaffold.

Edit `Pages/Index.cshtml`:

* Add the following Tag Helper at the bottom of the file:

```html
<phoria-island component="Counter" client="Client.Load"></phoria-island>
```

> [!NOTE]
> The `component` name must match the name of the component we added to the [Component Register](#add-and-register-a-component). The `client` directive is set to `Client.Load` because we want this component to hydrate on page load.
> 
> Please see the [Phoria Islands](./phoria-islands.md) guide for a description of available client directives and other options.

### Running the Phoria Web App

Now we can run our app and see Phoria in action.

From your terminal run:

```shell
# Add dev certs
dotnet dev-certs https --trust

# Start the Phoria Server
pnpm run dev

# Start the Phoria Web App (you will need to run this in a separate terminal instance/tab to the Phoria Server)
dotnet run --project WebApp.csproj --launch-profile https
```

> [!TIP]
> The `dotnet run` command doesn't automatically launch the browser unfortunately, but you can find the URL for the web app in the terminal output or by looking in your `launchSettings.json` file.

Now you will be able to navigate to the web app in your browser and:

* See the `Counter` component rendered via React
* See the styles and assets imported by the React component
  * Though granted it doesn't look great because it's all mixed in with the styles from the dotnet web app!
* Click on the button that reads `count is 0` to see that the component has hydrated and is reacting to state changes
* Make a change to the `Counter.tsx` component to see HMR working

### Next steps

If you're curious about how Phoria works in a production environment you can check out the [building for production](./building-for-production.md) guide.
