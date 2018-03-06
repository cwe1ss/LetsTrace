using OpenTracing;
using OpenTracing.Propagation;

namespace LetsTrace.Propagation
{
    public interface IPropagationRegistry
    {
        void AddCodec(string format, IInjector injector, IExtractor extractor);
        void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier);
        ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier);
    }
}