import { describe, expect, it, vi } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"

const disabledSettings = createPhoriaOtelAppSettings({
	observability: { logging: false, tracing: { enabled: false, samplingRatio: 0.1 }, metrics: false }
})

describe("createPhoriaObservability", () => {
	it("registers a real tracer provider when tracing is enabled", async () => {
		vi.resetModules()
		const { trace } = await import("@opentelemetry/api")
		const { createPhoriaObservability } = await import("./observability")
		const observability = createPhoriaObservability(
			createPhoriaOtelAppSettings({ observability: { tracing: { enabled: true, samplingRatio: 1 } } })
		)

		const provider = trace.getTracerProvider()
		expect(provider.constructor.name).not.toBe("NoopTracerProvider")
		await observability.shutdown()
	})
	it("returns an object whose shutdown resolves when no signals are enabled", async () => {
		const { createPhoriaObservability } = await import("./observability")
		const observability = createPhoriaObservability(disabledSettings)

		expect(observability).toHaveProperty("shutdown")
		await expect(observability.shutdown()).resolves.toBeUndefined()
	})
})
