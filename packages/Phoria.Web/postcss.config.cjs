const postcssPresetEnv = require("postcss-preset-env")

const config = () => ({
	plugins: [
		postcssPresetEnv({
			stage: 0
		})
	]
})

module.exports = config
