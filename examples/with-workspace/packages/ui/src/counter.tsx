import { useState } from "react"

export interface CounterProps {
	startAt?: number
}

export function Counter({ startAt }: CounterProps) {
	const [count, setCount] = useState(startAt ?? 0)

	return (
		<div className="react-counter">
			<a href="https://react.dev" target="_blank" rel="noreferrer">
				React
			</a>
			<button type="button" onClick={() => setCount((value) => value + 1)}>
				count is {count}
			</button>
		</div>
	)
}
