import { getPhoriaAppSettings, type PhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
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

export type {
	PhoriaAppSettings,
	PhoriaIslandComponentSsrService,
	PhoriaIslandRequest,
	PhoriaIslandSsrResult,
	PhoriaRequestHandler,
	PhoriaServerEntry,
	PhoriaServerEntryLoader,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	getPhoriaAppSettings,
	PhoriaIsland,
	parsePhoriaAppSettings
}
