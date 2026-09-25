import type { PhoriaOtelAppSettings } from "../../src/appsettings"

export function createPhoriaOtelAppSettings(overrides: Partial<PhoriaOtelAppSettings> = {}): PhoriaOtelAppSettings {
	return {
		root: "ui",
		base: "/ui",
		entry: "entry.ts",
		ssrBase: "/ssr",
		ssrEntry: "ssr.ts",
		server: { host: "localhost", https: false },
		build: { outDir: "dist" },
		...overrides
	}
}
