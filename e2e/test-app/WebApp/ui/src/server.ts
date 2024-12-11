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

const nodeEnv = process.env.NODE_ENV ?? "development"
const isProduction = nodeEnv === "production"

const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv })

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

	listenOptions.hostname = appsettings.Server.Host
	listenOptions.port = appsettings.Server.Port ?? 5173

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
