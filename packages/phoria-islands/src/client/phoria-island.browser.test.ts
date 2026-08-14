import { afterEach, describe, expect, it, vi } from "vitest"

describe("PhoriaIsland", () => {
	afterEach(() => {
		vi.restoreAllMocks()
		vi.useRealTimers()
	})

	async function createIsland(attributes: Record<string, string> = {}) {
		const { registerComponent, registerCsrService } = await import("~/register")
		const { PhoriaIsland } = await import("./phoria-island")
		const mount = vi.fn(async () => {})
		registerCsrService("browser-test", { mount })
		registerComponent("Widget", { framework: "browser-test", loader: async () => ({ default: {} }) })
		if (!customElements.get("phoria-island")) PhoriaIsland.register()
		const element = document.createElement("phoria-island") as InstanceType<typeof PhoriaIsland>
		for (const [name, value] of Object.entries(attributes)) element.setAttribute(name, value)
		return { element, mount }
	}

	it("reports a missing component attribute", async () => {
		const { element } = await createIsland()

		await expect(element.connectedCallback()).rejects.toThrow('No "component" attribute specified')
	})

	it("reports an unknown component", async () => {
		const { element } = await createIsland({ component: "Missing", "client:load": "" })
		const error = vi.spyOn(console, "error").mockImplementation(() => {})

		await element.connectedCallback()

		expect(error).toHaveBeenCalledWith(
			expect.stringContaining('Error loading "Missing" component:'),
			expect.objectContaining({ message: 'No component found with name "Missing".' })
		)
	})

	it("reports a missing CSR service", async () => {
		const { registerComponent, registerSsrService } = await import("~/register")
		const { PhoriaIsland } = await import("./phoria-island")
		registerSsrService("ssr-only", { render: async () => ({ framework: "ssr-only", html: "" }) })
		registerComponent("SsrOnly", { framework: "ssr-only", loader: async () => ({ default: {} }) })
		const element = new PhoriaIsland()
		element.setAttribute("component", "SsrOnly")
		element.setAttribute("client:load", "")
		const error = vi.spyOn(console, "error").mockImplementation(() => {})

		await element.connectedCallback()

		expect(error).toHaveBeenCalledWith(
			expect.stringContaining('Error loading "SsrOnly" component:'),
			expect.objectContaining({ message: 'No CSR service could be found for framework "ssr-only".' })
		)
	})

	it("mounts client:only without SSR hydration", async () => {
		const { element, mount } = await createIsland({ component: "Widget", "client:only": "" })

		await element.connectedCallback()

		expect(mount).toHaveBeenCalledWith(element, expect.objectContaining({ name: "Widget" }), null, { mode: "render" })
	})

	it("mounts on client:load connection", async () => {
		const { element, mount } = await createIsland({ component: "Widget", "client:load": "" })

		await element.connectedCallback()

		expect(mount).toHaveBeenCalledWith(element, expect.objectContaining({ name: "Widget" }), null, { mode: "hydrate" })
	})

	it("falls back to the timeout for client:idle", async () => {
		vi.useFakeTimers()
		const { element, mount } = await createIsland({ component: "Widget", "client:idle": "100" })

		await element.connectedCallback()
		expect(mount).not.toHaveBeenCalled()
		await vi.advanceTimersByTimeAsync(100)

		expect(mount).toHaveBeenCalledTimes(1)
	})
})
