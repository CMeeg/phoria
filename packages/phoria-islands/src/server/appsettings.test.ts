import { describe, expect, it } from "vitest"
import { parsePhoriaAppSettings } from "./appsettings"

describe("parsePhoriaAppSettings", () => {
	it("throws when `entry` is missing", async () => {
		await expect(
			parsePhoriaAppSettings({ cwd: "/nonexistent-path-for-test", inlineSettings: { ssrEntry: "ssr.ts" } })
		).rejects.toThrow("`entry` is required in `Phoria` app settings.")
	})

	it("applies default root and base when not provided", async () => {
		const settings = await parsePhoriaAppSettings({
			cwd: "/nonexistent-path-for-test",
			inlineSettings: { entry: "entry.ts", ssrEntry: "ssr.ts" }
		})

		expect(settings.root).toBe("ui")
		expect(settings.base).toBe("/ui")
		expect(settings.server.port).toBe(5173)
	})

	it("retains generic extension settings", async () => {
		type Extension = { observability?: { logging: boolean } }
		const settings = await parsePhoriaAppSettings<Extension>({
			cwd: "/nonexistent-path-for-test",
			inlineSettings: {
				entry: "entry.ts",
				ssrEntry: "ssr.ts",
				observability: { logging: true }
			}
		})

		expect(settings.observability?.logging).toBe(true)
		expect(settings.root).toBe("ui")
	})
})
