import { describe, expect, it } from "vitest"
import { phoriaVue } from "./plugin"

describe("phoria-vue plugin", () => {
	it("pre-bundles the vue runtime as an optimizeDeps entry", () => {
		const plugins = phoriaVue() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config: { optimizeDeps?: { include?: string[] } } = {}

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["vue"])
	})

	it("preserves user-supplied optimizeDeps include entries", () => {
		const plugins = phoriaVue() as { config: (config: object, env: object) => void }[]
		const plugin = plugins[plugins.length - 1]
		const config = { optimizeDeps: { include: ["custom-dep", "vue"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps.include).toEqual(["custom-dep", "vue"])
	})
})
