import { useState } from "react"
import reactLogo from "/react.svg"
import css from "./Counter.module.css"

interface CounterProps {
	startAt?: number
}

function Counter({ startAt }: CounterProps) {
	const [count, setCount] = useState(startAt ?? 0)

	return (
		<div className={`react-counter ${css.counter}`}>
			<a href="https://react.dev" target="_blank" rel="noreferrer">
				<img src={reactLogo} className="logo react" alt="React logo" />
			</a>
			<button type="button" onClick={() => setCount((count) => count + 1)}>
				count is {count}
			</button>
			<p>
				Edit <code>ui/src/app/components/Counter/Counter.tsx</code> to test HMR
			</p>
		</div>
	)
}

export { Counter }
