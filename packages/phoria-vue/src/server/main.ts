import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type RenderVuePhoriaIslandComponent,
	type VuePhoriaIsland,
	isVueIsland,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { isVueIsland, renderComponentToStream, renderComponentToString }

export type { RenderVuePhoriaIslandComponent, VuePhoriaIsland }
