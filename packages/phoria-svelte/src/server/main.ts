import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import {
	type PhoriaSvelteSsrOptions,
	type RenderSveltePhoriaIslandComponent,
	configureSvelteSsrService,
	renderComponentToString,
	service
} from "./ssr"

registerSsrService(framework.name, service)

export { configureSvelteSsrService, renderComponentToString }

export type { PhoriaSvelteSsrOptions, RenderSveltePhoriaIslandComponent }
