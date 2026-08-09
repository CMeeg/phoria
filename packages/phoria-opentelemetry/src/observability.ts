import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http"
import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-http"
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http"
import { HttpInstrumentation } from "@opentelemetry/instrumentation-http"
import { ConsoleLogRecordExporter, SimpleLogRecordProcessor } from "@opentelemetry/sdk-logs"
import { PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics"
import { NodeSDK, type NodeSDKConfiguration } from "@opentelemetry/sdk-node"
import { ParentBasedSampler, TraceIdRatioBasedSampler } from "@opentelemetry/sdk-trace-base"
import type { PhoriaObservabilityAppSettings } from "./appsettings"

let sdk: NodeSDK | undefined
let initialized = false

function createPhoriaObservability(settings: PhoriaObservabilityAppSettings): { shutdown: () => Promise<void> } {
	if (initialized) {
		return {
			shutdown: async () => {
				await sdk?.shutdown()
			}
		}
	}
	initialized = true

	const tracingEnabled = settings.tracing.enabled
	const metricsEnabled = settings.metrics
	const loggingEnabled = settings.logging

	if (!tracingEnabled && !metricsEnabled && !loggingEnabled) {
		return { shutdown: async () => {} }
	}

	const config: Partial<NodeSDKConfiguration> = {}

	if (tracingEnabled) {
		config.traceExporter = new OTLPTraceExporter()
		config.sampler = new ParentBasedSampler({ root: new TraceIdRatioBasedSampler(settings.tracing.samplingRatio) })
	} else {
		// Without an explicit `spanProcessors`, sdk-node falls back to env-derived
		// span processors (defaulting to OTLP), which would create a tracer provider
		// even when tracing is disabled.
		config.spanProcessors = []
	}

	if (metricsEnabled) {
		config.metricReaders = [new PeriodicExportingMetricReader({ exporter: new OTLPMetricExporter() })]
	}

	if (loggingEnabled) {
		const exporter = process.env.OTEL_EXPORTER_OTLP_ENDPOINT ? new OTLPLogExporter() : new ConsoleLogRecordExporter()
		config.logRecordProcessors = [new SimpleLogRecordProcessor({ exporter })]
	}

	if (tracingEnabled || metricsEnabled) {
		config.instrumentations = [
			new HttpInstrumentation({
				ignoreIncomingRequestHook: (request) => request.url === "/hc"
			})
		]
	}

	sdk = new NodeSDK(config)
	sdk.start()

	return {
		shutdown: async () => {
			await sdk?.shutdown()
		}
	}
}

export { createPhoriaObservability }
