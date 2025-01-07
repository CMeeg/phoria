import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type PhoriaVueSsrOptions,
	type RenderVuePhoriaIslandComponent,
	configureVueSsrService,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { configureVueSsrService, renderComponentToStream, renderComponentToString }

export type { PhoriaVueSsrOptions, RenderVuePhoriaIslandComponent }
