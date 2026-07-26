import type { Plugin } from "vite"
import { describe, expect, it } from "vitest"
import { dotnetDevCerts } from "./plugin"

describe("dotnetDevCerts plugin", () => {
	it("returns a vite plugin with the expected name", () => {
		const plugin = dotnetDevCerts() as Plugin
		expect(plugin).toHaveProperty("name")
		expect(plugin.name).toBe("dotnet-dev-certs")
	})
})
