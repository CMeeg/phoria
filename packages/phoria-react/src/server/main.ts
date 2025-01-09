import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { type RenderReactPhoriaIslandComponent, renderComponentToStream, renderComponentToString, service } from "./ssr"

registerSsrService(framework.name, service)

export { renderComponentToStream, renderComponentToString }

export type { RenderReactPhoriaIslandComponent }
