# @phoria/opentelemetry

OpenTelemetry instrumentation for [Phoria Server](https://github.com/CMeeg/phoria).

Owns the Node-side OpenTelemetry setup for Phoria Server sidecars: configuration parsing from `appsettings` files, an OTel log adapter, `NodeSDK` setup for traces, metrics and logs, and an h3 span-enrichment hook.

```ts
import {
	createPhoriaLogger,
	createPhoriaObservability,
	withPhoriaOtelInstrumentation
} from "@phoria/opentelemetry"
import { parsePhoriaAppSettings, type PhoriaOtelAppSettings } from "@phoria/phoria/server"
import { createApp, toNodeListener } from "h3"
import { listen } from "listhen"

const appsettings = await parsePhoriaAppSettings<PhoriaOtelAppSettings>({ cwd: __dirname })
const logger = createPhoriaLogger(appsettings)
const observability = createPhoriaObservability(appsettings)

const app = createApp(withPhoriaOtelInstrumentation(appsettings))

const listener = await listen(toNodeListener(app))

async function shutdown(signal: NodeJS.Signals) {
	void listener.close().then(async () => {
		await observability.shutdown()
		process.exit(0)
	})
}
```
