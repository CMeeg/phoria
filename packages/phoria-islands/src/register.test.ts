import { beforeEach, describe, expect, it, vi } from "vitest"

describe("register", () => {
	beforeEach(() => {
		vi.resetModules()
	})

	it("throws when registering a component for an unregistered framework", async () => {
		const { registerComponent } = await import("./register")

		expect(() => registerComponent("Widget", { framework: "unknown", loader: async () => ({ default: {} }) })).toThrow(
			'the "unknown" framework has not been registered'
		)
	})

	it("registers a component after its framework is registered and looks it up case-insensitively", async () => {
		const { registerCsrService, registerComponent, getComponent } = await import("./register")

		registerCsrService("React", { mount: async () => {} })
		registerComponent("Widget", { framework: "React", loader: async () => ({ default: {} }) })

		const entry = getComponent("widget")

		expect(entry?.name).toBe("Widget")
		expect(entry?.framework).toBe("React")
	})

	it("normalises framework names to lowercase in getFrameworks", async () => {
		const { registerCsrService, getFrameworks } = await import("./register")

		registerCsrService("Vue", { mount: async () => {} })

		expect(getFrameworks()).toContain("vue")
	})
})
