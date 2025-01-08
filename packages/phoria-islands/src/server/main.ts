import { type PhoriaAppSettings, getPhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
import { PhoriaIsland } from "./phoria-island"
import {
	type PhoriaServerEntryLoader,
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler
} from "./routing"
import type { PhoriaServerEntry } from "./server-entry"

export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	getPhoriaAppSettings,
	parsePhoriaAppSettings,
	PhoriaIsland
}

export type { PhoriaAppSettings, PhoriaServerEntry, PhoriaServerEntryLoader }
