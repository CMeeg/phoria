import { createFilter, normalizePath } from "@rollup/pluginutils"
import { type Options as SvelteOptions, svelte } from "@sveltejs/vite-plugin-svelte"
import MagicString from "magic-string"
import type { PluginOption, UserConfig } from "vite"

type CreateFilterParams = Parameters<typeof createFilter>

interface PhoriaSveltePluginOptions {
	include: CreateFilterParams[0]
	exclude: CreateFilterParams[1]
	cwd: string
	svelte?: SvelteOptions | false
}

const defaultOptions: PhoriaSveltePluginOptions = {
	include: ["**/*.svelte"],
	exclude: "node_modules/**",
	cwd: process.cwd()
}

function setSsr(config: UserConfig) {
	const external = ["@phoria/phoria-svelte/server"]

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

function phoriaSveltePlugin(options?: Partial<PhoriaSveltePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: "phoria-svelte",
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

function phoriaSvelte(options?: Partial<PhoriaSveltePluginOptions>): PluginOption {
	const plugins: PluginOption = options?.svelte !== false ? [...svelte(options?.svelte)] : []

	plugins.push(phoriaSveltePlugin(options))

	return plugins
}

export { phoriaSvelte }

export type { PhoriaSveltePluginOptions }
