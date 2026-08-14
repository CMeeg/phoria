import { describe, expect, it } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { createPhoriaObservability } from "./observability"

const disabledSettings = createPhoriaOtelAppSettings({
	observability: { logging: false, tracing: { enabled: false, samplingRatio: 0.1 }, metrics: false }
})

describe("createPhoriaObservability", () => {
	it("returns an object whose shutdown resolves when no signals are enabled", async () => {
		const observability = createPhoriaObservability(disabledSettings)

		expect(observability).toHaveProperty("shutdown")
		await expect(observability.shutdown()).resolves.toBeUndefined()
	})
})
