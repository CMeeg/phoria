---
phoria-dotnet: patch
---

Graceful stop of the Phoria server process: `StopServer` now sends SIGTERM and waits a grace period before force-killing the process tree, instead of immediately SIGKILLing the process.
