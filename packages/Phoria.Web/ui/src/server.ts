import { createApp, fromNodeMiddleware, toNodeListener } from "h3"
import { listen } from "listhen"
import { pathToFileURL } from "node:url"
import {
	parsePhoriaAppSettings,
	createPhoriaSsrRequestHandler,
	createPhoriaCsrRequestHandler
} from "@meeg/phoria/server"

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
					// TODO: strictPort is not working - Vite is trying to find the next port if the port is already in use - raise issue upstream
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

// TODO: How do we put this in watch mode when in dev?
const listener = await listen(toNodeListener(app), {
	hostname: host,
	port,
	isProd: isProduction,
	qr: false,
	tunnel: false
})

// Handle server shutdown

async function shutdown(signal: NodeJS.Signals) {
	console.log(`Received signal ${signal}. Shutting down server.`)

	await listener.close().then(() => {
		console.log("Server listener closed.")

		process.exit(0)
	})

	// Force shutdown after 5 seconds

	setTimeout(() => {
		console.error("Could not shutdown gracefully. Forcefully shutting down server.")

		process.exit(1)
	}, 5000)
}

process.on("SIGTERM", async (signal) => await shutdown(signal))
process.on("SIGINT", async (signal) => await shutdown(signal))

export { app, listener }
