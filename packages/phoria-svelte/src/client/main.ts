import { registerCsrService } from "@meeg/phoria"
import { framework } from "~/main"
import { service } from "./csr"

// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

registerCsrService(framework.name, service)
