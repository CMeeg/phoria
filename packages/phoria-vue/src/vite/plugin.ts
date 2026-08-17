import { createPhoriaFrameworkPlugin, type PhoriaFrameworkPluginOptions } from "@phoria/phoria/vite"
import vue, { type Options as VueOptions } from "@vitejs/plugin-vue"
import type { PluginOption } from "vite"

const pluginName = "phoria-vue"

interface PhoriaVuePluginOptions extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	vue?: VueOptions | false
}

const defaultOptions: PhoriaVuePluginOptions = {
	include: ["**/*.vue"],
	exclude: "node_modules/**",
	cwd: process.cwd(),
	workspacePackages: []
}

function phoriaVue(options?: Partial<PhoriaVuePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }
	const plugins: PluginOption = options?.vue !== false ? [vue(options?.vue)] : []

	plugins.push(
		createPhoriaFrameworkPlugin({
			name: pluginName,
			include: opts.include,
			exclude: opts.exclude,
			cwd: opts.cwd,
			workspacePackages: opts.workspacePackages,
			optimizeDeps: ["vue"],
			ssrExternal: ["@phoria/phoria-vue/server"]
		})
	)

	return plugins
}

export type { PhoriaVuePluginOptions }
export { phoriaVue }
