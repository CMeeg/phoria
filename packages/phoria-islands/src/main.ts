import {
	type PhoriaIslandComponent,
	type PhoriaIslandComponentEntry,
	type PhoriaIslandComponentModule,
	type PhoriaIslandProps,
	importComponent
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
	getComponent,
	getCsrService,
	getFrameworks,
	getSsrService,
	importComponent,
	registerComponent,
	registerComponents,
	registerCsrService,
	registerSsrService
}

export type {
	PhoriaIslandComponent,
	PhoriaIslandComponentEntry,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentOptions,
	PhoriaIslandProps
}
