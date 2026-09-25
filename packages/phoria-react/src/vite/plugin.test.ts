import { mkdir, mkdtemp, rm, symlink, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { normalize, relative } from "node:path"
import react from "@vitejs/plugin-react"
import type { Plugin, UserConfig } from "vite"
import { afterEach, describe, expect, it } from "vitest"
import { phoriaReact } from "./plugin"

const temporaryDirectories: string[] = []

type FrameworkPlugin = Plugin & {
	config: (config: UserConfig, env: object) => void
	configEnvironment: (name: string, options: object, env: object) => void
	configResolved: (config: { root: string }) => void
	transform: (this: unknown, code: string, id: string) => { code: string } | undefined
}

async function createWorkspaceFixture() {
	const directory = await mkdtemp(`${tmpdir()}/phoria-react-`)
	temporaryDirectories.push(directory)
	const root = `${directory}/app`
	const workspace = `${directory}/workspace-ui`

	await mkdir(`${root}/node_modules/@workspace`, { recursive: true })
	await mkdir(`${workspace}/src`, { recursive: true })
	await writeFile(`${workspace}/src/Widget.ts`, "export default 1")
	await symlink(workspace, `${root}/node_modules/@workspace/ui`, "dir")

	return { root, workspace, packageName: "@workspace/ui" }
}

afterEach(async () => {
	await Promise.all(temporaryDirectories.splice(0).map((directory) => rm(directory, { recursive: true, force: true })))
})

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

	it("forwards workspacePackages so opted-in workspace modules are transformed", async () => {
		const fixture = await createWorkspaceFixture()
		const plugin = getFrameworkPlugin({ cwd: fixture.root, workspacePackages: [fixture.packageName] })
		plugin.configResolved({ root: fixture.root })

		const transformed = plugin.transform("export default 1", `${fixture.root}/node_modules/@workspace/ui/src/Widget.ts`)

		expect(transformed?.code).toContain(
			`export const __phoriaComponentPath = "${normalize(relative(fixture.root, `${fixture.workspace}/src/Widget.ts`))}";`
		)
	})
})
