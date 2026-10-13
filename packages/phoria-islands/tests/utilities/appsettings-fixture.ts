import type { PhoriaAppSettings } from "../../src/server/appsettings"

export function createPhoriaAppSettings(overrides: Partial<PhoriaAppSettings> = {}): PhoriaAppSettings {
	return {
		root: "ui",
		base: "/ui",
		entry: "entry.ts",
		ssrBase: "/ssr",
		ssrEntry: "entry.ts",
		server: { host: "localhost", https: false },
		build: { outDir: "dist" },
		...overrides
	}
}
