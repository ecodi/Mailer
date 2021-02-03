using Mailer.Infrastructure.Connectors;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.CircuitBreaker;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.Infrastructure.Tests.Connectors
{
    public class PolicyProviderTests
    {
        private class TestException : Exception { }

        private readonly PolicyProvider _policyProvider;
        private readonly Mock<Func<Task<bool>>> _successFuncMock = new Mock<Func<Task<bool>>>();
        private readonly Mock<Func<Task<bool>>> _failureFuncMock = new Mock<Func<Task<bool>>>();

        public PolicyProviderTests()
        {
            _successFuncMock.Setup(f => f()).Returns(() => Task.FromResult(true));
            _failureFuncMock.Setup(f => f()).Returns(() => Task.FromException<bool>(new TestException()));
            _policyProvider = new PolicyProvider(new Mock<ILogger<PolicyProvider>>().Object);
        }

        [Fact]
        public async Task ExecutesSuccessFuncOnce()
        {
            await _policyProvider.Get<bool>("test", new PolicyConfig { Wr = new PolicyConfig.WrConfig(3, TimeSpan.Zero) })
                .ExecuteAsync(_successFuncMock.Object);
            _successFuncMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public async Task RetriesFailuresBasedOnConfig()
        {
            const int retries = 3;
            await Assert.ThrowsAsync<TestException>(() => _policyProvider.Get<bool>("test", new PolicyConfig { Wr = new PolicyConfig.WrConfig(retries, TimeSpan.Zero) })
                .ExecuteAsync(_failureFuncMock.Object));
            _failureFuncMock.Verify(action => action(), Times.Exactly(retries + 1));
        }

        [Fact]
        public async Task DoesNotRetryOnExceptionsNotIncludedInConfig()
        {
            await Assert.ThrowsAsync<TestException>(() => _policyProvider.Get<bool>("test", new PolicyConfig { Wr = new PolicyConfig.WrConfig(3, TimeSpan.Zero) }, typeof(FormatException))
                .ExecuteAsync(_failureFuncMock.Object));
            _failureFuncMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public async Task BreakesCircuitBasedOnConfigWithoutRetryPolicy()
        {
            const int maxErrorsCount = 3;
            var policy = _policyProvider.Get<bool>("test", new PolicyConfig { Cb = new PolicyConfig.CbConfig(maxErrorsCount, TimeSpan.FromMinutes(1)) });
            for (var i = 0; i <= 5; i++)
                await
                    Assert.ThrowsAsync(i >= 3 ? typeof(BrokenCircuitException) : typeof(TestException),
                        () => policy.ExecuteAsync(_failureFuncMock.Object));
            _failureFuncMock.Verify(action => action(), Times.Exactly(maxErrorsCount));
        }

        [Fact]
        public async Task BreakesCircuitBasedOnConfigWithRetryPolicy()
        {
            const int maxErrorsCount = 3;
            const int retries = 1;
            var policy = _policyProvider.Get<bool>("test", new PolicyConfig { Cb = new PolicyConfig.CbConfig( maxErrorsCount, TimeSpan.FromMinutes(1)), Wr = new PolicyConfig.WrConfig(retries, TimeSpan.Zero) });
            for (var i = 0; i <= 2; i++)
                await
                    Assert.ThrowsAsync(i >= maxErrorsCount / (retries + 1) ? typeof(BrokenCircuitException) : typeof(TestException),
                        () => policy.ExecuteAsync(_failureFuncMock.Object));
            _failureFuncMock.Verify(action => action(), Times.Exactly(maxErrorsCount));
        }
    }
}
