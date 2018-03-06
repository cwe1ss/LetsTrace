using System;
using LetsTrace.Propagation;
using NSubstitute;
using OpenTracing;
using OpenTracing.Propagation;
using Xunit;

namespace LetsTrace.Tests
{
    public class PropagationRegistryTests
    {
         private struct Builtin<TCarrier> : IFormat<TCarrier>
        {
            private readonly string _name;

            public Builtin(string name)
            {
                _name = name;
            }

            /// <summary>Short name for built-in formats as they tend to show up in exception messages</summary>
            public override string ToString()
            {
                return $"{GetType().Name}.{_name}";
            }
        }
        
        [Fact]
        public void PropagationRegistry_Constructor_ShouldSetupDefaultInjectorsAndExtractors()
        {
            var tracer = new PropagationRegistry();

            Assert.Contains(tracer._injectors, i => i.Key == BuiltinFormats.TextMap.ToString());
            Assert.Contains(tracer._injectors, i => i.Key == BuiltinFormats.HttpHeaders.ToString());
            Assert.Contains(tracer._extractors, i => i.Key == BuiltinFormats.TextMap.ToString());
            Assert.Contains(tracer._extractors, i => i.Key == BuiltinFormats.HttpHeaders.ToString());
        }

        [Fact]
        public void Tracer_ExtractAndInject_ShouldUseTheCorrectCodec()
        {
            var injector = Substitute.For<IInjector>();
            var extractor = Substitute.For<IExtractor>();
            var carrier = "carrier, yo";
            var spanContext = Substitute.For<ISpanContext>();

            var format = new Builtin<string>("format");

            extractor.Extract(Arg.Is<string>(c => c == carrier));
            injector.Inject(Arg.Is<ISpanContext>(sc => sc == spanContext), Arg.Is<string>(c => c == carrier));

            var pReg = new PropagationRegistry();
            pReg.AddCodec(format.ToString(), injector, extractor);
            pReg.Extract(format, carrier);
            pReg.Inject(spanContext, format, carrier);

            extractor.Received(1).Extract(Arg.Any<string>());
            injector.Received(1).Inject(Arg.Any<ISpanContext>(), Arg.Any<string>());
        }

        [Fact]
        public void Tracer_ExtractAndInject_ShouldThrowWhenCodecDoesNotExist()
        {
            var carrier = "carrier, yo";
            var spanContext = Substitute.For<ISpanContext>();
            var format = new Builtin<string>("format");

            var pReg = new PropagationRegistry();
            var ex = Assert.Throws<Exception>(() => pReg.Extract(format, carrier));
            Assert.Equal($"{format} is not a supported extraction format", ex.Message);

            ex = Assert.Throws<Exception>(() => pReg.Inject(spanContext, format, carrier));
            Assert.Equal($"{format} is not a supported injection format", ex.Message);
        }
    }
}
