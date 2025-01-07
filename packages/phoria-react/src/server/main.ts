import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type PhoriaReactSsrOptions,
	type RenderReactPhoriaIslandComponent,
	configureReactSsrService,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { configureReactSsrService, renderComponentToStream, renderComponentToString }

export type { PhoriaReactSsrOptions, RenderReactPhoriaIslandComponent }
