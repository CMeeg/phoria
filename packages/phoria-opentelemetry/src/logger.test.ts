import { logs, SeverityNumber } from "@opentelemetry/api-logs"
import { InMemoryLogRecordExporter, LoggerProvider, SimpleLogRecordProcessor } from "@opentelemetry/sdk-logs"
import { afterEach, describe, expect, it, vi } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { createPhoriaLogger } from "./logger"

const enabledSettings = createPhoriaOtelAppSettings({
	observability: {
		logging: true,
		tracing: { enabled: false, samplingRatio: 0.1 },
		metrics: false
	}
})

const disabledSettings = createPhoriaOtelAppSettings({ observability: { logging: false } })

afterEach(() => {
	logs.disable()
	vi.restoreAllMocks()
})

describe("createPhoriaLogger", () => {
	it("falls back to console when logging is disabled", () => {
		const infoSpy = vi.spyOn(console, "info").mockImplementation(() => {})
		const warnSpy = vi.spyOn(console, "warn").mockImplementation(() => {})
		const errorSpy = vi.spyOn(console, "error").mockImplementation(() => {})

		const logger = createPhoriaLogger(disabledSettings)
		logger.info("info message", { foo: "bar" })
		logger.warn("warn message")
		logger.error("error message", { err: new Error("boom") })

		expect(infoSpy).toHaveBeenCalledWith("info message", { foo: "bar" })
		expect(warnSpy).toHaveBeenCalledWith("warn message", undefined)
		expect(errorSpy).toHaveBeenCalledWith("error message", { err: new Error("boom") })
	})

	it("emits OTel log records when logging is enabled", async () => {
		const logger = createPhoriaLogger(enabledSettings)

		const exporter = new InMemoryLogRecordExporter()
		const loggerProvider = new LoggerProvider({
			processors: [new SimpleLogRecordProcessor({ exporter })]
		})
		logs.setGlobalLoggerProvider(loggerProvider)

		logger.info("info event", { foo: "bar", skipped: undefined, err: new Error("oops") })
		logger.warn("warn event")
		logger.error("error event", { count: 42 })

		await loggerProvider.forceFlush()

		const records = exporter.getFinishedLogRecords()
		expect(records).toHaveLength(3)

		const [info, warn, error] = records

		expect(info.severityNumber).toBe(SeverityNumber.INFO)
		expect(info.severityText).toBe("INFO")
		expect(info.body).toBe("info event")
		expect(info.eventName).toBe("info event")
		expect(info.attributes).toEqual({
			event: "info event",
			foo: "bar",
			err: "oops"
		})

		expect(warn.severityNumber).toBe(SeverityNumber.WARN)
		expect(warn.severityText).toBe("WARN")
		expect(warn.body).toBe("warn event")
		expect(warn.eventName).toBe("warn event")

		expect(error.severityNumber).toBe(SeverityNumber.ERROR)
		expect(error.severityText).toBe("ERROR")
		expect(error.body).toBe("error event")
		expect(error.attributes).toEqual({
			event: "error event",
			count: "42"
		})
	})
})
