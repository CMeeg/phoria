import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	isReactIsland,
	type ReactPhoriaIsland,
	type RenderReactPhoriaIslandComponent,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export type { ReactPhoriaIsland, RenderReactPhoriaIslandComponent }
export { isReactIsland, renderComponentToStream, renderComponentToString }
