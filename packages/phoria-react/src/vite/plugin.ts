import { createPhoriaFrameworkPlugin, type PhoriaFrameworkPluginOptions } from "@phoria/phoria/vite"
import react, { type Options as ViteReactPluginOptions } from "@vitejs/plugin-react"
import type { PluginOption } from "vite"

const pluginName = "phoria-react"

export type ReactOptions = Pick<ViteReactPluginOptions, "include" | "exclude">

interface PhoriaReactPluginOptions extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	react?: ReactOptions | false
}

const defaultOptions: PhoriaReactPluginOptions = {
	include: ["**/*.jsx", "**/*.tsx"],
	exclude: "node_modules/**",
	cwd: process.cwd(),
	workspacePackages: []
}

function phoriaReact(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }
	const plugins: PluginOption = options?.react !== false ? [...react(options?.react)] : []

	plugins.push(
		createPhoriaFrameworkPlugin({
			name: pluginName,
			include: opts.include,
			exclude: opts.exclude,
			cwd: opts.cwd,
			workspacePackages: opts.workspacePackages,
			optimizeDeps: ["react", "react-dom/client"],
			ssrExternal: ["@phoria/phoria-react/server"]
		})
	)

	return plugins
}

export type { PhoriaReactPluginOptions }
export { phoriaReact }
