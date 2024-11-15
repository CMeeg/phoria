import { createApp, fromNodeMiddleware, toNodeListener } from "h3"
import { listen } from "listhen"
import { getPhoriaAppSettings, createPhoriaRequestHandler } from "@meeg/phoria/server"

// Get environment and appsettings

const nodeEnv = process.env.NODE_ENV || "development"
const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT
const isProduction = nodeEnv === "production"
const appsettings = await getPhoriaAppSettings(process.cwd(), dotnetEnv)

// Create Vite dev server if not in production environment

const viteDevServer = isProduction
	? undefined
	: await import("vite").then((vite) =>
			vite.createServer({
				server: {
					middlewareMode: true,
					strictPort: true
				},
				appType: "custom"
			})
		)

// Create http server

const app = createApp()

if (viteDevServer) {
	app.use(fromNodeMiddleware(viteDevServer.middlewares))
}

// Handle ssr requests

const devServerEntryPath = "src/entry-server.tsx"
const prodServerEntryPath = "dist/server/entry-server.js"

const phoriaRequestHandler = createPhoriaRequestHandler(
	viteDevServer ? () => viteDevServer.ssrLoadModule(devServerEntryPath) : await import(prodServerEntryPath),
	appsettings.Ssr?.Base
)

app.use(phoriaRequestHandler)

// Handle errors

app.options.onError = (error) => {
	const err = error instanceof Error ? error : new Error("Unknown error", { cause: error })
	viteDevServer?.ssrFixStacktrace(err)

	console.log(err.stack)
}

// Start server

// TODO: This was used by `sirv` (static file server) - still needed with h3/listhen?
// const base = appsettings.Phoria.Base

const port = appsettings.Server?.Port ?? 5173

// TODO: How do we put this in watch mode when in dev?
const listener = await listen(toNodeListener(app), {
	port,
	isProd: isProduction
})

export { app, listener }
