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

async function parseAppSettings(path: string, cwd: string): Promise<Partial<PhoriaAppSettings>> {
	const appsettingsPath = up(path, { cwd })

	if (typeof appsettingsPath !== "string") {
		return {}
	}

	try {
		const appsettingsContent = await readFile(appsettingsPath, { encoding: "utf8" })

		const appsettings = safeDestr<AppSettings>(appsettingsContent)

		return appsettings.Phoria ?? {}
	} catch (error) {
		throw new Error(`Failed to parse appsettings file: ${appsettingsPath}`, { cause: error })
	}
}

// TODO: Could maybe make this more of a generic function that supports getting appsettings for any app, and add support for filtering by section e.g. in the case of Phoria we only want the Phoria section
async function getPhoriaAppSettings(cwd: string, environment?: string): Promise<Partial<PhoriaAppSettings>> {
	// TODO: Need to support or at least cater for different casing of the file name, e.g. appsettings.json, appSettings.json; and also the environment file e.g. appsettings.development.json, appsettings.Development.json
	// TODO: Could use https://unjs.io/packages/scule for case transforms; or could just make it clear that your environment variable and file names must use the same casing
	const appsettings = await parseAppSettings("appsettings.json", cwd)

	const envappsettings =
		typeof environment === "string" ? await parseAppSettings(`appsettings.${environment}.json`, cwd) : {}

	return defu(envappsettings, appsettings)
}

async function parsePhoriaAppSettings(cwd: string, environment?: string): Promise<PhoriaAppSettings> {
	const appsettings = await getPhoriaAppSettings(cwd, environment)

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
