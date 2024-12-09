import { pathToFileURL } from "node:url"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaSsrRequestHandler,
	parsePhoriaAppSettings
} from "@phoria/phoria/server"
import { createApp, fromNodeMiddleware, toNodeListener } from "h3"
import { type ListenOptions, listen } from "listhen"
import { isRunnableDevEnvironment } from "vite"

// Get environment and appsettings

const cwd = process.cwd()
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

	const environment = viteDevServer.environments.ssr

	if (!isRunnableDevEnvironment(environment)) {
		throw new Error("Vite dev server does not have a runnable SSR environment.")
	}

	app.use(fromNodeMiddleware(viteDevServer.middlewares))

	app.use(createPhoriaSsrRequestHandler(() => environment.runner.import(appsettings.SsrEntry), appsettings.SsrBase))
} else {
	// Configure the server to handle CSR and SSR requests

	app.use(createPhoriaCsrRequestHandler(appsettings.Base))

	// Without `pathToFileURL` you will receive a `ERR_UNSUPPORTED_ESM_URL_SCHEME` error on Windows
	const ssrEntry = pathToFileURL(`${cwd}/${appsettings.Root}/${appsettings.SsrEntry}`).href

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

	// If using https in production, you will need to source and pass the https options to the listener
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
