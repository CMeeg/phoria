import { describe, expect, it, vi } from "vitest"
import Hello from "../../tests/utilities/hello-fixture.svelte"
import { service } from "./csr"

describe("svelte csr service", () => {
	it("mounts a component into the island element", async () => {
		const island = document.createElement("div")
		document.body.appendChild(island)

		await service.mount(
			island,
			{ name: "Hello", framework: "svelte", loader: async () => ({ default: Hello }) },
			null,
			{
				mode: "render"
			}
		)

		await vi.waitFor(() => expect(island.textContent).toBe("Hello World"))
		island.remove()
	})

	it("hydrates the existing island content", async () => {
		const island = document.createElement("div")
		island.innerHTML = "<span>Hello World</span>"
		const existingContent = island.firstElementChild
		document.body.appendChild(island)

		await service.mount(
			island,
			{ name: "Hello", framework: "svelte", loader: async () => ({ default: Hello }) },
			null,
			{
				mode: "hydrate"
			}
		)

		await vi.waitFor(() => {
			expect(island.textContent).toBe("Hello World")
			expect(island.firstElementChild).toBe(existingContent)
		})
		island.remove()
	})
})
