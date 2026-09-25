import { mkdir, mkdtemp, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { createApp, toWebHandler } from "h3"
import { afterEach, describe, expect, it, vi } from "vitest"
import { createPhoriaAppSettings } from "../../tests/utilities/appsettings-fixture"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	type PhoriaLogger
} from "./routing"

const tempDirectories: string[] = []

afterEach(async () => {
	for (const directory of tempDirectories.splice(0)) await rm(directory, { recursive: true, force: true })
})

describe("createPhoriaSsrRequestHandler", () => {
	it("logs SSR entry load failures through the configured logger", async () => {
		const entries: Array<{ message: string; data?: Record<string, unknown> }> = []
		const logger: PhoriaLogger = {
			info: (message, data) => entries.push({ message, data }),
			warn: (message, data) => entries.push({ message, data }),
			error: (message, data) => entries.push({ message, data })
		}
		const app = createApp({ onError: () => {} })
		app.use(
			createPhoriaSsrRequestHandler(createPhoriaAppSettings({ ssrEntry: "missing.ts" }), {
				cwd: "/nonexistent-path-for-test",
				logger
			})
		)
		const handler = toWebHandler(app)

		await handler(new Request("http://localhost/ssr/render/example", { method: "POST" }), {})

		expect(entries).toHaveLength(1)
		expect(entries[0]?.message).toBe("Failed to load Phoria SSR server entry.")
		expect(entries[0]?.data?.error).toBeInstanceOf(Error)
	})

	it("logs missing CSR assets through the configured logger", async () => {
		const entries: Array<{ message: string; data?: Record<string, unknown> }> = []
		const logger: PhoriaLogger = {
			info: (message, data) => entries.push({ message, data }),
			warn: (message, data) => entries.push({ message, data }),
			error: (message, data) => entries.push({ message, data })
		}
		const app = createApp({ onError: () => {} })
		app.use(createPhoriaCsrRequestHandler(createPhoriaAppSettings(), { cwd: "/nonexistent-path-for-test", logger }))

		await toWebHandler(app)(new Request("http://localhost/ui/missing.js"), {})

		expect(entries.length).toBeGreaterThan(0)
		expect(entries.every((entry) => entry.message === "Phoria client asset not found.")).toBe(true)
		expect(entries[0]?.data?.path).toContain("/nonexistent-path-for-test/ui/dist/phoria/client/missing.js")
	})

	it("returns the health-check mode and registered frameworks through the dev handler", async () => {
		vi.stubEnv("NODE_ENV", "test")
		const { registerCsrService } = await import("~/register")
		registerCsrService("routing-test", { mount: async () => {} })
		const app = createApp({ onError: () => {} })
		const settings = createPhoriaAppSettings({ ssrEntry: "entry.ts" })
		const environment = { runner: { import: vi.fn(async () => ({ renderPhoriaIsland: vi.fn() })) } }
		const handler = createPhoriaDevSsrRequestHandler(
			{ environments: { ssr: environment }, _vite: { isRunnableDevEnvironment: () => true } } as never,
			settings
		)
		app.use(handler)

		const response = await toWebHandler(app)(new Request("http://localhost/hc"), {})

		expect(response.status).toBe(200)
		expect(await response.json()).toEqual({ mode: "test", frameworks: ["routing-test"] })
	})

	it("returns 500 when the dev server entry is not a Phoria server entry", async () => {
		const app = createApp({ onError: () => {} })
		const settings = createPhoriaAppSettings({ ssrEntry: "entry.ts" })
		const environment = { runner: { import: vi.fn(async () => ({ invalid: true })) } }
		app.use(
			createPhoriaDevSsrRequestHandler(
				{ environments: { ssr: environment }, _vite: { isRunnableDevEnvironment: () => true } } as never,
				settings
			)
		)

		const response = await toWebHandler(app)(new Request("http://localhost/hc"), {})

		expect(response.status).toBe(500)
	})

	it("serves a CSR JavaScript asset with its MIME type", async () => {
		const cwd = await mkdtemp(join(tmpdir(), "phoria-routing-"))
		tempDirectories.push(cwd)
		const assetDirectory = join(cwd, "ui", "dist", "phoria", "client")
		await mkdir(assetDirectory, { recursive: true })
		await writeFile(join(assetDirectory, "entry.js"), "export default 1")
		const app = createApp()
		app.use(createPhoriaCsrRequestHandler(createPhoriaAppSettings(), { cwd }))

		const response = await toWebHandler(app)(new Request("http://localhost/ui/entry.js"), {})

		expect(response.status).toBe(200)
		expect(response.headers.get("content-type")).toContain("text/javascript")
		expect(await response.text()).toBe("export default 1")
	})
})
