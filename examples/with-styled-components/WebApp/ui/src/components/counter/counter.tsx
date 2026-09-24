import { useState } from "react"
import { keyframes, styled } from "styled-components"
import reactLogo from "/react.svg"

export interface CounterProps {
  startAt?: number
}

const spin = keyframes`
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
`

const Logo = styled.img`
  &:hover {
    filter: drop-shadow(0 0 2em #61dafbaa);
  }

  @media (prefers-reduced-motion: no-preference) {
    animation: ${spin} infinite 20s linear;
  }
`

const Button = styled.button<{ $count?: number }>`
  display: block;
  margin: auto;
  opacity: ${({ $count }) => (($count ?? 0) % 2 === 0 ? "1" : "0.8")};
`

export function Counter({ startAt }: CounterProps) {
  const [count, setCount] = useState(startAt ?? 0)

  return (
    <div>
      <a href="https://react.dev" target="_blank" rel="noreferrer">
        <Logo src={reactLogo} className="logo" alt="React logo" />
      </a>
      <Button type="button" $count={count} onClick={() => setCount((count) => count + 1)}>
        count is {count}
      </Button>
      <p>
        Edit <code>ui/src/components/counter/counter.tsx</code> to test HMR
      </p>
    </div>
  )
}
