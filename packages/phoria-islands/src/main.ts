import {
	type PhoriaIsland,
	type PhoriaIslandComponent,
	type PhoriaIslandComponentModule,
	type PhoriaIslandImport,
	type PhoriaIslandProps,
	createIslandImport
} from "./phoria-island"
import {
	type PhoriaIslandComponentOptions,
	getComponent,
	getCsrService,
	getFrameworks,
	getSsrService,
	registerComponent,
	registerComponents,
	registerCsrService,
	registerSsrService
} from "./register"

export {
	createIslandImport,
	getComponent,
	getCsrService,
	getFrameworks,
	getSsrService,
	registerComponent,
	registerComponents,
	registerCsrService,
	registerSsrService
}

export type {
	PhoriaIsland,
	PhoriaIslandComponent,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentOptions,
	PhoriaIslandImport,
	PhoriaIslandProps
}
