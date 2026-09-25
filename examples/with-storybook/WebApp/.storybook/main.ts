import type { StorybookConfig } from "@storybook/react-vite"
import tailwindcss from "@tailwindcss/vite"
import { mergeConfig } from "vite"

const config: StorybookConfig = {
  framework: {
    name: "@storybook/react-vite",
    options: {
      builder: {
        // Keep Storybook from loading the application's Vite config, which configures Phoria's app entry.
        viteConfigPath: ".storybook/vite.config.ts",
      },
    },
  },
  stories: ["../ui/src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  viteFinal: async (config) => {
    return mergeConfig(config, {
      plugins: [tailwindcss()],
      resolve: { tsconfigPaths: true },
      publicDir: "ui/public",
      root: process.cwd(),
    })
  },
}

export default config
