import { logs, SeverityNumber } from "@opentelemetry/api-logs"
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http"
import { ConsoleLogRecordExporter, LoggerProvider, SimpleLogRecordProcessor } from "@opentelemetry/sdk-logs"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	parsePhoriaAppSettings
} from "@phoria/phoria/server"
import { createApp, toNodeListener } from "h3"
import { type ListenOptions, listen } from "listhen"

const hasOtlpEndpoint = Boolean(process.env.OTEL_EXPORTER_OTLP_ENDPOINT)
const loggerProvider = new LoggerProvider({
	processors: [
		new SimpleLogRecordProcessor({
			exporter: hasOtlpEndpoint ? new OTLPLogExporter() : new ConsoleLogRecordExporter()
		})
	]
})
logs.setGlobalLoggerProvider(loggerProvider)
const logger = logs.getLogger("phoria-server")

function log(event: string, severityNumber: SeverityNumber, attributes: Record<string, string | undefined> = {}) {
	logger.emit({
		severityNumber,
		severityText: SeverityNumber[severityNumber],
		body: event,
		attributes: {
			event,
			...Object.fromEntries(Object.entries(attributes).filter(([, value]) => value !== undefined))
		}
	})
}

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

	log("server.error", SeverityNumber.ERROR, {
		"error.message": err.message,
		"error.stack": err.stack,
		"error.cause": err.cause === undefined ? undefined : String(err.cause)
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
log("server.started", SeverityNumber.INFO)

// Handle server shutdown

function shutdown(signal: NodeJS.Signals) {
	log("server.shutdown.started", SeverityNumber.INFO, { signal })

	void listener.close().then(async () => {
		log("server.shutdown.completed", SeverityNumber.INFO)
		await loggerProvider.forceFlush()
		await loggerProvider.shutdown()
		process.exit(0)
	})

	// Drop idle keep-alive connections so close() doesn't wait for them
	listener.server.closeIdleConnections()

	// Force shutdown after 5 seconds
	setTimeout(() => {
		log("server.shutdown.forced", SeverityNumber.FATAL, { signal })
		void loggerProvider.forceFlush().finally(() => process.exit(1))
	}, 5000)
}

process.on("SIGTERM", (signal) => shutdown(signal))
process.on("SIGINT", (signal) => shutdown(signal))

export { app, listener }
