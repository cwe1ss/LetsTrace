using System;
using System.Collections.Generic;
using LetsTrace.Propagation;
using OpenTracing;
using OpenTracing.Propagation;

namespace LetsTrace.Propagation
{
    public class PropagationRegistry : IPropagationRegistry
    {
        internal Dictionary<string, IInjector> _injectors { get; private set; } = new Dictionary<string, IInjector>();
        internal Dictionary<string, IExtractor> _extractors { get; private set; } = new Dictionary<string, IExtractor>();

        public PropagationRegistry()
        {
            var defaultHeadersConfig = new HeadersConfig(Constants.TraceContextHeaderName, Constants.TraceBaggageHeaderPrefix);

            var textPropagator = TextMapPropagator.NewTextMapPropagator(defaultHeadersConfig);
            AddCodec(BuiltinFormats.TextMap.ToString(), textPropagator, textPropagator);

            var httpHeaderPropagator = TextMapPropagator.NewHTTPHeaderPropagator(defaultHeadersConfig);
            AddCodec(BuiltinFormats.HttpHeaders.ToString(), httpHeaderPropagator, httpHeaderPropagator);
        }

        public void AddCodec(string format, IInjector injector, IExtractor extractor)
        {
            _injectors[format] = injector;
            _extractors[format] = extractor;
        }

        public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
        {
            if (_extractors.ContainsKey(format.ToString())) {
                return _extractors[format.ToString()].Extract(carrier);
            }
            throw new Exception($"{format.ToString()} is not a supported extraction format");
        }

        public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
        {
            if (_injectors.ContainsKey(format.ToString())) {
                _injectors[format.ToString()].Inject(spanContext, carrier);
                return;
            }
            throw new Exception($"{format.ToString()} is not a supported injection format");
        }
    }
}
