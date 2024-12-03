import { existsSync } from "node:fs"
import { readFile } from "node:fs/promises"
import { join } from "node:path"
import { safeDestr } from "destr"
import { up } from "empathic/find"
import { x } from "tinyexec"
import type { PluginOption, UserConfig } from "vite"

interface DotnetDevCertsPluginOptions {
	basePath?: string
	certificateName?: string
	cwd: string
}

const defaultOptions: DotnetDevCertsPluginOptions = {
	cwd: process.cwd()
}

function getDevCertsBasePath(): string {
	if (typeof process.env.APPDATA === "string" && process.env.APPDATA !== "") {
		return join(process.env.APPDATA, "ASP.NET", "https")
	}

	return join(process.env.HOME ?? "~", ".aspnet", "https")
}

interface PackageJson {
	name: string
}

async function getPackageName(cwd: string): Promise<string | undefined> {
	const pkgPath = up("package.json", { cwd })

	if (typeof pkgPath === "undefined") {
		throw new Error("Could not find a package.json file in the current working directory.")
	}

	let name: string | undefined

	try {
		const pkgContent = await readFile(pkgPath, { encoding: "utf8" })

		const pkg = safeDestr<PackageJson>(pkgContent)

		name = pkg.name
	} catch (error) {
		throw new Error(`Failed to parse package.json file: ${pkgPath}`, { cause: error })
	}

	if (!name) {
		throw new Error(`The package.json file does not contain a name: ${pkgPath}`)
	}

	return name.replaceAll("@", "").replaceAll("/", "_").replaceAll(".", "-")
}

interface Certificate {
	cert: string
	key: string
}

async function execDotnetDevCerts(basePath: string, certificateName: string): Promise<Certificate> {
	if (!existsSync(basePath)) {
		throw new Error(`The base path for the dotnet dev certs does not exist: ${basePath}`)
	}

	const certFilePath = join(basePath, `${certificateName}.pem`)
	const keyFilePath = join(basePath, `${certificateName}.key`)

	const certificate = { cert: certFilePath, key: keyFilePath }

	if (existsSync(certFilePath) && existsSync(keyFilePath)) {
		return certificate
	}

	const result = await x("dotnet", [
		"dev-certs",
		"https",
		"--export-path",
		certFilePath,
		"--format",
		"Pem",
		"--no-password"
	])

	if (result.exitCode !== 0) {
		throw new Error(`Failed to generate dotnet dev certs at "${certFilePath}": ${result.stderr}`)
	}

	return certificate
}

function setServer(config: UserConfig, certificate: Certificate) {
	if (typeof config.server === "undefined") {
		config.server = {
			https: certificate
		}

		return
	}

	if (typeof config.server.https === "undefined") {
		config.server.https = certificate
	}
}

function dotnetDevCertsPlugin(options?: Partial<DotnetDevCertsPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	return {
		name: "dotnet-dev-certs",
		config: async (config, { mode }) => {
			if (mode !== "development") {
				return
			}

			const basePath = opts.basePath ?? getDevCertsBasePath()

			if (!basePath) {
				throw new Error(
					"Could not determine the base path for the dotnet dev certs. Please specify it in the plugin options."
				)
			}

			const certificateName = opts.certificateName ?? (await getPackageName(opts.cwd))

			if (!certificateName) {
				throw new Error("Could not determine a certificate name. Please specify it in the plugin options.")
			}

			const certificate = await execDotnetDevCerts(basePath, certificateName)

			setServer(config, certificate)
		}
	}
}

export { dotnetDevCertsPlugin as dotnetDevCerts }

export type { DotnetDevCertsPluginOptions }
