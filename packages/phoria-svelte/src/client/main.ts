import { registerCsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { service } from "./csr"

registerCsrService(framework.name, service)
