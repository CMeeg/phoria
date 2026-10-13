import { describe, expect, it } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { getPhoriaObservabilityAppSettings, type PhoriaObservabilityAppSettingsInput } from "./appsettings"

describe("getPhoriaObservabilityAppSettings", () => {
	it("applies observability defaults when none are provided", () => {
		const settings = getPhoriaObservabilityAppSettings(createPhoriaOtelAppSettings())

		expect(settings.logging).toBe(false)
		expect(settings.tracing).toEqual({ enabled: false, samplingRatio: 0.1 })
		expect(settings.metrics).toBe(false)
	})

	it("merges partial observability settings over the defaults", () => {
		const settings = getPhoriaObservabilityAppSettings(
			createPhoriaOtelAppSettings({ observability: { tracing: { enabled: true } } })
		)

		expect(settings.tracing.enabled).toBe(true)
		expect(settings.tracing.samplingRatio).toBe(0.1)
	})

	it("preserves unknown keys from the input settings", () => {
		const settings = getPhoriaObservabilityAppSettings(
			createPhoriaOtelAppSettings({
				observability: { custom: "value" } as unknown as PhoriaObservabilityAppSettingsInput
			})
		) as unknown as { custom: string }

		expect(settings.custom).toBe("value")
	})
})
