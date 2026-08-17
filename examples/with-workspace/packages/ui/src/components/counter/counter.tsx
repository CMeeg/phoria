import { useState } from "react"
import reactLogo from "../../assets/react.svg"
import css from "./counter.module.css"

export interface CounterProps {
	startAt?: number
}

export function Counter({ startAt }: CounterProps) {
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
				Edit <code>packages/ui/src/counter.tsx</code> to test HMR
			</p>
		</div>
	)
}
