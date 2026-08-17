import { mkdir, mkdtemp, rm, symlink, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { normalize, relative } from "node:path"
import { svelte } from "@sveltejs/vite-plugin-svelte"
import type { Plugin, UserConfig } from "vite"
import { afterEach, describe, expect, it } from "vitest"
import { phoriaSvelte } from "./plugin"

const temporaryDirectories: string[] = []

type FrameworkPlugin = Plugin & {
	config: (config: UserConfig, env: object) => void
	configEnvironment: (name: string, options: object, env: object) => void
	configResolved: (config: { root: string }) => void
	transform: (this: unknown, code: string, id: string) => { code: string } | undefined
}

async function createWorkspaceFixture() {
	const directory = await mkdtemp(`${tmpdir()}/phoria-svelte-`)
	temporaryDirectories.push(directory)
	const root = `${directory}/app`
	const workspace = `${directory}/workspace-ui`

	await mkdir(`${root}/node_modules/@workspace`, { recursive: true })
	await mkdir(`${workspace}/src`, { recursive: true })
	await writeFile(`${workspace}/src/Widget.svelte`, "<h1>Widget</h1>")
	await symlink(workspace, `${root}/node_modules/@workspace/ui`, "dir")

	return { root, workspace, packageName: "@workspace/ui" }
}

afterEach(async () => {
	await Promise.all(temporaryDirectories.splice(0).map((directory) => rm(directory, { recursive: true, force: true })))
})

function getFrameworkPlugin(options?: Parameters<typeof phoriaSvelte>[0]) {
	const plugins = phoriaSvelte(options) as Plugin[]
	return plugins[plugins.length - 1] as FrameworkPlugin
}

describe("phoria-svelte plugin", () => {
	it("composes the official Svelte plugin before the Phoria framework plugin", () => {
		const plugins = phoriaSvelte() as Plugin[]
		const officialPlugins = svelte()

		expect(plugins.slice(0, -1).map((plugin) => plugin.name)).toEqual(officialPlugins.map((plugin) => plugin.name))
		expect(plugins.at(-1)?.name).toBe("phoria-svelte")
	})

	it("allows disabling the official Svelte plugin without disabling Phoria", () => {
		const plugins = phoriaSvelte({ svelte: false }) as Plugin[]

		expect(plugins).toHaveLength(1)
		expect(plugins[0]?.name).toBe("phoria-svelte")
	})

	it("injects the component path for a source svelte module", () => {
		const plugin = getFrameworkPlugin()
		const id = normalize(`${process.cwd()}/src/Hello.svelte`)

		const transformed = plugin.transform("<h1>Hello</h1>", id)

		expect(transformed?.code).toContain('export const __phoriaComponentPath = "src/Hello.svelte";')
	})

	it("pre-bundles the svelte runtime as an optimizeDeps entry", () => {
		const plugin = getFrameworkPlugin()
		const config: { optimizeDeps?: { include?: string[] } } = {}

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["svelte"])
	})

	it("preserves user-supplied optimizeDeps include entries", () => {
		const plugin = getFrameworkPlugin()
		const config = { optimizeDeps: { include: ["custom-dep", "svelte"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps.include).toEqual(["custom-dep", "svelte"])
	})

	it("externalizes svelte for the ssr environment so the compiled component and the SSR renderer share one module instance", () => {
		const plugin = getFrameworkPlugin()
		const options: { resolve?: { external?: string[] } } = { resolve: { external: ["custom"] } }

		plugin.configEnvironment("ssr", options, { command: "build", mode: "production" })

		expect(options.resolve?.external).toEqual(["custom", "@phoria/phoria-svelte/server", "svelte"])
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

		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.svelte")).toBeUndefined()
		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.custom")?.code).toContain(
			'export const __phoriaComponentPath = "src/Hello.custom";'
		)
	})

	it("forwards workspacePackages so opted-in workspace modules are transformed", async () => {
		const fixture = await createWorkspaceFixture()
		const plugin = getFrameworkPlugin({ cwd: fixture.root, workspacePackages: [fixture.packageName] })
		plugin.configResolved({ root: fixture.root })

		const transformed = plugin.transform(
			"<h1>Widget</h1>",
			`${fixture.root}/node_modules/@workspace/ui/src/Widget.svelte`
		)

		expect(transformed?.code).toContain(
			`export const __phoriaComponentPath = "${normalize(relative(fixture.root, `${fixture.workspace}/src/Widget.svelte`))}";`
		)
	})
})
