import type { PluginOption, UserConfig, EnvironmentOptions, BuildEnvironmentOptions } from "vite"
import { type PhoriaAppSettings, parsePhoriaAppSettings } from "~/server/appsettings"

const pluginName = "phoria"

const environment = {
	client: "client",
	ssr: "ssr"
} as const

const defaultOutDir = "dist"

function setRoot(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	if (typeof config.root === "undefined") {
		config.root = appsettings.Root
	}
}

function setBase(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	if (typeof config.base === "undefined") {
		config.base = appsettings.Base
	}
}

function setServer(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	const options: typeof config.server = {
		...config.server
	}

	if (typeof options.host === "undefined") {
		options.host = appsettings.Server?.Host
	}

	if (typeof options.port === "undefined") {
		options.port = appsettings.Server?.Port

		if (typeof appsettings.Server?.Port !== "undefined") {
			options.strictPort = true
		}
	}

	config.server = options
}

function setEntry(options: BuildEnvironmentOptions, root?: string, entryFile?: string) {
	if (typeof entryFile === "undefined") {
		return
	}

	const input = root ? `${root}/${entryFile}` : entryFile

	options.rollupOptions = {
		input
	}
}

function setClientEnvironment(options: EnvironmentOptions, appsettings: Partial<PhoriaAppSettings>) {
	// Set build options

	options.build ??= {}
	options.build.manifest ??= true
	options.build.ssrManifest ??= true
	options.build.emptyOutDir ??= true
	options.build.outDir ??= `${appsettings.Build?.OutDir ?? defaultOutDir}/${pluginName}/${environment.client}`

	setEntry(options.build, appsettings.Root, appsettings.Entry)
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
	options.build.ssr ??= true
	options.build.emptyOutDir ??= true
	options.build.outDir ??= `${appsettings.Build?.OutDir ?? defaultOutDir}/${pluginName}/${environment.ssr}`

	setEntry(options.build, appsettings.Root, appsettings.SsrEntry)
}

interface PhoriaPluginOptions {
	appsettings: Partial<PhoriaAppSettings>
}

function phoriaPlugin(options?: Partial<PhoriaPluginOptions>): PluginOption {
	let appsettings: Partial<PhoriaAppSettings> = {}

	return {
		name: pluginName,
		config: async (config) => {
			const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"

			appsettings = await parsePhoriaAppSettings({
				environment: dotnetEnv,
				inlineSettings: options?.appsettings
			})

			config.environments ??= {}
			config.environments[environment.client] ??= {}
			config.environments[environment.ssr] ??= {}

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
			}
		}
	}
}

export { phoriaPlugin as phoria }

export type { PhoriaPluginOptions }
