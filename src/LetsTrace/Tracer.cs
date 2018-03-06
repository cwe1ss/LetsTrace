using System;
using System.Collections.Generic;
using System.Linq;
using LetsTrace.Propagation;
using LetsTrace.Reporters;
using LetsTrace.Samplers;
using LetsTrace.Util;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Util;

namespace LetsTrace
{
    // Tracer is the main object that consumers use to start spans
    public class Tracer : ILetsTraceTracer
    {
        private IPropagationRegistry _propagationRegistry;
        private IReporter _reporter;
        private ISampler _sampler;

        public IScopeManager ScopeManager { get; private set; }
        public IClock Clock { get; internal set; }
        public ISpan ActiveSpan { get { return ScopeManager.Active?.Span; } }
        public string HostIPv4 { get; }
        public string ServiceName { get; }

        // TODO: support tracer level tags
        // TODO: support metrics
        // TODO: add logger
        public Tracer(
            string serviceName,
            IReporter reporter,
            string hostIPv4,
            ISampler sampler,
            IScopeManager scopeManager = null,
            IPropagationRegistry propagationRegistry = null
        )
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
            HostIPv4 = hostIPv4 ?? throw new ArgumentNullException(nameof(hostIPv4));
            _sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            ScopeManager = scopeManager ?? new AsyncLocalScopeManager();
            _propagationRegistry = propagationRegistry ?? new PropagationRegistry();

            // set up default options - TODO: allow these to be overridden via options
            

            Clock = new Clock();
        }

        public ISpanBuilder BuildSpan(string operationName)
        {
            return new SpanBuilder(this, operationName, _sampler);
        }

        public void ReportSpan(ILetsTraceSpan span)
        {
            var context = span.Context as ILetsTraceSpanContext;
            if (context.IsSampled()) {
                _reporter.Report(span);
            }
        }

        public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier) => _propagationRegistry.Extract(format, carrier);

        public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier) => _propagationRegistry.Inject(spanContext, format, carrier);

        // TODO: setup baggage restriction
        public ILetsTraceSpan SetBaggageItem(ILetsTraceSpan span, string key, string value)
        {
            var context = (SpanContext)span.Context;
            var baggage = context.GetBaggageItems().ToDictionary(b => b.Key, b => b.Value);
            baggage[key] = value;
            context.SetBaggageItems(baggage);
            return span;
        }
    }
}