import { createFilter, normalizePath } from "@rollup/pluginutils"
import vue, { type Options as VueOptions } from "@vitejs/plugin-vue"
import MagicString from "magic-string"
import type { EnvironmentOptions, PluginOption, UserConfig } from "vite"

const pluginName = "phoria-vue"

const environment = {
	client: "client",
	ssr: "ssr"
} as const

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

function setSsrEnvironment(options: EnvironmentOptions) {
	const external = ["@phoria/phoria-vue/server"]

	options.resolve ??= {}

	if (typeof options.resolve.external === "undefined") {
		options.resolve.external = external
	} else if (Array.isArray(options.resolve.external)) {
		options.resolve.external.push(...external)
	}
}

function setOptimizeDeps(config: UserConfig, include: string[]) {
	config.optimizeDeps ??= {}
	config.optimizeDeps.include ??= []
	config.optimizeDeps.include = Array.from(new Set([...config.optimizeDeps.include, ...include]))
}

function phoriaVuePlugin(options?: Partial<PhoriaVuePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: pluginName,
		config: (config) => {
			config.environments ??= {}
			config.environments[environment.ssr] ??= {}

			// Pre-bundle the runtime as its own entry so the client's dynamic import
			// shares a single instance with the statically imported one in the app code

			setOptimizeDeps(config, ["vue"])
		},
		configEnvironment(name, options) {
			if (name === environment.ssr) {
				setSsrEnvironment(options)
			}
		},
		applyToEnvironment(environment) {
			return environment.name === "client" || environment.name === "ssr"
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

export type { PhoriaVuePluginOptions }
export { phoriaVue }
