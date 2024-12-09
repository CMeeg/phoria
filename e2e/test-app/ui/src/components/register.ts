import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: () => import("./Counter/Counter.tsx"),
    framework: "react"
  }
})
