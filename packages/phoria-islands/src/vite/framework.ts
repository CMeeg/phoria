import { existsSync, realpathSync } from "node:fs"
import { dirname, join, relative, resolve } from "node:path"
import { createFilter, normalizePath } from "@rollup/pluginutils"
import MagicString from "magic-string"
import type { EnvironmentOptions, Plugin, UserConfig } from "vite"

type CreateFilterParams = Parameters<typeof createFilter>

interface PhoriaFrameworkPluginOptions {
	name: string
	include: CreateFilterParams[0]
	exclude: CreateFilterParams[1]
	cwd: string
	workspacePackages: string[]
	optimizeDeps: string[]
	ssrExternal: string[]
}

function resolvePackageDir(packageName: string, cwd: string): string {
	let current = resolve(cwd)

	while (true) {
		const packagePath = join(current, "node_modules", packageName)

		if (existsSync(packagePath)) {
			return realpathSync(packagePath)
		}

		const parent = dirname(current)
		if (parent === current) {
			break
		}
		current = parent
	}

	throw new Error(`Unable to resolve workspace package "${packageName}" from cwd "${cwd}".`)
}

function mergeOptimizeDeps(config: UserConfig, entries: string[]) {
	config.optimizeDeps ??= {}
	config.optimizeDeps.include = Array.from(new Set([...(config.optimizeDeps.include ?? []), ...entries]))
}

function configureSsrExternal(options: EnvironmentOptions, entries: string[]) {
	options.resolve ??= {}
	if (Array.isArray(options.resolve.external)) {
		options.resolve.external = Array.from(new Set([...options.resolve.external, ...entries]))
	} else if (typeof options.resolve.external === "undefined") {
		options.resolve.external = entries
	}
}

function isWithin(directory: string, path: string) {
	const relativePath = relative(directory, path)
	return relativePath === "" || (!relativePath.startsWith("..") && !relativePath.includes("../"))
}

function cleanModuleId(id: string) {
	return id.replace(/[?#].*$/, "")
}

function createPhoriaFrameworkPlugin(options: PhoriaFrameworkPluginOptions): Plugin {
	const filter = createFilter(options.include, options.exclude)
	const workspaceDirectories = options.workspacePackages.map((packageName) =>
		resolvePackageDir(packageName, options.cwd)
	)
	let root = normalizePath(resolve(options.cwd))

	return {
		name: options.name,
		config(config) {
			config.environments ??= {}
			config.environments.ssr ??= {}
			mergeOptimizeDeps(config, options.optimizeDeps)
		},
		configEnvironment(name, environmentOptions) {
			if (name === "ssr") {
				configureSsrExternal(environmentOptions, options.ssrExternal)
			}
		},
		configResolved(config) {
			root = normalizePath(config.root)
		},
		applyToEnvironment(environment) {
			return environment.name === "client" || environment.name === "ssr"
		},
		transform(code, id) {
			const cleanId = cleanModuleId(id)
			const normalizedId = normalizePath(cleanId)
			if (normalizedId.includes("/node_modules/")) {
				return
			}
			const realId = existsSync(cleanId) ? normalizePath(realpathSync(cleanId)) : normalizedId
			const isWorkspaceModule = workspaceDirectories.some((directory) => isWithin(normalizePath(directory), realId))

			if (!isWorkspaceModule && !filter(normalizedId)) {
				return
			}

			const componentPath = normalizePath(relative(root, cleanId))
			const source = new MagicString(code)
			source.append(`\n\nexport const __phoriaComponentPath = ${JSON.stringify(componentPath)};`)

			return {
				code: source.toString(),
				map: source.generateMap({ source: cleanId, file: `${cleanId}.map`, includeContent: true })
			}
		}
	}
}

export type { PhoriaFrameworkPluginOptions }
export { createPhoriaFrameworkPlugin, resolvePackageDir }
