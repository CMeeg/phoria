import { dirname } from "node:path"
import { fileURLToPath } from "node:url"
import {
  createPhoriaLogger,
  createPhoriaObservability,
  createPhoriaRequestSpanHook,
  parsePhoriaObservabilityAppSettings,
} from "@phoria/opentelemetry"
import {
  createPhoriaCsrRequestHandler,
  createPhoriaDevCsrRequestHandler,
  createPhoriaDevSsrRequestHandler,
  createPhoriaSsrRequestHandler,
  createPhoriaViteDevServer,
  parsePhoriaAppSettings,
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
const observabilitySettings = await parsePhoriaObservabilityAppSettings({ cwd: __dirname, environment: dotnetEnv })
const phoriaLogger = createPhoriaLogger(observabilitySettings)
const observability = createPhoriaObservability(observabilitySettings)

// Create Vite dev server if not in production environment

const viteDevServer = isProduction ? undefined : await createPhoriaViteDevServer(import("vite"))

// Create http server

const app = createApp({ ...createPhoriaRequestSpanHook({ base: appsettings.base, ssrBase: appsettings.ssrBase }) })

if (viteDevServer) {
  // Let the Vite dev server handle CSR requests, HMR and SSR

  app.use(createPhoriaDevCsrRequestHandler(viteDevServer))

  app.use(createPhoriaDevSsrRequestHandler(viteDevServer, appsettings))
} else {
  // Configure the server to handle CSR and SSR requests

  app.use(createPhoriaCsrRequestHandler(appsettings, { logger: phoriaLogger }))

  app.use(createPhoriaSsrRequestHandler(appsettings, { logger: phoriaLogger }))
}

// Handle errors

app.options.onError = (error) => {
  const err = error instanceof Error ? error : new Error("Unknown error", { cause: error })
  viteDevServer?.ssrFixStacktrace(err)

  phoriaLogger.error("server.error", {
    "error.message": err.message,
    "error.stack": err.stack,
    "error.cause": err.cause === undefined ? undefined : String(err.cause),
  })
}

// Start server

const listenOptions: Partial<ListenOptions> = {
  https: false,
  isProd: isProduction,
  qr: false,
  tunnel: false,
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
      key: viteDevServer.config.server.https.key?.toString(),
    }
  }
} else {
  // In production, we will source the listener options from appsettings

  listenOptions.hostname = appsettings.server.host
  listenOptions.port = appsettings.server.port ?? 5173

  // NOTE: If using https in production, you will need to source and pass the https options to the listener
}

const listener = await listen(toNodeListener(app), listenOptions)
phoriaLogger.info("server.started")

// Handle server shutdown

function shutdown(signal: NodeJS.Signals) {
  phoriaLogger.info("server.shutdown.started", { signal })

  void listener.close().then(async () => {
    phoriaLogger.info("server.shutdown.completed")
    await observability.shutdown()
    process.exit(0)
  })

  // Drop idle keep-alive connections so close() doesn't wait for them
  listener.server.closeIdleConnections()

  // Force shutdown after 5 seconds
  setTimeout(() => {
    phoriaLogger.error("server.shutdown.forced", { signal })
    void observability.shutdown().finally(() => process.exit(1))
  }, 5000)
}

process.on("SIGTERM", (signal) => shutdown(signal))
process.on("SIGINT", (signal) => shutdown(signal))

export { app, listener }
