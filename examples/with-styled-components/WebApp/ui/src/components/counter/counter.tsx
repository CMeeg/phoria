import { useState } from "react"
import { styled } from "styled-components"
import reactLogo from "/react.svg"

export interface CounterProps {
  startAt?: number
}

const CounterContainer = styled.div`
  padding: 2rem;
  border: 1px solid #646cff;
  border-radius: 8px;
`

const CounterButton = styled.button`
  padding: 0.75rem 1.25rem;
  border: 1px solid #646cff;
  border-radius: 6px;
  background: #ffffff;
  color: #213547;
  cursor: pointer;
`

export function Counter({ startAt }: CounterProps) {
  const [count, setCount] = useState(startAt ?? 0)

  return (
    <CounterContainer className="react-counter">
      <a href="https://react.dev" target="_blank" rel="noreferrer">
        <img src={reactLogo} className="logo react" alt="React logo" />
      </a>
      <CounterButton type="button" onClick={() => setCount((count) => count + 1)}>
        count is {count}
      </CounterButton>
      <p>
        Edit <code>ui/src/components/counter/counter.tsx</code> to test HMR
      </p>
    </CounterContainer>
  )
}
