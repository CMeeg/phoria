import { createElement } from "react"
import { describe, expect, it } from "vitest"
import { isReactIsland, renderComponentToString, service } from "./ssr"

describe("react ssr service", () => {
	it("renders an inline component to a string", () => {
		const Hello = () => createElement("span", null, "Hello React")

		const html = renderComponentToString({ component: Hello, componentName: "Hello", framework: "react" }, null)

		expect(html).toContain("Hello React")
	})

	it("rejects components from another framework", async () => {
		await expect(
			service.render({ name: "Counter", framework: "svelte", loader: async () => ({ default: {} }) } as never, null)
		).rejects.toThrow('react cannot render the svelte component named "Counter".')
	})

	it("identifies react islands", () => {
		expect(isReactIsland({ framework: "react" } as Parameters<typeof isReactIsland>[0])).toBe(true)
		expect(isReactIsland({ framework: "svelte" } as Parameters<typeof isReactIsland>[0])).toBe(false)
	})
})
