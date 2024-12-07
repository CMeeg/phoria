import { join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, test } from "vitest"
import appsettingsJson from "./_data/appsettings.json" with { type: "json" }
import appsettingsProductionJson from "./_data/appsettings.production.json" with { type: "json" }
import { type PhoriaAppSettings, defaultAppsettings, getPhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"

const entry = "ui/src/entry-client.ts"
const ssrEntry = "ui/src/entry-server.ts"
const dataCwd = fileURLToPath(join(new URL(import.meta.url).href, "../_data"))

const inlineSettings: PhoriaAppSettings = {
	Root: "test",
	Base: "/test",
	Entry: entry,
	SsrBase: "/ssrtest",
	SsrEntry: ssrEntry,
	Server: {
		Host: "test.com",
		Port: 8080,
		Https: true
	},
	Build: {
		OutDir: "build"
	}
}

describe("getPhoriaAppSettings", () => {
	test("with invalid file source will error", async () => {
		await expect(() => getPhoriaAppSettings({ cwd: dataCwd, fileName: "error.txt" })).rejects.toThrowError()
	})

	test("with `inlineSettings` will parse", async () => {
		const appsettings = await getPhoriaAppSettings({ inlineSettings })

		const expectedAppsettings = {
			...defaultAppsettings,
			...inlineSettings
		}

		expect(appsettings).toEqual(expectedAppsettings)
	})

	test("with `appsettings.json` file will parse", async () => {
		const appsettings = await getPhoriaAppSettings({ cwd: dataCwd })

		const expectedAppsettings = {
			...appsettingsJson.Phoria
		}

		expect(appsettings).toEqual(expectedAppsettings)
	})

	test("with `appsettings.json` and `appsettings.{env}.json` files will parse", async () => {
		const appsettings = await getPhoriaAppSettings({ cwd: dataCwd, environment: "production" })

		const expectedAppsettings = {
			...appsettingsJson.Phoria,
			...appsettingsProductionJson.Phoria
		}

		expect(appsettings).toEqual(expectedAppsettings)
	})

	test("with missing `appsettings.{env}.json` file will parse", async () => {
		const appsettings = await getPhoriaAppSettings({
			cwd: dataCwd,
			environment: "development",
			inlineSettings: { Entry: entry, SsrEntry: ssrEntry }
		})

		const expectedAppsettings = {
			...appsettingsJson.Phoria
		}

		expect(appsettings).toEqual(expectedAppsettings)
	})
})

describe("parsePhoriaAppSettings", () => {
	test("with no settings source will error", async () => {
		await expect(() => parsePhoriaAppSettings()).rejects.toThrowError()
	})

	test("with no `SsrEntry` will error", async () => {
		await expect(() => parsePhoriaAppSettings({ inlineSettings: { Entry: entry } })).rejects.toThrowError()
	})

	test("with no `Entry` will error", async () => {
		await expect(() => parsePhoriaAppSettings({ inlineSettings: { SsrEntry: ssrEntry } })).rejects.toThrowError()
	})

	test("with `Entry` and `SsrEntry` will parse", async () => {
		const appsettings = await parsePhoriaAppSettings({ inlineSettings: { Entry: entry, SsrEntry: ssrEntry } })

		expect(appsettings.Entry).toEqual(entry)
		expect(appsettings.SsrEntry).toEqual(ssrEntry)
	})
})
