import type { PluginOption, UserConfig } from "vite"
import react, { type Options as ViteReactPluginOptions } from "@vitejs/plugin-react"
import { createFilter, normalizePath } from "@rollup/pluginutils"
import MagicString from "magic-string"

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

function setSsr(config: UserConfig) {
	const external = ["@meeg/phoria-react/server"]

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

function phoriaReactPlugin(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i")

	// TODO: Maybe also add the client and server imports to client and server entries?

	return {
		name: "phoria-react",
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

function phoriaReact(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const plugins: PluginOption = options?.react !== false ? [...react(options?.react)] : []

	plugins.push(phoriaReactPlugin(options))

	return plugins
}

export { phoriaReact }

export type { PhoriaReactPluginOptions }
