import { createFilter, normalizePath } from "@rollup/pluginutils"
import react, { type Options as ViteReactPluginOptions } from "@vitejs/plugin-react"
import MagicString from "magic-string"
import type { PluginOption, EnvironmentOptions } from "vite"

export type ReactOptions = Pick<ViteReactPluginOptions, "include" | "exclude" | "babel">

type CreateFilterParams = Parameters<typeof createFilter>

interface PhoriaReactPluginOptions {
	include: CreateFilterParams[0]
	exclude: CreateFilterParams[1]
	cwd: string
	react?: ReactOptions | false
}

const defaultOptions: PhoriaReactPluginOptions = {
	include: ["**/*.jsx", "**/*.tsx"],
	exclude: "node_modules/**",
	cwd: process.cwd()
}

function setSsrEnvironment(options: EnvironmentOptions) {
	const external = ["@phoria/phoria-react/server"]

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

function phoriaReactPlugin(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: "phoria-react",
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

function phoriaReact(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const plugins: PluginOption = options?.react !== false ? [...react(options?.react)] : []

	plugins.push(phoriaReactPlugin(options))

	return plugins
}

export { phoriaReact }

export type { PhoriaReactPluginOptions }
