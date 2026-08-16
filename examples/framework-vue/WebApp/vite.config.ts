import { inspectConfig } from "@meeg/vite-plugin-inspect-config"
import { phoria } from "@phoria/phoria/vite"
import { phoriaVue } from "@phoria/phoria-vue/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

export default defineConfig({
  resolve: {
    tsconfigPaths: true,
  },
  plugins: [dotnetDevCerts(), phoria(), phoriaVue(), inspectConfig()],
})
