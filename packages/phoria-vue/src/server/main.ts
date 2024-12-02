import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { service } from "./ssr"

registerSsrService(framework.name, service)
