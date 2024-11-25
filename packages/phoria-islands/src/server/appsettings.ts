import { readFile } from "node:fs/promises"
import { up } from "empathic/find"
import { safeDestr } from "destr"
import { defu } from "defu"

interface AppSettings {
	Phoria?: Partial<PhoriaAppSettings>
}

interface PhoriaAppSettings {
	Root: string
	Base: string
	Entry: string
	SsrBase: string
	SsrEntry: string
	Server: PhoriaServerAppSettings
	Build: PhoriaBuildAppSettings
}

interface PhoriaServerAppSettings {
	Host: string
	Port?: number
}

interface PhoriaBuildAppSettings {
	OutDir: string
}

async function parseAppSettings(
	path: string,
	cwd: string,
	encoding: BufferEncoding
): Promise<Partial<PhoriaAppSettings>> {
	const appsettingsPath = up(path, { cwd })

	if (typeof appsettingsPath !== "string") {
		return {}
	}

	try {
		const appsettingsContent = await readFile(appsettingsPath, { encoding })

		const appsettings = safeDestr<AppSettings>(appsettingsContent)

		return appsettings.Phoria ?? {}
	} catch (error) {
		throw new Error(`Failed to parse appsettings file: ${appsettingsPath}`, { cause: error })
	}
}

interface PhoriaAppSettingsOptions {
	fileName: string
	encoding: BufferEncoding
	cwd: string
	environment?: string
}

const defaultAppsettingsOptions: PhoriaAppSettingsOptions = {
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

// TODO: Could maybe make this more of a generic function that supports getting appsettings for any app, and add support for filtering by section e.g. in the case of Phoria we only want the Phoria section
async function getPhoriaAppSettings(options?: Partial<PhoriaAppSettingsOptions>): Promise<Partial<PhoriaAppSettings>> {
	const opts = defu(options, defaultAppsettingsOptions)

	const appsettings = await parseAppSettings(opts.fileName, opts.cwd, opts.encoding)

	const envAppsettings =
		typeof opts.environment === "string"
			? await parseAppSettings(getEnvAppsettingsFileName(opts.fileName, opts.environment), opts.cwd, opts.encoding)
			: {}

	return defu(envAppsettings, appsettings)
}

// Defaults here must be in sync with the defaults set in `Phoria/PhoriaOptions.cs`
const defaultAppsettings: Partial<PhoriaAppSettings> = {
	Root: "ui",
	Base: "/ui",
	SsrBase: "/ssr",
	Server: {
		Host: "localhost",
		Port: 5173
	},
	Build: {
		OutDir: "dist"
	}
}

async function parsePhoriaAppSettings(options?: Partial<PhoriaAppSettingsOptions>): Promise<PhoriaAppSettings> {
	const appsettings = await getPhoriaAppSettings(options)

	const parsedAppSettings = defu(appsettings, defaultAppsettings) as PhoriaAppSettings

	if (!parsedAppSettings.Entry) {
		throw new Error("`Entry` is required in `Phoria` app settings.")
	}

	if (!parsedAppSettings.SsrEntry) {
		throw new Error("`SsrEntry` is required in `Phoria` app settings.")
	}

	return parsedAppSettings
}

export { getPhoriaAppSettings, parsePhoriaAppSettings }

export type { PhoriaAppSettings }
