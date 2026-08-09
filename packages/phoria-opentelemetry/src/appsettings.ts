import { readFile } from "node:fs/promises"
import { defu } from "defu"
import { safeDestr } from "destr"
import { up } from "empathic/find"

interface ObservabilityAppSettings {
	phoria?: {
		observability?: Partial<PhoriaObservabilityAppSettings>
	}
}

interface PhoriaObservabilityTracingAppSettings {
	enabled: boolean
	samplingRatio: number
}

interface PhoriaObservabilityAppSettings {
	logging: boolean
	logHealthChecks?: boolean
	tracing: PhoriaObservabilityTracingAppSettings
	metrics: boolean
}

interface PhoriaObservabilityAppSettingsOptions {
	fileName: string
	encoding: BufferEncoding
	cwd: string
	environment?: string
}

const defaultOptions: PhoriaObservabilityAppSettingsOptions = {
	fileName: "appsettings.json",
	encoding: "utf8",
	cwd: process.cwd()
}

function getEnvAppsettingsFileName(fileName: string, environment: string) {
	const lastPeriod = fileName.lastIndexOf(".")
	const extension = fileName.slice(lastPeriod)
	const baseName = fileName.slice(0, lastPeriod)

	return `${baseName}.${environment}${extension}`
}

function getEnvironment(): string {
	return process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? process.env.NODE_ENV ?? "Development"
}

async function parseObservabilityAppSettings(
	path: string,
	cwd: string,
	encoding: BufferEncoding
): Promise<Partial<PhoriaObservabilityAppSettings>> {
	const appsettingsPath = up(path, { cwd })

	if (typeof appsettingsPath !== "string") {
		return {}
	}

	try {
		const appsettingsContent = await readFile(appsettingsPath, { encoding })

		const appsettings = safeDestr<ObservabilityAppSettings>(appsettingsContent)

		return appsettings.phoria?.observability ?? {}
	} catch (error) {
		throw new Error(`Failed to parse appsettings file: ${appsettingsPath}`, { cause: error })
	}
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

async function parsePhoriaObservabilityAppSettings(
	options?: Partial<PhoriaObservabilityAppSettingsOptions>
): Promise<PhoriaObservabilityAppSettings> {
	const opts = defu(options, defaultOptions)
	const environment = opts.environment ?? getEnvironment()

	const baseSettings = await parseObservabilityAppSettings(opts.fileName, opts.cwd, opts.encoding)

	const envSettings = await parseObservabilityAppSettings(
		getEnvAppsettingsFileName(opts.fileName, environment),
		opts.cwd,
		opts.encoding
	)

	return defu(envSettings, baseSettings, defaultObservabilityAppSettings) as PhoriaObservabilityAppSettings
}

export type { PhoriaObservabilityAppSettings }
export { parsePhoriaObservabilityAppSettings }
