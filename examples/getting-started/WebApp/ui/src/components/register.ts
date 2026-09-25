import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: {
      module: () => import("./counter/counter.tsx"),
      component: (module) => module.Counter,
    },
    framework: "react",
  },
})
