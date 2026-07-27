import { getPhoriaAppSettings, type PhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
import { PhoriaIsland } from "./phoria-island"
import {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
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
	PhoriaIslandSsrResult,
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
