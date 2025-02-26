import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type RenderSveltePhoriaIslandComponent,
	type SveltePhoriaIsland,
	isSvelteIsland,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { isSvelteIsland, renderComponentToString }

export type { RenderSveltePhoriaIslandComponent, SveltePhoriaIsland }
