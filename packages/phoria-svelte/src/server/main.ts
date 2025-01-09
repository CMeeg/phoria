import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { type RenderSveltePhoriaIslandComponent, renderComponentToString, service } from "./ssr"

registerSsrService(framework.name, service)

export { renderComponentToString }

export type { RenderSveltePhoriaIslandComponent }
