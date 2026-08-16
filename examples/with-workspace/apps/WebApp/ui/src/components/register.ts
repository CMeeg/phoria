import { registerComponents } from "@phoria/phoria"

registerComponents({
	Counter: {
		loader: {
			module: () => import("./counter"),
			component: (module) => module.Counter
		},
		framework: "react"
	}
})
