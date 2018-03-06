using System;
using System.Collections.Generic;
using LetsTrace.Propagation;
using LetsTrace.Reporters;
using LetsTrace.Samplers;
using LetsTrace.Util;
using NSubstitute;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Util;
using Xunit;

namespace LetsTrace.Tests
{
    public class TracerTests
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
        public void Tracer_Constructor_ShouldThrowWhenServiceNameIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Tracer(null, null, null, null, null));
            Assert.Equal("serviceName", ex.ParamName);
        }

        [Fact]
        public void Tracer_Constructor_ShouldThrowWhenReporterIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Tracer("testingService", null, null, null, null));
            Assert.Equal("reporter", ex.ParamName);
        }

        [Fact]
        public void Tracer_Constructor_ShouldThrowWhenHostIPv4IsNull()
        {
            var reporter = Substitute.For<IReporter>();

            var ex = Assert.Throws<ArgumentNullException>(() => new Tracer("testingService", reporter, null, null, null));
            Assert.Equal("hostIPv4", ex.ParamName);
        }

        [Fact]
        public void Tracer_Constructor_ShouldThrowWhenSamplerIsNull()
        {
            var reporter = Substitute.For<IReporter>();
            var sampler = Substitute.For<ISampler>();

            var ex = Assert.Throws<ArgumentNullException>(() => new Tracer("testingService", reporter, "192.168.1.1", null, null));
            Assert.Equal("sampler", ex.ParamName);
        }

        [Fact]
        public void Tracer_Constructor_ShouldUseOpenTracingScopeManagerWhenScopeManagerIsNull()
        {
            var reporter = Substitute.For<IReporter>();
            var sampler = Substitute.For<ISampler>();

            var tracer = new Tracer("testingService", reporter, "", sampler, null);
            Assert.True(tracer.ScopeManager is AsyncLocalScopeManager);
        }

        [Fact]
        public void Tracer_BuildSpan_ShouldPassItselfAndOperationNameToSpanBuilder()
        {
            var reporter = Substitute.For<IReporter>();
            var operationName = "testing";
            var sampler = Substitute.For<ISampler>();
            var scopeManager = Substitute.For<IScopeManager>();
            sampler.IsSampled(Arg.Any<TraceId>(), Arg.Any<string>()).Returns((false, new Dictionary<string, Field>()));

            var tracer = new Tracer("testingService", reporter, "192.168.1.1", sampler, scopeManager);
            var span = (ILetsTraceSpan)tracer.BuildSpan(operationName).Start();

            Assert.Equal(operationName, span.OperationName);
            Assert.Equal(tracer, span.Tracer);
        }

        [Fact]
        public void Tracer_BuildSpan_ShouldUsePassedInPropagationRegistry()
        {
            var reporter = Substitute.For<IReporter>();
            var sampler = Substitute.For<ISampler>();
            var scopeManager = Substitute.For<IScopeManager>();
            var pReg = Substitute.For<IPropagationRegistry>();

            IFormat<string> format = new Builtin<string>("format");
            var carrier = "carrier, yo";
            pReg.Extract(Arg.Is<IFormat<string>>(f => f == format), Arg.Is<string>(c => c == carrier));
            var spanContext = Substitute.For<ISpanContext>();
            pReg.Inject(Arg.Is<ISpanContext>(sc => sc == spanContext), Arg.Is<IFormat<string>>(f => f == format), Arg.Is<string>(c => c == carrier));


            var tracer = new Tracer("testingService", reporter, "192.168.1.1", sampler, null, pReg);
            tracer.Extract(format, carrier);
            tracer.Inject(spanContext, format, carrier);

            pReg.Received(1).Extract(Arg.Any<IFormat<string>>(), Arg.Any<string>());
            pReg.Received(1).Inject(Arg.Any<ISpanContext>(), Arg.Any<IFormat<string>>(), Arg.Any<string>());
        }

        [Fact]
        public void Tracer_ReportSpan_ShouldPassSpanToReporter()
        {
            var reporter = Substitute.For<IReporter>();
            var span = Substitute.For<ILetsTraceSpan>();
            var sampler = Substitute.For<ISampler>();
            var scopeManager = Substitute.For<IScopeManager>();
            var context = Substitute.For<ILetsTraceSpanContext>();
            context.IsSampled().Returns(true);
            span.Context.Returns(context);

            var tracer = new Tracer("testingService", reporter, "192.168.1.1", sampler, scopeManager);
            tracer.ReportSpan(span);

            reporter.Received(1).Report(Arg.Any<ILetsTraceSpan>());
        }

        [Fact]
        public void Tracer_ReportSpan_ShouldNotReportWhenNotSampled()
        {
            var reporter = Substitute.For<IReporter>();
            var span = Substitute.For<ILetsTraceSpan>();
            var sampler = Substitute.For<ISampler>();
            var scopeManager = Substitute.For<IScopeManager>();
            var context = Substitute.For<ILetsTraceSpanContext>();
            context.IsSampled().Returns(false);
            span.Context.Returns(context);

            var tracer = new Tracer("testingService", reporter, "192.168.1.1", sampler, scopeManager);
            tracer.ReportSpan(span);

            reporter.Received(0).Report(Arg.Any<ILetsTraceSpan>());
        }

        [Fact]
        public void Tracer_SetBaggageItem_ShouldAddAndSetBaggageItems()
        {
            var reporter = Substitute.For<IReporter>();
            var spanContext = new SpanContext(new TraceId());
            var sampler = Substitute.For<ISampler>();
            var scopeManager = Substitute.For<IScopeManager>();

            var tracer = new Tracer("testingService", reporter, "192.168.1.1", sampler, scopeManager);
            var span = new Span(tracer, "testing", spanContext);

            var key = "key1";
            var value = "value1";
            var value2 = "value2";

            tracer.SetBaggageItem(span, key, value);
            Assert.Equal(value, span.GetBaggageItem(key));

            tracer.SetBaggageItem(span, key, value2);
            Assert.Equal(value2, span.GetBaggageItem(key));
        }
    }
}
