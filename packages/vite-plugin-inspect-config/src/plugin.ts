import type { PluginOption } from "vite"
import { join } from "node:path"
import { writeFile, mkdir } from "node:fs/promises"
import { existsSync } from "node:fs"

interface InspectConfigPluginOptions {
	outputDir?: string
	cwd: string
}

const defaultOptions: InspectConfigPluginOptions = {
	cwd: process.cwd()
}

function inspectConfigPlugin(options?: Partial<InspectConfigPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }

	return {
		name: "inspect-config",
		configResolved: async (resolvedConfig) => {
			const outputDir = join(
				opts.cwd,
				opts.outputDir ?? join((resolvedConfig.base ?? "").replaceAll("/", ""), ".vite-config")
			)

			if (!existsSync(outputDir)) {
				await mkdir(outputDir, { recursive: true })
			}

			await writeFile(join(outputDir, "vite.config.json"), JSON.stringify(resolvedConfig, null, 2))
		}
	}
}

export { inspectConfigPlugin as inspectConfig }

export type { InspectConfigPluginOptions }
