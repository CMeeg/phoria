import { getPhoriaAppSettings, type PhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
import { type PhoriaLogger, phoriaConsoleLogger } from "./logger"
import { PhoriaIsland, type PhoriaIslandRequest } from "./phoria-island"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	type PhoriaRequestHandler,
	type PhoriaServerEntryLoader
} from "./routing"
import type {
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
} from "./ssr"
import { createPhoriaViteDevServer, type PhoriaViteDevServer } from "./vite"

export type {
	PhoriaAppSettings,
	PhoriaIslandComponentSsrService,
	PhoriaIslandRequest,
	PhoriaIslandSsrResult,
	PhoriaLogger,
	PhoriaRequestHandler,
	PhoriaServerEntry,
	PhoriaServerEntryLoader,
	PhoriaViteDevServer,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	createPhoriaViteDevServer,
	getPhoriaAppSettings,
	PhoriaIsland,
	parsePhoriaAppSettings,
	phoriaConsoleLogger
}
