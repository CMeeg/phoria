import { registerSsrService } from "@meeg/phoria"
import { framework } from "~/main"
import { service } from "./ssr"

registerSsrService(framework.name, service)
