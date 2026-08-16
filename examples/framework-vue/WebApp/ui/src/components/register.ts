import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: {
      module: () => import("./counter/counter-button.vue"),
      component: (module) => module.default,
    },
    framework: "vue",
  },
})
