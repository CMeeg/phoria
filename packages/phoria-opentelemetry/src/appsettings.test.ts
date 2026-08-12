import { describe, expect, it, vi } from "vitest"
import type { PhoriaOtelAppSettings } from "./appsettings"
import { createPhoriaLogger } from "./logger"

const appsettings: PhoriaOtelAppSettings = {
	root: "ui",
	base: "/ui",
	entry: "src/entry-client.ts",
	ssrBase: "/ssr",
	ssrEntry: "src/entry-server.ts",
	server: {
		host: "localhost",
		https: false
	},
	build: {
		outDir: "dist"
	},
	observability: {
		logging: false,
		tracing: {
			enabled: false,
			samplingRatio: 0.1
		},
		metrics: false
	}
}

describe("PhoriaOtelAppSettings", () => {
	it("normalizes missing observability values through the OTel logger factory", () => {
		const info = vi.spyOn(console, "info").mockImplementation(() => {})
		const logger = createPhoriaLogger({ ...appsettings, observability: undefined })

		logger.info("message")

		expect(info).toHaveBeenCalledWith("message", undefined)
		info.mockRestore()
	})
})
