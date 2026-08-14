import { describe, expect, it, vi } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { createPhoriaLogger } from "./logger"

describe("PhoriaOtelAppSettings", () => {
	it("normalizes missing observability values through the OTel logger factory", () => {
		const info = vi.spyOn(console, "info").mockImplementation(() => {})
		const logger = createPhoriaLogger(createPhoriaOtelAppSettings())

		logger.info("message")

		expect(info).toHaveBeenCalledWith("message", undefined)
		info.mockRestore()
	})
})
