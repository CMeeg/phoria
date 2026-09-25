import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: () => import("./counter/counter.svelte"),
    framework: "svelte",
  },
})
