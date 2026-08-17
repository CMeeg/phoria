import { normalize } from "node:path"
import react from "@vitejs/plugin-react"
import type { Plugin } from "vite"
import { describe, expect, it } from "vitest"
import { phoriaReact } from "./plugin"

type FrameworkPlugin = Plugin & {
	config: (config: object, env: object) => void
	configEnvironment: (name: string, options: object, env: object) => void
	configResolved: (config: { root: string }) => void
	transform: (this: unknown, code: string, id: string) => { code: string } | undefined
}

function getFrameworkPlugin(options?: Parameters<typeof phoriaReact>[0]) {
	const plugins = phoriaReact(options) as Plugin[]
	return plugins[plugins.length - 1] as FrameworkPlugin
}

describe("phoria-react plugin", () => {
	it("composes the official React plugin before the Phoria framework plugin", () => {
		const plugins = phoriaReact() as Plugin[]
		const officialPlugins = react() as Plugin[]

		expect(plugins.slice(0, -1).map((plugin) => plugin.name)).toEqual(officialPlugins.map((plugin) => plugin.name))
		expect(plugins.at(-1)?.name).toBe("phoria-react")
	})

	it("allows disabling the official React plugin without disabling Phoria", () => {
		const plugins = phoriaReact({ react: false }) as Plugin[]

		expect(plugins).toHaveLength(1)
		expect(plugins[0]?.name).toBe("phoria-react")
	})

	it("injects the component path for a source tsx module", () => {
		const plugin = getFrameworkPlugin()
		const id = normalize(`${process.cwd()}/src/Hello.tsx`)

		const transformed = plugin.transform("export default function Hello() {}", id)

		expect(transformed?.code).toContain('export const __phoriaComponentPath = "src/Hello.tsx";')
	})

	it("merges React optimize dependencies with user entries", () => {
		const plugin = getFrameworkPlugin()
		const config: { optimizeDeps?: { include?: string[] } } = { optimizeDeps: { include: ["custom", "react"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["custom", "react", "react-dom/client"])
	})

	it("externalizes the React Phoria server in the SSR environment", () => {
		const plugin = getFrameworkPlugin()
		const options: { resolve?: { external?: string[] } } = { resolve: { external: ["custom"] } }

		plugin.configEnvironment("ssr", options, { command: "build", mode: "production" })

		expect(options.resolve?.external).toEqual(["custom", "@phoria/phoria-react/server"])
	})

	it("applies to client and SSR environments but not server", () => {
		const plugin = getFrameworkPlugin()

		expect(plugin.applyToEnvironment?.({ name: "client" } as never)).toBe(true)
		expect(plugin.applyToEnvironment?.({ name: "ssr" } as never)).toBe(true)
		expect(plugin.applyToEnvironment?.({ name: "server" } as never)).toBe(false)
	})

	it("forwards public component path options to the shared factory", () => {
		const plugin = getFrameworkPlugin({ include: ["**/*.custom"], exclude: "ignored/**", cwd: "/tmp/custom-root" })
		plugin.configResolved({ root: "/tmp/custom-root" })

		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.tsx")).toBeUndefined()
		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.custom")?.code).toContain(
			'export const __phoriaComponentPath = "src/Hello.custom";'
		)
	})
})
