import { createPhoriaFrameworkPlugin, type PhoriaFrameworkPluginOptions } from "@phoria/phoria/vite"
import { type Options as SvelteOptions, svelte } from "@sveltejs/vite-plugin-svelte"
import type { PluginOption } from "vite"

const pluginName = "phoria-svelte"

interface PhoriaSveltePluginOptions
	extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	svelte?: SvelteOptions | false
}

const defaultOptions: PhoriaSveltePluginOptions = {
	include: ["**/*.svelte"],
	exclude: "node_modules/**",
	cwd: process.cwd(),
	workspacePackages: []
}

function phoriaSvelte(options?: Partial<PhoriaSveltePluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }
	const plugins: PluginOption = options?.svelte !== false ? [...svelte(options?.svelte)] : []

	plugins.push(
		createPhoriaFrameworkPlugin({
			name: pluginName,
			include: opts.include,
			exclude: opts.exclude,
			cwd: opts.cwd,
			workspacePackages: opts.workspacePackages,
			optimizeDeps: ["svelte"],
			ssrExternal: ["@phoria/phoria-svelte/server", "svelte"]
		})
	)

	return plugins
}

export type { PhoriaSveltePluginOptions }
export { phoriaSvelte }
