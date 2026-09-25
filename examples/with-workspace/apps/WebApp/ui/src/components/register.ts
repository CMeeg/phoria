import { registerComponents } from "@phoria/phoria"

registerComponents({
	Counter: {
		loader: {
			module: () => import("@phoriaexamples/ui"),
			component: (module) => module.Counter
		},
		framework: "react"
	}
})
