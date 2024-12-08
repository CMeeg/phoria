# Getting started with Phoria

Adding Phoria to a project is currently a [manual process](#manual-process), but there are [examples](https://github.com/CMeeg/phoria-examples) (including a completed version of this [getting started example](https://github.com/CMeeg/phoria-examples/tree/main/examples/getting-started) if you just want to skip to the end!) available if you would prefer to just copy some code to use as a starting point.

## Prerequisites

* **dotnet** - `v8` or higher
* **Node.js** - `v18.17.1` or `v20.3.0`, `v22.0.0` or higher
  * We recommend using [fnm](https://github.com/Schniz/fnm) or [nvm](https://github.com/nvm-sh/nvm) to manage Node installations
* **Node package manager** - We recommend [pnpm](https://pnpm.io/), but npm will work just as well
* **Code editor** - We recommend [VS Code](https://code.visualstudio.com/)
* **Terminal** - You will need to be able to run CLI tools through a terminal of your choice

## Manual process

You can follow along with this process to create a new Phoria project or to add Phoria to an existing dotnet web app. If you are adding to an existing web app then you will need to adapt the instructions as you go.

Please make sure you have met the [prerequisites](#prerequisites) and then we can continue with:

* [Adding and configuring the Phoria Server](#phoria-server)
* [Adding and configuring the Phoria Web App](#phoria-web-app)
* [Adding Phoria Islands](#adding-phoria-islands)
* [Running the Phoria Web App](#running-the-phoria-web-app)

### Phoria Server

The [Phoria Server](./phoria-server.md) is responsible for rendering your UI components inside Phoria Islands. It is built around [Vite](https://vite.dev/), which means you can enjoy a first class development experience, lightning fast HMR and access to its expansive plugin catalogue and ecosystem.

#### Install Vite

We will scaffold a new Vite Project as our starting point.

> In this getting started guide we will choose React as our UI framework, but you can choose any [supported UI framework](./supported-ui-frameworks.md). We will also be using pnpm, fnm and VS Code on Windows, but feel free to substitute the commands based on your preferences and choice of OS.

Run the following commands in your terminal:

```shell
# Switch to a supported version of Node and pnpm

fnm use 22
corepack install

# Install Vite's React Project Scaffold
pnpm create vite@5.5.5
#> Project name: getting-started
#> Select a framework: React
#> Select a variant: TypeScript

# Change into the directory that has been generated - substitute `getting-started` with whatever project name you chose
cd getting-started

# Install dependencies
pnpm install

# Phoria currently only supports React 19
pnpm add react@rc react-dom@rc
pnpm add -D @types/react@npm:types-react@rc @types/react-dom@npm:types-react-dom@rc
```

> This is optional and won't affect you working with Phoria, but you may want to take a look at the `README.md` that has been generated and apply any recommendations.

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

> The `ui` folder naming convention is a default that can be changed.

Then we need to make some small amendments to the following files:

* Update `tsconfig.app.json`
  * Replace `"include": ["src"]` with `"include": ["ui/src"]`
* Edit `.gitignore`
  * Delete the following lines
    * `.vscode/*`
    * `!.vscode/extensions.json`
    * `*.sln`
  * Add
    * `bin/`
    * `obj/`

#### Add Phoria Server

The Phoria Server is essentially a Node script that starts a [h3](https://h3.unjs.io/) server and listens for CSR (Client Side Rendering) and SSR (Server Side Rendering) requests that are proxied through to it from the dotnet web app.

> The Phoria libraries that you are about to install expose request handlers that are consumed by the server, but you own the server so feel free to extend it and use it for other things should you wish.

Let's start by running the following commands in your terminal:

```shell
# Add required dependencies

pnpm add @phoria/phoria @phoria/phoria-react h3 listhen
pnpm add -D @phoria/vite-plugin-dotnet-dev-certs @types/node tsx
```

Now add the Phoria Server script:

* Create `ui/src/server.ts`
* Update `tsconfig.node.json`
  * Add `ui/src/server.ts` to the `include` array
* Copy the following code into `ui/src/server.ts` and save

```ts
import { pathToFileURL } from "node:url"
import {
  createPhoriaCsrRequestHandler,
  createPhoriaSsrRequestHandler,
  parsePhoriaAppSettings
} from "@phoria/phoria/server"
import { createApp, fromNodeMiddleware, toNodeListener } from "h3"
import { listen } from "listhen"

// Get environment and appsettings

const cwd = process.cwd()
const nodeEnv = process.env.NODE_ENV ?? "development"
const isProduction = nodeEnv === "production"

const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"
const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv })

// Create Vite dev server if not in production environment

const host = appsettings.Server.Host
const port = appsettings.Server.Port ?? 5173

const viteDevServer = isProduction
  ? undefined
  : await import("vite").then((vite) =>
      vite.createServer({
        appType: "custom",
        server: {
          middlewareMode: true,
          host,
          port,
          strictPort: true
        }
      })
    )

// Create http server

const app = createApp()

if (viteDevServer) {
  // Let the Vite dev server handle CSR requests, HMR and SSR

  app.use(fromNodeMiddleware(viteDevServer.middlewares))

  app.use(createPhoriaSsrRequestHandler(() => viteDevServer.ssrLoadModule(appsettings.SsrEntry), appsettings.SsrBase))
} else {
  // Configure the server to handle CSR and SSR requests

  app.use(createPhoriaCsrRequestHandler(appsettings.Base))

  // Without `pathToFileURL` you will receive a `ERR_UNSUPPORTED_ESM_URL_SCHEME` error on Windows
  const ssrEntry = pathToFileURL(`${cwd}/${appsettings.SsrEntry}`).href

  // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
  app.use(createPhoriaSsrRequestHandler(await import(ssrEntry), appsettings.SsrBase))
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

// If using https in dev, we will source the https options from the vite dev server
// If using https in production, we need to source and pass the https options to the listener
const https =
  appsettings.Server.Https && viteDevServer
    ? {
        cert: viteDevServer.config.server?.https?.cert?.toString(),
        key: viteDevServer.config.server?.https?.key?.toString()
      }
    : false

const listener = await listen(toNodeListener(app), {
  hostname: host,
  port,
  https,
  isProd: isProduction,
  qr: false,
  tunnel: false
})

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

> Please feel free to read through the comments in the server script to get a feel for what it does.

#### Add and register a component

Phoria uses a [Component Register](./component-register.md) to find and resolve the components that we want to use in our Islands i.e. where to import them from, if it's a default or named export, and which UI framework to use when rendering.

> For the purpose of this getting started guide we will reuse a component provided by the Vite Project Scaffold, but you can create your own component if you wish.

First we will "create" the component:

* Create a `components/Counter` folder inside `ui/src`
* Move
  * `ui/src/assets/**` -> `ui/src/components/Counter/assets/**`
* Rename and move
  * `ui/src/App.css` -> `ui/src/components/Counter/Counter.css`
  * `ui/src/App.tsx` -> `ui/src/components/Counter/Counter.tsx`
* Edit `ui/src/components/Counter/Counter.tsx`
  * Update `import './App.css'` -> `import './Counter.css'`
  * Rename the `App` function and default export to `Counter`

Then we will register the component:

* Create `ui/src/components/register.ts`
* Copy the following code into `ui/src/components/register.ts` and save

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    /* The default export of this `module` will be used as the `component`.
    If you want to use a named export, you can specify an object with `module`
    and `component` keys. */
    loader: () => import("./Counter/Counter.tsx"),
    framework: "react"
  }
})
```

#### Add Client Entry

The [Client Entry](./client-entry.md) script is the entrypoint for the browser and is responsible for hydrating your registered Phoria Island components using the appropriate CSR strategy provided by your chosen UI framework (or frameworks).

You can also choose to initialise other client-side code here, or import CSS, if you wish.

> Phoria supports SSR-only Islands also in which case there will be no hydration of those components.

* Create `ui/src/entry-client.ts`
* Copy the following code into `ui/src/entry-client.ts` and save

```ts
import "@phoria/phoria-react/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"

/* `PhoriaIsland` is a custom element that will be used to render the components 
that you have registered. */
PhoriaIsland.register()
```

#### Add Server Entry

The [Server Entry](./server-entry.md) script is the entrypoint for the server and is responsible for generating the markup of your registered Phoria Island components using the appropriate SSR strategy provided by your chosen UI framework (or frameworks), which will be appended to the HTTP response stream by the Phoria Web App.

> Phoria supports CSR-only Islands also in which case there will be no SSR of those components.

* Create `ui/src/entry-server.ts`
* Copy the following code into `ui/src/entry-server.ts` and save

```ts
import "@phoria/phoria-react/server"
import "./components/register"
import { serverEntry } from "@phoria/phoria/server"

/* `serverEntry` is used by the Phoria Server to render the components that you
have registered. */
export { serverEntry }
```

#### Configure Vite

In a development environment the Phoria Server will use the Vite Dev Server so that it can take advantage of the many [features](https://vite.dev/guide/features.html) that Vite provides to give you a great dev experience.

Phoria provides several plugins that aim to make it painless to add to your Vite configuration.

> Vite is also used to build your CSR and SSR bundles and your server for production, but the topics of [building for production](./build-for-production.md) and [deployment](./deployment.md) are covered in separate guides.

Replace the contents of `vite.config.ts` with the following code and save:

```ts
import { phoriaReact } from "@phoria/phoria-react/vite"
import { phoria } from "@phoria/phoria/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

// https://vite.dev/config/
export default defineConfig({
  publicDir: "public",
  plugins: [
    /* This plugin is not required for Phoria to work, but we will be using https
    via `dotnet dev-certs` in our examples and this plugin makes it easy to share
    `dotnet dev-certs` with our Vite Dev Server. */
    dotnetDevCerts(),
    /* The primary purpose of the core plugin is to take care of configuration.
    When we set up our Phoria Web App these settings will come from your dotnet
    appseetings file(s), but for now we need to set them here. */
    phoria(),
    /* Each supported UI framework has its own plugin and internally registers
    the Vite framework plugin for you e.g. the `@vitejs/plugin-react` plugin
    in this case */
    phoriaReact()
  ]
})
```
And you will need to update your `package.json` file to run the Phoria Server:

* Update the `dev` script to
  * `"dev": "tsx ./ui/src/server.ts"`

### Phoria Web App

Now we will set up the [Phoria Web App](./phoria-web-app.md), which is just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured.

#### Create a dotnet web app

Run the following commands in your terminal:

> The solution and project names used in this guide are just examples and you can of course use whatever names you like.

```shell
# Create a dotnet web app
dotnet new webapp --name WebApp --no-restore --framework net8.0 --output .

# Create a solution file
dotnet new sln --name GettingStarted --output .

# Add the web app project to the solution
dotnet sln add WebApp.csproj

# Add the Phoria NuGet package to the web app
dotnet add WebApp.csproj package Phoria
```

> We have created a Razor Pages web app, but you could use MVC also if you prefer.

#### Configure the web app

Phoria is added to your app via the `WebApplicationBuilder` and configured in `appsettings.json`.

> Phoria can be configured programatically via the `WebApplicationBuilder`, but it's recommended to use `appsettings.json` because then the `phoria` Vite plugin and the Phoria `server.ts` can read and share that configuration.

Either copy and paste the code below or edit your `Program.cs` file to add the lines that are preceeded by a comment:

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

app.MapRazorPages();

// WebSockets are required for the Vite Dev Server's HMR (Hot Module Reload) feature
if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
}

// Use the Phoria services
app.UsePhoria();

app.Run();
```

Add the following section to your `appsettings.json` file:

```json
{
  // ... Existing settings ...

  // Add the Phoria section
  "Phoria": {
    "Entry": "ui/src/entry-client.ts",
    "SsrEntry": "ui/src/entry-server.ts",
    "Server": {
      "Https": true
    }
  }
}
```

> Any [configuration](./configuration.md) option that is not set explicitly will fallback to a default value except for `Entry` and `SsrEntry` because there are no sensible defaults for these options. The `Https` option will default to `false` so we are setting it here because we want our Phoria Server to use `https`.

#### Add the Phoria Tag Helpers

The `Phoria` NuGet package contains a set of [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro) for:

* Rendering script and style tags from your [Client Entry](#add-client-entry) script
* Rendering [preload directives](https://vite.dev/guide/ssr#generating-preload-directives) for your scripts and styles to avoid a flash of unstyled content (FOUC)
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

> The `component` name must match the name of the component we added to the [Component Register](#add-and-register-a-component). The `client` directive is set to `Client.Load` because we want this component to hydrate on page load. Please see the [Phoria Islands](./phoria-islands.md) guide for a description of available client directives and other options.

### Running the Phoria Web App

Now we can run our app and see Phoria in action.

From your terminal run:

```shell
# Add dev certs
dotnet dev-certs https

# Start the Phoria Server
pnpm run dev

# Start the Phoria Web App (you will need to run this in a separate terminal instance/tab to the Phoria Server)
dotnet run --project WebApp.csproj --launch-profile https
```

> The `dotnet run` command doesn't automatically launch the browser unfortunately, but you can find the URL for the web app in the terminal output or by looking in your `launchSettings.json` file.

Now you will be able to navigate to the web app in your browser and:

* See the `Counter` component rendered via React
* See the styles and assets imported by the React component
  * Though granted it doesn't look great because it's all mixed in with the styles from the dotnet web app!
* Click on the button that reads `count is 0` to see that the component has hydrated and is reacting to state changes
* Make a change to the `Counter.tsx` component to see HMR working