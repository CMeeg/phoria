import { dirname } from "node:path"
import { fileURLToPath } from "node:url"
import { logs, SeverityNumber } from "@opentelemetry/api-logs"
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http"
import { defaultResource, resourceFromAttributes } from "@opentelemetry/resources"
import { ConsoleLogRecordExporter, LoggerProvider, SimpleLogRecordProcessor } from "@opentelemetry/sdk-logs"
import { SEMRESATTRS_SERVICE_NAME } from "@opentelemetry/semantic-conventions"
import {
  createPhoriaCsrRequestHandler,
  createPhoriaDevCsrRequestHandler,
  createPhoriaDevSsrRequestHandler,
  createPhoriaSsrRequestHandler,
  createPhoriaViteDevServer,
  type PhoriaLogger,
  parsePhoriaAppSettings,
} from "@phoria/phoria/server"
import { createApp, toNodeListener } from "h3"
import { type ListenOptions, listen } from "listhen"

const hasOtlpEndpoint = Boolean(process.env.OTEL_EXPORTER_OTLP_ENDPOINT)
const serverResource = defaultResource().merge(resourceFromAttributes({ [SEMRESATTRS_SERVICE_NAME]: "phoria-server" }))
const loggerProvider = new LoggerProvider({
  resource: serverResource,
  processors: [
    new SimpleLogRecordProcessor({
      exporter: hasOtlpEndpoint ? new OTLPLogExporter() : new ConsoleLogRecordExporter(),
    }),
  ],
})
logs.setGlobalLoggerProvider(loggerProvider)
const logger = logs.getLogger("phoria-server")

function log(event: string, severityNumber: SeverityNumber, attributes: Record<string, unknown> = {}) {
  logger.emit({
    severityNumber,
    severityText: SeverityNumber[severityNumber],
    body: event,
    eventName: event,
    attributes: {
      event,
      ...Object.fromEntries(
        Object.entries(attributes)
          .filter(([, value]) => value !== undefined)
          .map(([key, value]) => [key, value instanceof Error ? value.message : String(value)]),
      ),
    },
  })
}

const phoriaLogger: PhoriaLogger = {
  info: (message, data) => log(message, SeverityNumber.INFO, data),
  warn: (message, data) => log(message, SeverityNumber.WARN, data),
  error: (message, data) => log(message, SeverityNumber.ERROR, data),
}

const __dirname = dirname(fileURLToPath(import.meta.url))
const nodeEnv = process.env.NODE_ENV ?? "development"
const isProduction = nodeEnv === "production"
const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"
const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv, cwd: __dirname })

const viteDevServer = isProduction ? undefined : await createPhoriaViteDevServer(import("vite"))
const app = createApp()

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
  log("server.error", SeverityNumber.ERROR, {
    "error.message": err.message,
    "error.stack": err.stack,
    "error.cause": err.cause === undefined ? undefined : String(err.cause),
  })
}

const listenOptions: Partial<ListenOptions> = {
  https: false,
  isProd: isProduction,
  qr: false,
  tunnel: false,
  hostname: appsettings.server.host,
  port: appsettings.server.port ?? 5173,
}

if (viteDevServer?.config.server?.https) {
  listenOptions.https = {
    cert: viteDevServer.config.server.https.cert?.toString(),
    key: viteDevServer.config.server.https.key?.toString(),
  }
}

// NOTE: If using https in production, you will need to source and pass the https options to the listener
const listener = await listen(toNodeListener(app), listenOptions)
log("server.started", SeverityNumber.INFO)

function shutdown(signal: NodeJS.Signals) {
  log("server.shutdown.started", SeverityNumber.INFO, { signal })
  void listener.close().then(async () => {
    log("server.shutdown.completed", SeverityNumber.INFO)
    await loggerProvider.forceFlush()
    await loggerProvider.shutdown()
    process.exit(0)
  })
  listener.server.closeIdleConnections()
  setTimeout(() => {
    log("server.shutdown.forced", SeverityNumber.FATAL, { signal })
    void loggerProvider.forceFlush().finally(() => process.exit(1))
  }, 5000)
}

process.on("SIGTERM", shutdown)
process.on("SIGINT", shutdown)

export { app, listener }
