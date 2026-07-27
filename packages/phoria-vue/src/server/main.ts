import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	isVueIsland,
	type RenderVuePhoriaIslandComponent,
	renderComponentToStream,
	renderComponentToString,
	service,
	type VuePhoriaIsland
} from "./ssr"

registerSsrService(framework.name, service)

export type { RenderVuePhoriaIslandComponent, VuePhoriaIsland }
export { isVueIsland, renderComponentToStream, renderComponentToString }
