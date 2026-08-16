import { inspectConfig } from "@meeg/vite-plugin-inspect-config"
import { phoria } from "@phoria/phoria/vite"
import { phoriaReact } from "@phoria/phoria-react/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

export default defineConfig({
  plugins: [dotnetDevCerts(), phoria(), phoriaReact(), inspectConfig()],
})
