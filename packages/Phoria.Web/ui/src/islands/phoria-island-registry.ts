// TODO: Not sure on the structure of the registries - will need to feel it out as we go

interface PhoriaIslandFramework<T> {
	createComponent: (component: PhoriaIslandComponentOptions<T>) => PhoriaIslandComponent
}

// TODO: Can I get rid of the "unknown" type here?
const frameworkRegistry = new Map<string, PhoriaIslandFramework<unknown>>()

function registerFramework<T>(name: string, framework: PhoriaIslandFramework<T>) {
	frameworkRegistry.set(name.toLowerCase(), framework as PhoriaIslandFramework<unknown>)
}

function getFramework(name: string) {
	return frameworkRegistry.get(name.toLowerCase())
}

interface PhoriaIslandComponentOptions<T> {
	loader: () => Promise<T>
	framework: string
}

const componentRegistry = new Map<string, PhoriaIslandComponent>()

interface PhoriaIslandComponent {
	mountComponent: (container: HTMLElement) => Promise<void>
}

function registerComponent<T>(name: string, component: PhoriaIslandComponentOptions<T>) {
	const framework = getFramework(component.framework)

	if (!framework) {
		throw new Error(
			`Cannot register component "${name}" because the "${component.framework}" framework has not been registered.`
		)
	}

	const frameworkComponent = framework.createComponent(component)

	componentRegistry.set(name.toLowerCase(), frameworkComponent)
}

function registerComponents(components: Record<string, PhoriaIslandComponentOptions<unknown>>) {
	for (const [name, component] of Object.entries(components)) {
		registerComponent(name, component)
	}
}

function getComponent(name: string) {
	return componentRegistry.get(name.toLowerCase())
}

export { registerFramework, getFramework, registerComponent, registerComponents, getComponent }

export type { PhoriaIslandFramework, PhoriaIslandComponent }
