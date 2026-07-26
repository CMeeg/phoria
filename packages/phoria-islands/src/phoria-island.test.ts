import { describe, expect, it } from "vitest"
import { importComponent } from "./phoria-island"

describe("importComponent", () => {
	it("resolves the default export loader", async () => {
		const result = await importComponent({
			name: "Widget",
			framework: "react",
			loader: async () => ({ default: "component", __phoriaComponentPath: "/widget.tsx" })
		})

		expect(result.component).toBe("component")
		expect(result.componentName).toBe("Widget")
		expect(result.componentPath).toBe("/widget.tsx")
	})

	it("throws when the default export is missing", async () => {
		await expect(importComponent({ name: "Widget", framework: "react", loader: async () => ({}) })).rejects.toThrow(
			"must be exposed as the default export"
		)
	})

	it("resolves a named export via the module/component loader pair", async () => {
		const result = await importComponent({
			name: "Widget",
			framework: "react",
			loader: {
				module: async () => ({ Named: "named-component", __phoriaComponentPath: "/w.tsx" }),
				component: (m) => m.Named
			}
		})

		expect(result.component).toBe("named-component")
		expect(result.componentPath).toBe("/w.tsx")
	})
})
