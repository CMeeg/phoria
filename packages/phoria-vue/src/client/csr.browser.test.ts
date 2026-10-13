import { describe, expect, it, vi } from "vitest"
import { h } from "vue"
import { service } from "./csr"

describe("vue csr service", () => {
	it("mounts an options component into the island element", async () => {
		const island = document.createElement("div")
		document.body.appendChild(island)
		const Hello = { render: () => h("span", "Hello World") }

		await service.mount(island, { name: "Hello", framework: "vue", loader: async () => ({ default: Hello }) }, null)

		await vi.waitFor(() => expect(island.textContent).toBe("Hello World"))
		island.remove()
	})
})
