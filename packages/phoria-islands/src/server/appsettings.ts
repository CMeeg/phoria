import { readFile } from "node:fs/promises"
import { up } from "empathic/find"
import { safeDestr } from "destr"
import { defu } from "defu"

interface AppSettings {
	Phoria?: Partial<PhoriaAppSettings>
}

interface PhoriaSsrAppSettings {
	Base: string
	Manifest: string
}

interface PhoriaServerAppSettings {
	Host: string
	Port: number
}

interface PhoriaAppSettings {
	Base: string
	Manifest: string
	Ssr: Partial<PhoriaSsrAppSettings>
	Server: Partial<PhoriaServerAppSettings>
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
	const appsettings = await parseAppSettings("appsettings.json", cwd)

	const envappsettings =
		typeof environment === "string" ? await parseAppSettings(`appsettings.${environment}.json`, cwd) : {}

	return defu(envappsettings, appsettings)
}

export { getPhoriaAppSettings }

export type { PhoriaAppSettings }
