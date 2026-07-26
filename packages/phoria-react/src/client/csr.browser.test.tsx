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
})
