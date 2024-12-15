import { readFile } from "node:fs/promises"
import { defu } from "defu"
import { safeDestr } from "destr"
import { up } from "empathic/find"

interface AppSettings {
	phoria?: Partial<PhoriaAppSettings>
}

interface PhoriaAppSettings {
	root: string
	base: string
	entry: string
	ssrBase: string
	ssrEntry: string
	server: PhoriaServerAppSettings
	build: PhoriaBuildAppSettings
}

interface PhoriaServerAppSettings {
	host: string
	port?: number
	https: boolean
}

interface PhoriaBuildAppSettings {
	outDir: string
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

		return appsettings.phoria ?? {}
	} catch (error) {
		throw new Error(`Failed to parse appsettings file: ${appsettingsPath}`, { cause: error })
	}
}

interface PhoriaAppSettingsOptions {
	fileName: string
	encoding: BufferEncoding
	cwd: string
	environment?: string
	inlineSettings?: Partial<PhoriaAppSettings>
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

	const baseappsettings = defu(appsettings, opts.inlineSettings ?? {})

	const envAppsettings =
		typeof opts.environment === "string"
			? await parseAppSettings(getEnvAppsettingsFileName(opts.fileName, opts.environment), opts.cwd, opts.encoding)
			: {}

	return defu(envAppsettings, baseappsettings)
}

// Defaults here must be in sync with the defaults set in `Phoria/PhoriaOptions.cs`
const defaultAppsettings: Partial<PhoriaAppSettings> = {
	root: "ui",
	base: "/ui",
	ssrBase: "/ssr",
	server: {
		host: "localhost",
		port: 5173,
		https: false
	},
	build: {
		outDir: "dist"
	}
}

async function parsePhoriaAppSettings(options?: Partial<PhoriaAppSettingsOptions>): Promise<PhoriaAppSettings> {
	const appsettings = await getPhoriaAppSettings(options)

	const parsedAppSettings = defu(appsettings, defaultAppsettings) as PhoriaAppSettings

	if (!parsedAppSettings.entry) {
		throw new Error("`entry` is required in `Phoria` app settings.")
	}

	if (!parsedAppSettings.ssrEntry) {
		throw new Error("`ssrEntry` is required in `Phoria` app settings.")
	}

	return parsedAppSettings
}

export { getPhoriaAppSettings, parsePhoriaAppSettings }

export type { PhoriaAppSettings }
