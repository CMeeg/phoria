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
})
