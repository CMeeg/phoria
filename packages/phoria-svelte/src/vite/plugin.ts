import { createFilter, normalizePath } from "@rollup/pluginutils"
import { type Options as SvelteOptions, svelte } from "@sveltejs/vite-plugin-svelte"
import MagicString from "magic-string"
import type { PluginOption, EnvironmentOptions } from "vite"

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

function setSsrEnvironment(options: EnvironmentOptions) {
	const external = ["@phoria/phoria-svelte/server"]

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

function phoriaSveltePlugin(options?: Partial<PhoriaSveltePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: "phoria-svelte",
		config: (config) => {
			config.environments ??= {}
			config.environments.ssr ??= {}
		},
		configEnvironment(name, options) {
			if (name === "ssr") {
				setSsrEnvironment(options)
			}
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
