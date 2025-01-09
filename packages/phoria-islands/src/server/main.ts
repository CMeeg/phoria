import { type PhoriaAppSettings, getPhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
import { PhoriaIsland } from "./phoria-island"
import {
	type PhoriaServerEntryLoader,
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler
} from "./routing"
import type {
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
} from "./ssr"

export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	getPhoriaAppSettings,
	parsePhoriaAppSettings,
	PhoriaIsland
}

export type {
	PhoriaAppSettings,
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	PhoriaServerEntryLoader,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
