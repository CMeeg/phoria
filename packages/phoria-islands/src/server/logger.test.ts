import { afterEach, describe, expect, it, vi } from "vitest"
import { phoriaConsoleLogger } from "./logger"

describe("phoriaConsoleLogger", () => {
	afterEach(() => {
		vi.restoreAllMocks()
	})

	it("delegates info messages to console.info", () => {
		const info = vi.spyOn(console, "info").mockImplementation(() => {})
		const data = { value: "test" }

		phoriaConsoleLogger.info("message", data)

		expect(info).toHaveBeenCalledWith("message", data)
	})
})
