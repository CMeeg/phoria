import { describe, expect, it } from "vitest"
import { h } from "vue"
import { isVueIsland, renderComponentToString, service } from "./ssr"

const Hello = { render: () => h("span", "Hello World") }

describe("vue ssr service", () => {
	it("renders an options component to a string", async () => {
		const html = await renderComponentToString({ component: Hello, componentName: "Hello", framework: "vue" }, null)

		expect(html).toContain("Hello World")
	})

	it("uses a renderComponent override", async () => {
		const result = await service.render(
			{ name: "Hello", framework: "vue", loader: async () => ({ default: Hello }) },
			null,
			{ renderComponent: async () => "<span>Override</span>" }
		)

		expect(result.html).toBe("<span>Override</span>")
	})

	it("rejects components from another framework", async () => {
		await expect(
			service.render({ name: "Counter", framework: "react", loader: async () => ({ default: {} }) } as never, null)
		).rejects.toThrow('vue cannot render the react component named "Counter".')
	})

	it("identifies vue islands", () => {
		expect(isVueIsland({ framework: "vue" } as Parameters<typeof isVueIsland>[0])).toBe(true)
		expect(isVueIsland({ framework: "react" } as Parameters<typeof isVueIsland>[0])).toBe(false)
	})
})
