import { dirname } from "node:path"
import { fileURLToPath } from "node:url"
import {
	createPhoriaLogger,
	createPhoriaObservability,
	type PhoriaOtelAppSettings,
	withPhoriaOtelInstrumentation
} from "@phoria/opentelemetry"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	createPhoriaViteDevServer,
	parsePhoriaAppSettings
} from "@phoria/phoria/server"
import { createApp, toNodeListener } from "h3"
import { type ListenOptions, listen } from "listhen"

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const nodeEnv = process.env.NODE_ENV ?? "development"
const isProduction = nodeEnv === "production"
const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
const appsettings = await parsePhoriaAppSettings<PhoriaOtelAppSettings>({ environment: dotnetEnv, cwd: __dirname })
const phoriaLogger = createPhoriaLogger(appsettings)
const observability = createPhoriaObservability(appsettings)
const viteDevServer = isProduction ? undefined : await createPhoriaViteDevServer(import("vite"))
const app = createApp(withPhoriaOtelInstrumentation(appsettings))

if (viteDevServer) {
	app.use(createPhoriaDevCsrRequestHandler(viteDevServer))
	app.use(createPhoriaDevSsrRequestHandler(viteDevServer, appsettings))
} else {
	app.use(createPhoriaCsrRequestHandler(appsettings, { logger: phoriaLogger }))
	app.use(createPhoriaSsrRequestHandler(appsettings, { logger: phoriaLogger }))
}

app.options.onError = (error) => {
	const err = error instanceof Error ? error : new Error("Unknown error", { cause: error })
	viteDevServer?.ssrFixStacktrace(err)
	phoriaLogger.error("server.error", { "error.message": err.message, "error.stack": err.stack })
}

const listenOptions: Partial<ListenOptions> = { https: false, isProd: isProduction, qr: false, tunnel: false }
if (viteDevServer) {
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
	listenOptions.hostname = appsettings.server.host
	listenOptions.port = appsettings.server.port ?? 5573
}

const listener = await listen(toNodeListener(app), listenOptions)
phoriaLogger.info("server.started")

function shutdown(signal: NodeJS.Signals) {
	phoriaLogger.info("server.shutdown.started", { signal })
	void listener.close().then(async () => {
		phoriaLogger.info("server.shutdown.completed")
		await observability.shutdown()
		process.exit(0)
	})
	listener.server.closeIdleConnections()
	setTimeout(() => void observability.shutdown().finally(() => process.exit(1)), 5000)
}

process.on("SIGTERM", shutdown)
process.on("SIGINT", shutdown)

export { app, listener }
