import { logs, SeverityNumber } from "@opentelemetry/api-logs"
import { type PhoriaLogger, phoriaConsoleLogger } from "@phoria/phoria/server"
import { getPhoriaObservabilityAppSettings, type PhoriaOtelAppSettings } from "./appsettings"

function createPhoriaLogger(appsettings: PhoriaOtelAppSettings): PhoriaLogger {
	const settings = getPhoriaObservabilityAppSettings(appsettings)

	if (!settings.logging) {
		return phoriaConsoleLogger
	}

	const logger = logs.getLogger("phoria-server")

	function emit(message: string, severityNumber: SeverityNumber, attributes: Record<string, unknown> = {}) {
		logger.emit({
			severityNumber,
			severityText: SeverityNumber[severityNumber],
			body: message,
			eventName: message,
			attributes: {
				event: message,
				...Object.fromEntries(
					Object.entries(attributes)
						.filter(([, value]) => value !== undefined)
						.map(([key, value]) => [key, value instanceof Error ? value.message : String(value)])
				)
			}
		})
	}

	return {
		info: (message, data) => emit(message, SeverityNumber.INFO, data),
		warn: (message, data) => emit(message, SeverityNumber.WARN, data),
		error: (message, data) => emit(message, SeverityNumber.ERROR, data)
	}
}

export { createPhoriaLogger }
