import { mkdir, mkdtemp, rm, symlink, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join, relative } from "node:path"
import type { EnvironmentOptions, Plugin, UserConfig } from "vite"
import { afterEach, describe, expect, it } from "vitest"
import { createPhoriaFrameworkPlugin, resolvePackageDir } from "./framework"

const temporaryDirectories: string[] = []

async function createFixture() {
	const directory = await mkdtemp(join(tmpdir(), "phoria-framework-"))
	temporaryDirectories.push(directory)
	const root = join(directory, "app")
	const workspace = join(directory, "workspace-ui")
	await mkdir(join(root, "src"), { recursive: true })
	await mkdir(join(root, "node_modules"), { recursive: true })
	await mkdir(join(workspace, "src"), { recursive: true })
	await mkdir(join(workspace, "dist"), { recursive: true })
	await writeFile(join(root, "src", "App.tsx"), "export default 1")
	await writeFile(join(workspace, "src", "Widget.tsx"), "export default 1")
	await writeFile(join(workspace, "dist", "index.js"), "export default 1")
	await mkdir(join(root, "node_modules", "@workspace"), { recursive: true })
	await symlink(workspace, join(root, "node_modules", "@workspace", "ui"), "dir")

	return { directory, root, workspace, packageName: "@workspace/ui" }
}

type FrameworkHooks = {
	config: (config: UserConfig, env: { command: "serve" | "build"; mode: string }) => void | Promise<void>
	configEnvironment: (
		name: string,
		options: EnvironmentOptions,
		env: { command: "serve" | "build"; mode: string }
	) => void
	configResolved: (config: { root: string }) => void
	applyToEnvironment: (environment: { name: string }) => boolean
	transform: (
		this: unknown,
		code: string,
		id: string
	) => { code: string; map: unknown } | undefined | Promise<{ code: string; map: unknown } | undefined>
}

function pluginHooks(plugin: Plugin) {
	return plugin as unknown as FrameworkHooks
}

function createPlugin(
	fixture: Awaited<ReturnType<typeof createFixture>>,
	overrides: Partial<Parameters<typeof createPhoriaFrameworkPlugin>[0]> = {}
) {
	return pluginHooks(
		createPhoriaFrameworkPlugin({
			name: "test-framework",
			include: ["**/*.tsx"],
			exclude: "node_modules/**",
			cwd: fixture.root,
			workspacePackages: [fixture.packageName],
			optimizeDeps: ["framework-runtime"],
			ssrExternal: ["test-framework/server"],
			...overrides
		})
	)
}

afterEach(async () => {
	await Promise.all(
		temporaryDirectories.splice(0).map(async (directory) => {
			await rm(directory, { recursive: true, force: true })
		})
	)
})

describe("createPhoriaFrameworkPlugin", () => {
	it("emits root-relative paths for app modules and strips query and hash suffixes", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture)
		await plugin.config({}, { command: "serve", mode: "development" })
		plugin.configResolved({ root: fixture.root } as never)

		const result = await plugin.transform("export default 1", `${join(fixture.root, "src/App.tsx")}?v=1#hash`)

		expect(result?.code).toContain('export const __phoriaComponentPath = "src/App.tsx";')
		expect(result?.map).toBeDefined()
	})

	it("leaves Vue virtual style modules untouched", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture, { include: ["**/*.vue"] })
		plugin.configResolved({ root: fixture.root } as never)

		const result = await plugin.transform(
			".counter-button { color: red; }",
			`${join(fixture.root, "src/counter-button.vue")}?vue&type=style&lang.css`
		)

		expect(result).toBeUndefined()
	})

	it("uses the configured Vite root when cwd is different", async () => {
		const fixture = await createFixture()
		const root = fixture.directory
		const plugin = createPlugin(fixture, { cwd: fixture.root })
		await plugin.config({}, { command: "serve", mode: "development" })
		plugin.configResolved({ root } as never)

		const result = await plugin.transform("export default 1", join(fixture.root, "src/App.tsx"))

		expect(result?.code).toContain('export const __phoriaComponentPath = "app/src/App.tsx";')
	})

	it("transforms opted-in workspace source and dist modules from symlinked IDs", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture)
		await plugin.config({}, { command: "build", mode: "production" })
		plugin.configResolved({ root: fixture.root } as never)

		const symlinkedWorkspace = join(fixture.root, "node_modules", "@workspace", "ui")
		const source = await plugin.transform("export default 1", join(symlinkedWorkspace, "src/Widget.tsx?direct"))
		const dist = await plugin.transform("export default 1", join(symlinkedWorkspace, "dist/index.js#hash"))

		expect(source?.code).toContain(
			`export const __phoriaComponentPath = "${normalize(relative(fixture.root, join(fixture.workspace, "src/Widget.tsx")))}";`
		)
		expect(dist?.code).toContain(
			`export const __phoriaComponentPath = "${normalize(relative(fixture.root, join(fixture.workspace, "dist/index.js")))}";`
		)
	})

	it("leaves filtered and unconfigured workspace modules untouched", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture, { workspacePackages: [] })
		plugin.configResolved({ root: fixture.root } as never)

		expect(await plugin.transform("code", join(fixture.root, "node_modules/pkg/index.tsx"))).toBeUndefined()
		expect(await plugin.transform("code", join(fixture.workspace, "dist/index.js"))).toBeUndefined()
	})

	it("reports an actionable error for an unresolvable workspace package", () => {
		expect(() => resolvePackageDir("missing-package", "/tmp/phoria-missing-cwd")).toThrow(
			/missing-package.*\/tmp\/phoria-missing-cwd/
		)
	})

	it("merges optimize dependencies without duplicates and configures SSR externals", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture)
		const config: UserConfig = { optimizeDeps: { include: ["custom", "framework-runtime"] } }
		await plugin.config(config, { command: "serve", mode: "development" })
		const environment: EnvironmentOptions = { resolve: { external: ["custom-external"] } }
		plugin.configEnvironment("ssr", environment, { command: "serve", mode: "development" })

		expect(config.optimizeDeps?.include).toEqual(["custom", "framework-runtime"])
		expect(config.environments?.ssr).toEqual({})
		expect(environment.resolve?.external).toEqual(["custom-external", "test-framework/server"])
	})

	it("preserves non-array SSR external configuration while applying requested entries", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture)

		const allExternal: EnvironmentOptions = { resolve: { external: true } }
		plugin.configEnvironment("ssr", allExternal, { command: "build", mode: "production" })
		expect(allExternal.resolve?.external).toBe(true)

		const selectiveExternal: EnvironmentOptions = { resolve: { external: (() => false) as never } }
		plugin.configEnvironment("ssr", selectiveExternal, { command: "build", mode: "production" })
		const external = selectiveExternal.resolve?.external as unknown as (source: string) => boolean
		expect(external("test-framework/server")).toBe(true)
		expect(external("other-module")).toBe(false)

		const disabledExternal: EnvironmentOptions = { resolve: { external: false as never } }
		plugin.configEnvironment("ssr", disabledExternal, { command: "build", mode: "production" })
		const disabled = disabledExternal.resolve?.external as unknown as (source: string) => boolean
		expect(disabled("test-framework/server")).toBe(true)
		expect(disabled("other-module")).toBe(false)
	})

	it("applies only to client and SSR environments", async () => {
		const fixture = await createFixture()
		const plugin = createPlugin(fixture)

		expect(plugin.applyToEnvironment({ name: "client" } as never)).toBe(true)
		expect(plugin.applyToEnvironment({ name: "ssr" } as never)).toBe(true)
		expect(plugin.applyToEnvironment({ name: "server" } as never)).toBe(false)
		expect(
			await plugin.transform.call({ environment: { name: "server" } }, "code", join(fixture.root, "src/App.tsx"))
		).toBeUndefined()
	})
})

function normalize(path: string) {
	return path.replaceAll("\\", "/")
}
