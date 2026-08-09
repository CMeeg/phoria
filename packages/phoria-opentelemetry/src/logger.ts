import { logs, SeverityNumber } from "@opentelemetry/api-logs"
import type { PhoriaObservabilitySettings } from "./appsettings"

interface PhoriaLogger {
	info(message: string, data?: Record<string, unknown>): void
	warn(message: string, data?: Record<string, unknown>): void
	error(message: string, data?: Record<string, unknown>): void
}

function createPhoriaLogger(settings: PhoriaObservabilitySettings): PhoriaLogger {
	if (!settings.logging) {
		return {
			info: (message, data) => console.info(message, data),
			warn: (message, data) => console.warn(message, data),
			error: (message, data) => console.error(message, data)
		}
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

export type { PhoriaLogger }
export { createPhoriaLogger }
