import type { Meta, StoryObj } from "@storybook/react"
import { Counter } from "./counter"

const meta = {
  title: "Phoria/Counter",
  component: Counter,
  tags: ["autodocs"],
  argTypes: {
    startAt: { control: "number" },
  },
  decorators: [
    (Story) => (
      <div className="p-8">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Counter>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    startAt: 5,
  },
}

export const WithStartCount: Story = {
  args: {
    startAt: 10,
  },
}
