import { type PhoriaAppSettings, getPhoriaAppSettings, parsePhoriaAppSettings } from "./appsettings"
import {
	type PhoriaServerEntryLoader,
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler
} from "./routing"
import { type PhoriaServerEntry, serverEntry } from "./server-entry"

export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler,
	getPhoriaAppSettings,
	parsePhoriaAppSettings,
	serverEntry
}

export type { PhoriaAppSettings, PhoriaServerEntry, PhoriaServerEntryLoader }
