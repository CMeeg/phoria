import {
	importComponent,
	type PhoriaIslandComponent,
	type PhoriaIslandComponentEntry,
	type PhoriaIslandComponentModule,
	type PhoriaIslandProps
} from "./phoria-island"
import {
	getComponent,
	getCsrService,
	getFrameworks,
	getSsrService,
	type PhoriaIslandComponentOptions,
	registerComponent,
	registerComponents,
	registerCsrService,
	registerSsrService
} from "./register"

export type {
	PhoriaIslandComponent,
	PhoriaIslandComponentEntry,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentOptions,
	PhoriaIslandProps
}
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
