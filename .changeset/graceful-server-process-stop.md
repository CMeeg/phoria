---
phoria-dotnet: patch
---

Graceful stop of the Phoria server process on host shutdown: `StopServer` sends SIGTERM and waits a grace period before force-killing the process tree, instead of immediately SIGKILLing the process, and the host's shutdown signal now triggers `StopServer` rather than being passed to CliWrap's `ListenAsync` (whose cancellation would SIGKILL the node process before the graceful path could run).
