import { mkdtemp, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { afterEach, describe, expect, it } from "vitest"
import { getPhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"

const tempDirectories: string[] = []

afterEach(async () => {
	for (const directory of tempDirectories.splice(0)) {
		await rm(directory, { recursive: true, force: true })
	}
})

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

	it("merges the explicitly selected environment appsettings file", async () => {
		const cwd = await mkdtemp(join(tmpdir(), "phoria-appsettings-"))
		tempDirectories.push(cwd)
		await writeFile(
			join(cwd, "appsettings.json"),
			JSON.stringify({ phoria: { entry: "base.ts", ssrEntry: "base-ssr.ts", server: { port: 5000 } } })
		)
		await writeFile(
			join(cwd, "appsettings.Development.json"),
			JSON.stringify({ phoria: { entry: "development.ts", ssrEntry: "development-ssr.ts", server: { https: true } } })
		)

		const settings = await parsePhoriaAppSettings({ cwd, environment: "Development" })

		expect(settings.entry).toBe("development.ts")
		expect(settings.server).toMatchObject({ port: 5000, https: true })
	})

	it("uses an explicitly supplied environment for getPhoriaAppSettings", async () => {
		const cwd = await mkdtemp(join(tmpdir(), "phoria-appsettings-"))
		tempDirectories.push(cwd)
		await writeFile(join(cwd, "appsettings.json"), JSON.stringify({ phoria: { entry: "base.ts" } }))
		await writeFile(join(cwd, "appsettings.Development.json"), JSON.stringify({ phoria: { entry: "development.ts" } }))

		const settings = await getPhoriaAppSettings({ cwd, environment: "Development" })

		expect(settings.entry).toBe("development.ts")
	})
})
