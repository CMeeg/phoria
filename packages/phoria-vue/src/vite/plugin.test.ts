import { mkdir, mkdtemp, rm, symlink, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { normalize, relative } from "node:path"
import vue from "@vitejs/plugin-vue"
import type { Plugin, UserConfig } from "vite"
import { afterEach, describe, expect, it } from "vitest"
import { phoriaVue } from "./plugin"

const temporaryDirectories: string[] = []

type FrameworkPlugin = Plugin & {
	config: (config: UserConfig, env: object) => void
	configEnvironment: (name: string, options: object, env: object) => void
	configResolved: (config: { root: string }) => void
	transform: (this: unknown, code: string, id: string) => { code: string } | undefined
}

async function createWorkspaceFixture() {
	const directory = await mkdtemp(`${tmpdir()}/phoria-vue-`)
	temporaryDirectories.push(directory)
	const root = `${directory}/app`
	const workspace = `${directory}/workspace-ui`

	await mkdir(`${root}/node_modules/@workspace`, { recursive: true })
	await mkdir(`${workspace}/src`, { recursive: true })
	await writeFile(`${workspace}/src/Widget.vue`, "<template><h1>Widget</h1></template>")
	await symlink(workspace, `${root}/node_modules/@workspace/ui`, "dir")

	return { root, workspace, packageName: "@workspace/ui" }
}

afterEach(async () => {
	await Promise.all(temporaryDirectories.splice(0).map((directory) => rm(directory, { recursive: true, force: true })))
})

function getFrameworkPlugin(options?: Parameters<typeof phoriaVue>[0]) {
	const plugins = phoriaVue(options) as Plugin[]
	return plugins[plugins.length - 1] as FrameworkPlugin
}

describe("phoria-vue plugin", () => {
	it("composes the official Vue plugin before the Phoria framework plugin", () => {
		const plugins = phoriaVue() as Plugin[]
		const officialPlugin = vue()

		expect(plugins.slice(0, -1).map((plugin) => plugin.name)).toEqual([officialPlugin.name])
		expect(plugins.at(-1)?.name).toBe("phoria-vue")
	})

	it("allows disabling the official Vue plugin without disabling Phoria", () => {
		const plugins = phoriaVue({ vue: false }) as Plugin[]

		expect(plugins).toHaveLength(1)
		expect(plugins[0]?.name).toBe("phoria-vue")
	})

	it("injects the component path for a source vue module", () => {
		const plugin = getFrameworkPlugin()
		const id = normalize(`${process.cwd()}/src/Hello.vue`)

		const transformed = plugin.transform("<template><h1>Hello</h1></template>", id)

		expect(transformed?.code).toContain('export const __phoriaComponentPath = "src/Hello.vue";')
	})

	it("pre-bundles the vue runtime as an optimizeDeps entry", () => {
		const plugin = getFrameworkPlugin()
		const config: { optimizeDeps?: { include?: string[] } } = {}

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["vue"])
	})

	it("preserves user-supplied optimizeDeps include entries", () => {
		const plugin = getFrameworkPlugin()
		const config = { optimizeDeps: { include: ["custom-dep", "vue"] } }

		plugin.config(config, { command: "serve", mode: "development" })

		expect(config.optimizeDeps.include).toEqual(["custom-dep", "vue"])
	})

	it("externalizes the vue server entry for the ssr environment", () => {
		const plugin = getFrameworkPlugin()
		const options: { resolve?: { external?: string[] } } = { resolve: { external: ["custom"] } }

		plugin.configEnvironment("ssr", options, { command: "build", mode: "production" })

		expect(options.resolve?.external).toEqual(["custom", "@phoria/phoria-vue/server"])
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

		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.vue")).toBeUndefined()
		expect(plugin.transform("code", "/tmp/custom-root/src/Hello.custom")?.code).toContain(
			'export const __phoriaComponentPath = "src/Hello.custom";'
		)
	})

	it("forwards workspacePackages so opted-in workspace modules are transformed", async () => {
		const fixture = await createWorkspaceFixture()
		const plugin = getFrameworkPlugin({ cwd: fixture.root, workspacePackages: [fixture.packageName] })
		plugin.configResolved({ root: fixture.root })

		const transformed = plugin.transform(
			"<template><h1>Widget</h1></template>",
			`${fixture.root}/node_modules/@workspace/ui/src/Widget.vue`
		)

		expect(transformed?.code).toContain(
			`export const __phoriaComponentPath = "${normalize(relative(fixture.root, `${fixture.workspace}/src/Widget.vue`))}";`
		)
	})
})
