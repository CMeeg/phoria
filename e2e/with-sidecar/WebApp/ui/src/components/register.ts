import { registerComponents } from "@phoria/phoria"

registerComponents({
	ReactCounter: {
		loader: {
			module: () => import("./Counter.tsx"),
			component: (module) => module.Counter
		},
		framework: "react"
	}
})
