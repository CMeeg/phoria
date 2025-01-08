import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type RenderVuePhoriaIslandComponent,
	renderComponentToStream,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { renderComponentToStream, renderComponentToString }

export type { RenderVuePhoriaIslandComponent }
