import { registerComponents } from "@phoria/phoria"

registerComponents({
  ReactCounter: {
    loader: {
      module: () => import("./counter/counter.tsx"),
      component: (module) => module.Counter,
    },
    framework: "react",
  },
  VueCounter: {
    loader: () => import("./counter/counter-button.vue"),
    framework: "vue",
  },
  SvelteCounter: {
    loader: () => import("./counter/counter.svelte"),
    framework: "svelte",
  },
})
