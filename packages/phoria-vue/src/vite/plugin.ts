import type { PluginOption, UserConfig } from "vite"
import vue, { type Options as VueOptions } from "@vitejs/plugin-vue"
import { createFilter, normalizePath } from "@rollup/pluginutils"
import MagicString from "magic-string"

type CreateFilterParams = Parameters<typeof createFilter>

interface PhoriaVuePluginOptions {
	include: CreateFilterParams[0]
	exclude: CreateFilterParams[1]
	cwd: string
	vue?: VueOptions | false
}

const defaultOptions: PhoriaVuePluginOptions = {
	include: ["**/*.vue"],
	exclude: "node_modules/**",
	cwd: process.cwd()
}

function setSsr(config: UserConfig) {
	const external = ["@meeg/phoria-vue/server"]

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

function phoriaVuePlugin(options?: Partial<PhoriaVuePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: "phoria-vue",
		config: async (config) => {
			setSsr(config)
		},
		transform(code, id) {
			if (!filter(id)) {
				return
			}

			// Remove the cwd from the start of the path

			const path = id.replace(cwdRegex, "")

			// Add the path to the module as named export

			const s = new MagicString(code)
			s.append(`\n\nexport const __phoriaComponentPath = "${path}";`)

			// Generate the source map and return the transformed code

			const map = s.generateMap({
				source: id,
				file: `${id}.map`,
				includeContent: true
			})

			return {
				code: s.toString(),
				map
			}
		}
	}
}

function phoriaVue(options?: Partial<PhoriaVuePluginOptions>): PluginOption {
	const plugins: PluginOption = options?.vue !== false ? [vue(options?.vue)] : []

	plugins.push(phoriaVuePlugin(options))

	return plugins
}

export { phoriaVue }

export type { PhoriaVuePluginOptions }
