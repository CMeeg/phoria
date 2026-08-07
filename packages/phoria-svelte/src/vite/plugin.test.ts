import { describe, expect, it } from "vitest"
import { phoriaSvelte } from "./plugin"

describe("phoria-svelte plugin", () => {
	it("pre-bundles the svelte runtime as an optimizeDeps entry", () => {
		const plugins = phoriaSvelte() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config: { optimizeDeps?: { include?: string[] } } = {}

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["svelte"])
	})

	it("preserves user-supplied optimizeDeps include entries", () => {
		const plugins = phoriaSvelte() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config = { optimizeDeps: { include: ["custom-dep", "svelte"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps.include).toEqual(["custom-dep", "svelte"])
	})
})
