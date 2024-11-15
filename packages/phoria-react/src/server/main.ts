import { registerSsrService } from "@meeg/phoria"
import { framework } from "~/main"
import { service } from "./ssr"

// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

registerSsrService(framework.name, service)
