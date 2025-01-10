import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type ReactPhoriaIsland,
	type RenderReactPhoriaIslandComponent,
	isReactIsland,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { isReactIsland, renderComponentToStream, renderComponentToString }

export type { ReactPhoriaIsland, RenderReactPhoriaIslandComponent }
