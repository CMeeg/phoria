import type { Preview } from "@storybook/react-vite"
import "../ui/src/styles/global.css"

const preview: Preview = {
	decorators: [
		(Story) => {
			return (
				<div className="bg-neutral-50 dark:bg-neutral-800 text-neutral-900 dark:text-neutral-50">
					<div className="flex m-0 min-h-[100vh] min-w-[320px] place-items-center">
						<div className="max-w-[1280px] mx-auto my-0 p-8 text-center">
							<Story />
						</div>
					</div>
				</div>
			)
		}
	]
}

export default preview
