import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { configureReactSsr, renderIslandToStream, renderIslandToString, service } from "./ssr"

registerSsrService(framework.name, service)

export { configureReactSsr, renderIslandToStream, renderIslandToString }
