import { createApp, toWebHandler } from "h3"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createPhoriaAppSettings } from "../../tests/utilities/appsettings-fixture"
import { createPhoriaViteDevServer } from "./vite"

describe("createPhoriaViteDevServer", () => {
	it("creates a middleware Vite server", async () => {
		const vite = {
			createServer: async (config: unknown) => ({ config }),
			isRunnableDevEnvironment: () => true
		} as never

		const server = await createPhoriaViteDevServer(Promise.resolve(vite))

		expect(server.config).toEqual({
			appType: "custom",
			server: { middlewareMode: true }
		})
	})
})

describe("createPhoriaDevSsrRequestHandler", () => {
	beforeEach(() => {
		vi.resetModules()
	})

	it("throws when the dev server has no runnable SSR environment", async () => {
		const { createPhoriaDevSsrRequestHandler } = await import("./routing")
		const server = {
			environments: { ssr: {} },
			_vite: { isRunnableDevEnvironment: () => false }
		} as never

		expect(() => createPhoriaDevSsrRequestHandler(server, createPhoriaAppSettings())).toThrow(
			"Vite dev server does not have a runnable SSR environment."
		)
	})

	it("renders an island through the dev server's SSR runner", async () => {
		const { registerSsrComponentFramework } = await import("../../tests/utilities/register-fakes")
		const { createPhoriaDevSsrRequestHandler } = await import("./routing")

		registerSsrComponentFramework("react", "<span>Counter</span>")

		// The SSR router only imports the server entry module id (routing.ts calls
		// `runner.import(appsettings.ssrEntry)`); component modules are resolved by the
		// registered loader, not the runner.
		const settings = createPhoriaAppSettings()
		const serverEntry = {
			renderPhoriaIsland: (island: { render: () => Promise<{ framework: string; html: string }> }) => island.render()
		}
		const server = {
			environments: {
				ssr: {
					runner: {
						import: (id: string) => {
							if (id !== settings.ssrEntry) {
								throw new Error(`Unexpected SSR runner import: ${id}`)
							}

							return serverEntry
						}
					}
				}
			},
			_vite: { isRunnableDevEnvironment: () => true }
		} as never

		const app = createApp({ onError: () => {} })
		app.use(createPhoriaDevSsrRequestHandler(server, settings))
		const handler = toWebHandler(app)

		const response = await handler(new Request("http://localhost/ssr/render/Counter", { method: "POST" }), {})

		expect(response.status).toBe(200)
		expect(await response.text()).toContain("<span>Counter</span>")
		expect(response.headers.get("x-phoria-island-framework")).toBe("react")
	})
})
