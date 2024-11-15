// Should be able to use just react-dom/server, but right now we can't
// See https://github.com/facebook/react/issues/26906
declare module "react-dom/server.edge" {
	export * from "react-dom/server"
}
