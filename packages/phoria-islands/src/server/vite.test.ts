import { describe, expect, it } from "vitest"
import { createPhoriaAppSettings } from "../../tests/utilities/appsettings-fixture"
import { createPhoriaDevSsrRequestHandler } from "./routing"
import { createPhoriaViteDevServer } from "./vite"

describe("createPhoriaViteDevServer", () => {
	it("creates a middleware Vite server and retains its Vite module", async () => {
		const vite = {
			createServer: async (config: unknown) => ({ config }),
			isRunnableDevEnvironment: () => true
		} as never

		const server = await createPhoriaViteDevServer(Promise.resolve(vite))

		expect(server._vite).toBe(vite)
		expect(server.config).toEqual({
			appType: "custom",
			server: { middlewareMode: true }
		})
	})

	it("uses the Vite module attached to the dev server for SSR validation", () => {
		const server = {
			_vite: { isRunnableDevEnvironment: () => true },
			environments: { ssr: { runner: { import: async () => ({}) } } }
		} as never

		expect(() => createPhoriaDevSsrRequestHandler(server, createPhoriaAppSettings())).not.toThrow()
	})
})
