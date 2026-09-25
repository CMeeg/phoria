import type { PhoriaAppSettings } from "@phoria/phoria/server"
import { defu } from "defu"

interface PhoriaObservabilityTracingAppSettings {
	enabled: boolean
	samplingRatio: number
}

interface PhoriaObservabilityAppSettingsInput {
	logging?: boolean
	logHealthChecks?: boolean
	tracing?: Partial<PhoriaObservabilityTracingAppSettings>
	metrics?: boolean
}

interface PhoriaObservabilityAppSettings extends PhoriaObservabilityAppSettingsInput {
	logging: boolean
	tracing: PhoriaObservabilityTracingAppSettings
	metrics: boolean
}

// Defaults here must be in sync with the defaults in `Phoria/PhoriaObservabilityOptions.cs`
const defaultObservabilityAppSettings: PhoriaObservabilityAppSettings = {
	logging: false,
	tracing: {
		enabled: false,
		samplingRatio: 0.1
	},
	metrics: false
}

type PhoriaOtelAppSettings = PhoriaAppSettings<{
	observability?: PhoriaObservabilityAppSettingsInput
}>

function getPhoriaObservabilityAppSettings(appsettings: PhoriaOtelAppSettings): PhoriaObservabilityAppSettings {
	return defu(appsettings.observability, defaultObservabilityAppSettings) as PhoriaObservabilityAppSettings
}

export type { PhoriaObservabilityAppSettings, PhoriaObservabilityAppSettingsInput, PhoriaOtelAppSettings }
export { getPhoriaObservabilityAppSettings }
