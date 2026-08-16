namespace Phoria;

public sealed class PhoriaObservabilityOptions
{
	public const string SectionName = "Phoria:Observability";
	public bool Logging { get; set; }
	public bool Metrics { get; set; }
	public bool LogHealthChecks { get; set; }
	public PhoriaObservabilityTracingOptions Tracing { get; set; } = new();
}

public sealed class PhoriaObservabilityTracingOptions
{
	public bool Enabled { get; set; }
	public double SamplingRatio { get; set; } = 0.1;
}
