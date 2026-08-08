namespace Phoria.Logging;

public static class EventId
{
	public static class Islands
	{
		public const int EntryAttributeMissing = 1101;
		public const int ViteManifestKeyNotFound = 1102;
		public const int ManifestEntryDoesntHaveCssChunks = 1103;
		public const int ServerUnhealthyDegradingToClient = 1104;
	}

	public static class Server
	{
		public const int MiddlewareProxyViaHttpError = 1201;
		public const int ServerIsHealthy = 1202;
		public const int ServerIsUnhealthy = 1203;
		public const int EstablishingWebSocketProxy = 1204;
		public const int FailedToEstablishWebSocketProxy = 1205;
		public const int FailedToCloseWebSocket = 1206;
		public const int ProcessNotConfigured = 1207;
		public const int ProcessIsHealthy = 1208;
		public const int ProcessIsRunning = 1209;
		public const int ProcessStdOut = 1210;
		public const int ProcessStdErr = 1211;
		public const int ProcessExited = 1212;
		public const int ProcessException = 1213;
		public const int ProcessStarting = 1214;
		public const int ProcessTerminationSignalSent = 1215;
		public const int ProcessForceStopped = 1216;
		public const int ServerIsNotReadyYet = 1217;
	}

	public static class Vite
	{
		public const int ManifestFileWontBeRead = 1301;
		public const int DetectedChangeInManifest = 1302;
		public const int ManifestFileNotFound = 1303;
		public const int SsrManifestFileWontBeRead = 1304;
		public const int DetectedChangeInSsrManifest = 1305;
		public const int SsrManifestFileNotFound = 1306;
	}
}
