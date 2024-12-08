import type { PluginOption, UserConfig } from "vite"
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
	if (typeof appsettings.Server === "undefined") {
		return
	}

	if (typeof config.server === "undefined") {
		config.server = {
			host: appsettings.Server.Host,
			port: appsettings.Server.Port
		}

		if (typeof appsettings.Server.Port !== "undefined") {
			config.server.strictPort = true
		}

		return
	}

	if (typeof config.server.host === "undefined") {
		config.server.host = appsettings.Server.Host
	}

	if (typeof config.server.port === "undefined") {
		config.server.port = appsettings.Server.Port

		if (typeof appsettings.Server.Port !== "undefined") {
			config.server.strictPort = true
		}
	}
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

function setSsr(config: UserConfig) {
	const external = ["@phoria/phoria"]

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

			setRoot(config, appsettings)
			setBase(config, appsettings)
			setServer(config, appsettings)
			setBuild(config, appsettings)
			setSsr(config)
		}
	}
}

export { phoriaPlugin as phoria }

export type { PhoriaPluginOptions }
