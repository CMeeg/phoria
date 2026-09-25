import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	isSvelteIsland,
	type RenderSveltePhoriaIslandComponent,
	renderComponentToString,
	type SveltePhoriaIsland,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export type { RenderSveltePhoriaIslandComponent, SveltePhoriaIsland }
export { isSvelteIsland, renderComponentToString }
