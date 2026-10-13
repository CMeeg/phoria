import { createElement } from "react"
import { describe, expect, it, vi } from "vitest"
import { service } from "./csr"

describe("react csr service", () => {
	it("renders a component into the island element", async () => {
		const island = document.createElement("div")
		document.body.appendChild(island)

		const Hello = () => createElement("span", null, "Hello World")

		await service.mount(island, { name: "Hello", framework: "react", loader: async () => ({ default: Hello }) }, null, {
			mode: "render"
		})

		await vi.waitFor(() => expect(island.textContent).toContain("Hello World"))

		island.remove()
	})

	it("hydrates the existing island content", async () => {
		const island = document.createElement("div")
		island.innerHTML = "<span>Hello World</span>"
		const existingContent = island.firstElementChild
		document.body.appendChild(island)

		const Hello = () => createElement("span", null, "Hello World")

		await service.mount(island, { name: "Hello", framework: "react", loader: async () => ({ default: Hello }) }, null, {
			mode: "hydrate"
		})

		await vi.waitFor(() => {
			expect(island.textContent).toBe("Hello World")
			expect(island.firstElementChild).toBe(existingContent)
		})

		island.remove()
	})

	it("defines the react-refresh globals so islands can hydrate without Vite's HTML preamble", async () => {
		// Phoria serves HTML from the .NET host, so Vite's dev-only react-refresh
		// preamble (window.$RefreshReg$/$RefreshSig$, normally injected via
		// transformIndexHtml) never runs. Without it, @vitejs/plugin-react's
		// refresh transform throws "can't detect preamble" the first time a
		// compiled component module evaluates.

		window.$RefreshReg$ = undefined
		window.$RefreshSig$ = undefined

		const island = document.createElement("div")
		document.body.appendChild(island)

		const Hello = () => createElement("span", null, "Hello World")

		await service.mount(island, { name: "Hello", framework: "react", loader: async () => ({ default: Hello }) }, null, {
			mode: "render"
		})

		expect(typeof window.$RefreshReg$).toBe("function")
		expect(typeof window.$RefreshSig$).toBe("function")

		island.remove()
	})
})
