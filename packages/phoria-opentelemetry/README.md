# @phoria/opentelemetry

OpenTelemetry instrumentation for [Phoria Server](https://github.com/CMeeg/phoria).

Owns the Node-side OpenTelemetry setup for Phoria Server sidecars: configuration parsing from `appsettings` files, an OTel log adapter, `NodeSDK` setup for traces, metrics and logs, and an h3 span-enrichment hook.

```ts
import {
	createPhoriaLogger,
	createPhoriaObservability,
	createPhoriaRequestSpanHook,
	parsePhoriaObservabilityAppSettings
} from "@phoria/opentelemetry"
import { createApp, toNodeListener } from "h3"
import { listen } from "listhen"

const appsettings = { base: "/ui", ssrBase: "/ssr" }
const settings = parsePhoriaObservabilityAppSettings({ cwd: __dirname })
const logger = createPhoriaLogger(settings)
const observability = createPhoriaObservability(settings)

const app = createApp({ ...createPhoriaRequestSpanHook(appsettings) })

const listener = await listen(toNodeListener(app))

async function shutdown(signal: NodeJS.Signals) {
	void listener.close().then(async () => {
		await observability.shutdown()
		process.exit(0)
	})
}
```
