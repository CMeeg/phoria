import type { Component } from "svelte"
import { describe, expect, it } from "vitest"
import { isSvelteIsland, renderComponentToString, service } from "./ssr"

const Hello = Object.assign(
	(internals: { push: (html: string) => void }) => internals.push("<span>Hello World</span>"),
	{ render: () => ({ html: "<span>Hello World</span>", head: "", css: null }) }
) as unknown as Component

describe("svelte ssr service", () => {
	it("renders a hand-rolled component to a string", () => {
		const html = renderComponentToString({ component: Hello, componentName: "Hello", framework: "svelte" }, null)

		expect(html).toContain("<span>Hello World</span>")
	})

	it("uses a renderComponent override", async () => {
		const result = await service.render(
			{ name: "Hello", framework: "svelte", loader: async () => ({ default: Hello }) },
			null,
			{ renderComponent: async () => "<span>Override</span>" }
		)

		expect(result.html).toBe("<span>Override</span>")
	})

	it("rejects components from another framework", async () => {
		await expect(
			service.render({ name: "Counter", framework: "react", loader: async () => ({ default: {} }) } as never, null)
		).rejects.toThrow('svelte cannot render the react component named "Counter".')
	})

	it("identifies svelte islands", () => {
		expect(isSvelteIsland({ framework: "svelte" } as Parameters<typeof isSvelteIsland>[0])).toBe(true)
		expect(isSvelteIsland({ framework: "react" } as Parameters<typeof isSvelteIsland>[0])).toBe(false)
	})
})
