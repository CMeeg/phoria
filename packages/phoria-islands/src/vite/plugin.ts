import type { PluginOption, UserConfig } from "vite"
import { parsePhoriaAppSettings, type PhoriaAppSettings } from "~/server/appsettings"

interface PhoriaPluginOptions {
	appsettings: PhoriaAppSettings
}

function setRoot(config: UserConfig, appsettings: PhoriaAppSettings) {
	if (typeof config.root === "undefined") {
		config.root = appsettings.Root
	}
}

function setBase(config: UserConfig, appsettings: PhoriaAppSettings) {
	if (typeof config.base === "undefined") {
		config.base = appsettings.Base
	}
}

function setBuild(config: UserConfig, appsettings: PhoriaAppSettings) {
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
	}
}

function setSsr(config: UserConfig) {
	const external = ["@meeg/phoria"]

	if (typeof config.ssr === "undefined") {
		config.ssr = {
			external
		}

		return
	}

	if (typeof config.ssr.external === "undefined") {
		config.ssr.external = external

		return
	}

	if (Array.isArray(config.ssr.external)) {
		config.ssr.external.push(...external)
	}
}

function phoriaPlugin(options?: Partial<PhoriaPluginOptions>): PluginOption {
	return {
		name: "phoria",
		config: async (config) => {
			const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"

			const appsettings = options?.appsettings
				? options.appsettings
				: await parsePhoriaAppSettings({ environment: dotnetEnv })

			setRoot(config, appsettings)
			setBase(config, appsettings)
			setBuild(config, appsettings)
			setSsr(config)
		}
	}
}

export { phoriaPlugin as phoria }

export type { PhoriaPluginOptions }
