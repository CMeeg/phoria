import http from "node:http"
import https from "node:https"
import { describe, expect, it } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { createPhoriaObservability } from "./observability"

const metricsSettings = createPhoriaOtelAppSettings({
	observability: { logging: false, tracing: { enabled: false, samplingRatio: 0.1 }, metrics: true }
})

describe("createPhoriaObservability instrumentation", () => {
	it("patches preloaded node:http and node:https", async () => {
		const pristineHttpEmit = http.Server.prototype.emit
		const pristineHttpsEmit = https.Server.prototype.emit

		const observability = createPhoriaObservability(metricsSettings)

		expect(http.Server.prototype.emit).not.toBe(pristineHttpEmit)
		expect(https.Server.prototype.emit).not.toBe(pristineHttpsEmit)

		await observability.shutdown()
	})
})
