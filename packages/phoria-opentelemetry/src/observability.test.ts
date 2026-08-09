import { describe, expect, it } from "vitest"
import type { PhoriaObservabilitySettings } from "./appsettings"
import { createPhoriaObservability } from "./observability"

const disabledSettings: PhoriaObservabilitySettings = {
	logging: false,
	tracing: {
		enabled: false,
		samplingRatio: 0.1
	},
	metrics: false
}

describe("createPhoriaObservability", () => {
	it("returns an object whose shutdown resolves when no signals are enabled", async () => {
		const observability = createPhoriaObservability(disabledSettings)

		expect(observability).toHaveProperty("shutdown")
		await expect(observability.shutdown()).resolves.toBeUndefined()
	})

	it("returns a stable object on double-call", async () => {
		const first = createPhoriaObservability(disabledSettings)
		const second = createPhoriaObservability(disabledSettings)

		await expect(first.shutdown()).resolves.toBeUndefined()
		await expect(second.shutdown()).resolves.toBeUndefined()
	})
})
