import type { Plugin } from "vite"
import { createFilter, normalizePath } from "@rollup/pluginutils"
import MagicString from "magic-string"

type CreateFilterParams = Parameters<typeof createFilter>

interface PhoriaVuePluginOptions {
	include: CreateFilterParams[0]
	exclude: CreateFilterParams[1],
	cwd: string
}

const defaultOptions: PhoriaVuePluginOptions = {
	include: ["**/*.vue"],
	exclude: "node_modules/**",
	cwd: process.cwd()
}

// TODO: Maybe also include the vue plugin so you don't have to install it separately?

function phoriaVuePlugin(options?: Partial<PhoriaVuePluginOptions>): Plugin {
	const opts = { ...defaultOptions, ...options }

	const filter = createFilter(opts.include, opts.exclude)

	const cwd = normalizePath(opts.cwd)
	const cwdRegex = new RegExp(`^${cwd}`, "i");

	return {
		name: "phoria-vue",
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

export { phoriaVuePlugin as phoriaVue }

export type { PhoriaVuePluginOptions }
