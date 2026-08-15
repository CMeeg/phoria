import { normalizePath } from "@rollup/pluginutils"
import { describe, expect, it } from "vitest"
import { phoriaReact } from "./plugin"

describe("phoria-react plugin", () => {
	it("pre-bundles the react runtimes as optimizeDeps entries", () => {
		const plugins = phoriaReact() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config: { optimizeDeps?: { include?: string[] } } = {}

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["react", "react-dom/client"])
	})

	it("preserves user-supplied optimizeDeps include entries", () => {
		const plugins = phoriaReact() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config = { optimizeDeps: { include: ["custom-dep", "react"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps.include).toEqual(["custom-dep", "react", "react-dom/client"])
	})

	it("injects the component path for a source tsx module", () => {
		const plugins = phoriaReact() as { transform: (code: string, id: string) => { code: string } | undefined }[]
		const plugin = plugins[plugins.length - 1]

		const transformed = plugin.transform(
			"export default function Hello() {}",
			normalizePath(`${process.cwd()}/src/Hello.tsx`)
		)

		expect(transformed?.code).toContain('export const __phoriaComponentPath = "/src/Hello.tsx";')
	})

	it("does not transform a node_modules module", () => {
		const plugins = phoriaReact() as { transform: (code: string, id: string) => { code: string } | undefined }[]
		const plugin = plugins[plugins.length - 1]

		expect(
			plugin.transform(
				"export default function Hello() {}",
				normalizePath(`${process.cwd()}/node_modules/hello/Hello.tsx`)
			)
		).toBeUndefined()
	})

	it("applies to client and ssr environments but not server", () => {
		const plugins = phoriaReact() as unknown as { applyToEnvironment: (environment: { name: string }) => boolean }[]
		const plugin = plugins[plugins.length - 1]

		expect(plugin.applyToEnvironment({ name: "client" })).toBe(true)
		expect(plugin.applyToEnvironment({ name: "ssr" })).toBe(true)
		expect(plugin.applyToEnvironment({ name: "server" })).toBe(false)
	})
})
