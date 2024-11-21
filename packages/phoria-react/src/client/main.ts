import { registerCsrService } from "@meeg/phoria"
import { framework } from "~/main"
import { service } from "./csr"

registerCsrService(framework.name, service)
