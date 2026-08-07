import { useState } from "react"

export interface CounterProps {
  startAt?: number
}

export function Counter({ startAt = 0 }: CounterProps) {
  const [count, setCount] = useState(startAt)

  return (
    <button type="button" onClick={() => setCount((value) => value + 1)}>
      count is {count}
    </button>
  )
}
