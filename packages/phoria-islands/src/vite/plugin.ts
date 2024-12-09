import type { PluginOption, UserConfig, EnvironmentOptions } from "vite"
import { type PhoriaAppSettings, parsePhoriaAppSettings } from "~/server/appsettings"

interface PhoriaPluginOptions {
	appsettings: Partial<PhoriaAppSettings>
}

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

function setBuild(config: UserConfig, appsettings: Partial<PhoriaAppSettings>) {
	if (typeof appsettings.Entry === "undefined") {
		return
	}

	if (typeof config.build === "undefined") {
		config.build = {
			rollupOptions: {
				input: appsettings.Entry
			}
		}

		return
	}

	if (typeof config.build.rollupOptions === "undefined") {
		config.build.rollupOptions = {
			input: appsettings.Entry
		}

		return
	}

	if (typeof config.build.rollupOptions.input === "undefined") {
		config.build.rollupOptions.input = appsettings.Entry

		return
	}

	if (typeof config.build.rollupOptions.input === "string") {
		config.build.rollupOptions.input = [config.build.rollupOptions.input, appsettings.Entry]

		return
	}

	if (Array.isArray(config.build.rollupOptions.input)) {
		config.build.rollupOptions.input.push(appsettings.Entry)

		return
	}

	if (typeof config.build.rollupOptions.input === "object") {
		config.build.rollupOptions.input = {
			...config.build.rollupOptions.input,
			"phoria-client-entry": appsettings.Entry
		}

		return
	}
}

function setSsrEnvironment(options: EnvironmentOptions) {
	const external = ["@phoria/phoria"]

	options.resolve ??= {}

	if (typeof options.resolve.external === "undefined") {
		options.resolve.external = external

		return
	}

	if (Array.isArray(options.resolve.external)) {
		options.resolve.external.push(...external)

		return
	}
}

function phoriaPlugin(options?: Partial<PhoriaPluginOptions>): PluginOption {
	return {
		name: "phoria",
		config: async (config) => {
			const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "Development"

			const appsettings = await parsePhoriaAppSettings({
				environment: dotnetEnv,
				inlineSettings: options?.appsettings
			})

			config.environments ??= {}
			config.environments.ssr ??= {}

			setRoot(config, appsettings)
			setBase(config, appsettings)
			setServer(config, appsettings)
			setBuild(config, appsettings)
		},
		configEnvironment(name, options) {
			if (name === "ssr") {
				setSsrEnvironment(options)
			}
		}
	}
}

export { phoriaPlugin as phoria }

export type { PhoriaPluginOptions }
