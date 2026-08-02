import { createApp, toWebHandler } from "h3"
import { describe, expect, it } from "vitest"
import { createPhoriaCsrRequestHandler, createPhoriaSsrRequestHandler, type PhoriaLogger } from "./routing"

const appsettings = {
	root: "ui",
	base: "/ui",
	entry: "entry.ts",
	ssrBase: "/ssr",
	ssrEntry: "entry.ts",
	server: { host: "localhost", https: false },
	build: { outDir: "dist" }
}

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
			createPhoriaSsrRequestHandler(
				{
					root: "ui",
					base: "/ui",
					entry: "entry.ts",
					ssrBase: "/ssr",
					ssrEntry: "missing.ts",
					server: { host: "localhost", https: false },
					build: { outDir: "dist" }
				},
				{ cwd: "/nonexistent-path-for-test", logger }
			)
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
		app.use(createPhoriaCsrRequestHandler(appsettings, { cwd: "/nonexistent-path-for-test", logger }))

		await toWebHandler(app)(new Request("http://localhost/ui/missing.js"), {})

		expect(entries.length).toBeGreaterThan(0)
		expect(entries.every((entry) => entry.message === "Phoria client asset not found.")).toBe(true)
		expect(entries[0]?.data?.path).toContain("/nonexistent-path-for-test/ui/dist/phoria/client/missing.js")
	})
})
