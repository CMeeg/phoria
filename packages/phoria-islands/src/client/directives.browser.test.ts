import { describe, expect, it, vi } from "vitest"
import { media, visible } from "./directives"

describe("visible directive", () => {
	it("calls mount when the element intersects the viewport", async () => {
		const element = document.createElement("div")
		document.body.appendChild(element)

		const mount = vi.fn(async () => {})

		await visible(mount, { element: element as never, component: "Widget", value: null })

		await vi.waitFor(() => expect(mount).toHaveBeenCalledTimes(1), { timeout: 2000 })

		element.remove()
	})
})

describe("media directive", () => {
	it("throws when no query is provided", async () => {
		const mount = vi.fn(async () => {})

		await expect(media(mount, { element: {} as never, component: "Widget", value: null })).rejects.toThrow(
			'No "query" specified'
		)
	})

	it("mounts immediately when the media query already matches", async () => {
		const mount = vi.fn(async () => {})

		await media(mount, { element: {} as never, component: "Widget", value: "all" })

		await vi.waitFor(() => expect(mount).toHaveBeenCalledTimes(1), { timeout: 2000 })
	})
})
