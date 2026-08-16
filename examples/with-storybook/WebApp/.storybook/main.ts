import type { StorybookConfig } from "@storybook/react-vite"
import { mergeConfig } from "vite"
import viteConfig from "../vite.config.ts"

const storybookViteConfig = {
  ...viteConfig,
  plugins: viteConfig.plugins?.flat().filter((plugin) => plugin && !plugin.name?.startsWith("phoria")),
}

const config: StorybookConfig = {
  core: {
    builder: {
      name: "@storybook/builder-vite",
      options: {
        viteConfigPath: "./.storybook/vite.config.ts",
      },
    },
  },
  framework: {
    name: "@storybook/react-vite",
    options: {},
  },
  stories: ["../ui/src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  viteFinal: async (config) => {
    const mergedConfig = mergeConfig(config, storybookViteConfig)

    return mergeConfig(mergedConfig, {
      build: { rolldownOptions: { input: undefined } },
      plugins: mergedConfig.plugins?.flat().filter((plugin) => plugin && !plugin.name?.startsWith("phoria")),
      publicDir: "ui/public",
      root: process.cwd(),
    })
  },
}

export default config
