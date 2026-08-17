import { isAbsolute, join } from "node:path"
import type { BuildEnvironmentOptions, EnvironmentOptions, PluginOption, UserConfig } from "vite"
import { type PhoriaAppSettings, parsePhoriaAppSettings } from "~/server/appsettings"
import type { PhoriaFrameworkPluginOptions } from "./framework"
import { createPhoriaFrameworkPlugin } from "./framework"

const pluginName = "phoria"

const environment = {
	client: "client",
	server: "server",
	ssr: "ssr"
} as const

const defaultOutDir = "dist"

function setRoot(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	if (typeof config.root === "undefined") {
		config.root = appsettings.root
	}
}

function setBase(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	if (typeof config.base === "undefined") {
		config.base = appsettings.base
	}
}

function setServer(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	const options: typeof config.server = {
		...config.server
	}

	if (typeof options.host === "undefined") {
		options.host = appsettings.server?.host
	}

	if (typeof options.port === "undefined") {
		options.port = appsettings.server?.port

		if (typeof appsettings.server?.port !== "undefined") {
			options.strictPort = true
		}
	}

	config.server = options
}

function setEntry(options: BuildEnvironmentOptions, entryFile?: string) {
	if (typeof entryFile === "undefined") {
		return
	}

	// `entryFile` is relative to the Vite config root, which `setRoot` sets to `appsettings.root`

	options.rolldownOptions = {
		...options.rolldownOptions,
		input: entryFile
	}
}

function setClientEnvironment(options: EnvironmentOptions, appsettings: Partial<PhoriaAppSettings>) {
	// Set build options

	options.build ??= {}
	options.build.manifest = true
	options.build.ssrManifest = true
	options.build.emptyOutDir ??= true
	options.build.outDir = `${appsettings.build?.outDir ?? defaultOutDir}/${pluginName}/${environment.client}`

	setEntry(options.build, appsettings.entry)
}

function setSsrEnvironment(options: EnvironmentOptions, appsettings: Partial<PhoriaAppSettings>) {
	// Set resolve options

	const external = ["@phoria/phoria"]

	options.resolve ??= {}

	if (typeof options.resolve.external === "undefined") {
		options.resolve.external = external
	} else if (Array.isArray(options.resolve.external)) {
		options.resolve.external.push(...external)
	}

	// Set build options

	options.build ??= {}
	options.build.ssr = true
	options.build.emptyOutDir ??= true
	options.build.copyPublicDir ??= false
	options.build.outDir = `${appsettings.build?.outDir ?? defaultOutDir}/${pluginName}/${environment.ssr}`

	setEntry(options.build, appsettings.ssrEntry)
}

function setServerEnvironment(
	options: EnvironmentOptions,
	appsettings: Partial<PhoriaAppSettings>,
	serverEntry: string
) {
	// Set resolve options

	const external = ["@phoria/phoria"]

	options.resolve ??= {}

	if (typeof options.resolve.external === "undefined") {
		options.resolve.external = external
	} else if (Array.isArray(options.resolve.external)) {
		options.resolve.external.push(...external)
	}

	// Set build options

	options.build ??= {}
	options.build.ssr = true
	options.build.target ??= "es2022"
	options.build.copyPublicDir ??= false
	options.build.emptyOutDir ??= true
	options.build.outDir = `${appsettings.build?.outDir ?? defaultOutDir}/${environment.server}`

	setEntry(options.build, serverEntry)
}

function parseWorkingDirectory(cwd: string | undefined) {
	if (typeof cwd === "undefined") {
		return process.cwd()
	}

	if (isAbsolute(cwd)) {
		return cwd
	}

	return join(process.cwd(), cwd)
}

interface PhoriaPluginOptions {
	cwd: string
	appsettings: Partial<PhoriaAppSettings>
	/** Entry for the Phoria Server bundle. Set to `false` to skip building it. */
	serverEntry: string | false
}

const defaultOptions: Pick<PhoriaPluginOptions, "serverEntry"> = {
	serverEntry: "src/server.ts"
}

function phoriaPlugin(options?: Partial<PhoriaPluginOptions>): PluginOption {
	const cwd = parseWorkingDirectory(options?.cwd)
	const serverEntry = options?.serverEntry ?? defaultOptions.serverEntry
	let appsettings: Partial<PhoriaAppSettings> = {}

	return {
		name: pluginName,
		config: async (config) => {
			const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"

			appsettings = await parsePhoriaAppSettings({
				environment: dotnetEnv,
				cwd,
				inlineSettings: options?.appsettings
			})

			config.environments ??= {}
			config.environments[environment.client] ??= {}
			config.environments[environment.ssr] ??= {}

			if (serverEntry !== false) {
				config.environments[environment.server] ??= {}
			}

			setRoot(config, appsettings)
			setBase(config, appsettings)
			setServer(config, appsettings)
		},
		configEnvironment(name, options) {
			switch (name) {
				case environment.client:
					setClientEnvironment(options, appsettings)
					break
				case environment.ssr:
					setSsrEnvironment(options, appsettings)
					break
				case environment.server:
					// Safe: configEnvironment is only invoked for environments present in
					// config.environments, and `server` is only registered there when
					// serverEntry !== false (see the `config` hook above).
					setServerEnvironment(options, appsettings, serverEntry as string)
					break
			}
		},
		buildApp: {
			order: "pre",
			async handler(builder) {
				const order = [environment.client, environment.ssr, environment.server]

				for (const name of order) {
					const env = builder.environments[name]

					if (env && !env.isBuilt) {
						await builder.build(env)
					}
				}
			}
		}
	}
}

export type { PhoriaFrameworkPluginOptions, PhoriaPluginOptions }
export { createPhoriaFrameworkPlugin, phoriaPlugin as phoria }
