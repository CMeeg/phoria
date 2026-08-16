interface PhoriaLogger {
	info(message: string, data?: Record<string, unknown>): void
	warn(message: string, data?: Record<string, unknown>): void
	error(message: string, data?: Record<string, unknown>): void
}

const phoriaConsoleLogger: PhoriaLogger = {
	info: (message, data) => console.info(message, data),
	warn: (message, data) => console.warn(message, data),
	error: (message, data) => console.error(message, data)
}

export type { PhoriaLogger }
export { phoriaConsoleLogger }
