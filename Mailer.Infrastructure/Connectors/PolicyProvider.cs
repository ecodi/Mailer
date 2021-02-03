using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mailer.Infrastructure.Connectors
{
    public class PolicyProvider : IPolicyProvider
    {
        private readonly ILogger<PolicyProvider> _logger;
        private readonly ConcurrentDictionary<string, IAsyncPolicy> _policies = new ConcurrentDictionary<string, IAsyncPolicy>();

        public PolicyProvider(ILogger<PolicyProvider> logger)
        {
            _logger = logger;
        }

        public IAsyncPolicy<TResult> Get<TResult>(string policyName, PolicyConfig defaultConfig, params Type[] handledExceptionsTypes)
        {
            var policy = _policies.GetOrAdd(policyName, pn =>
            {
                var policies = new List<IAsyncPolicy>();
                if (defaultConfig.Wr != null)
                    policies.Add(Policy.Handle<Exception>(ex =>
                    {
                        if (!handledExceptionsTypes.Any())
                            return !(ex is BrokenCircuitException);
                        var t = ex.GetType();
                        return handledExceptionsTypes.Any(eType => eType == t);
                    }).WaitAndRetryAsync(defaultConfig.Wr.RetryCount,
                        retryAttempt => TimeSpan.FromTicks(defaultConfig.Wr.SleepDurationBase.Ticks * retryAttempt),
                        onRetry: (ex, ts) => _logger.LogWarning("Retrying call for policy {policyName}", policyName)));
                if (defaultConfig.Cb != null)
                    policies.Add(Policy.Handle<Exception>(ex =>
                    {
                        var t = ex.GetType();
                        return !handledExceptionsTypes.Any() || handledExceptionsTypes.Any(eType => eType == t);
                    }).CircuitBreakerAsync(defaultConfig.Cb.ExceptionsAllowedBeforeBreaking, defaultConfig.Cb.DurationOfBreak,
                        onBreak: (ex, ts) => _logger.LogWarning("Broken circuit for policy {policyName}", policyName),
                        onHalfOpen: () => _logger.LogWarning("Circuit half open for policy {policyName}", policyName),
                        onReset: () => _logger.LogWarning("Circuit reset for policy {policyName}", policyName)));
                return policies.Count > 1 ? Policy.WrapAsync(policies.ToArray()) : policies.First();
            });
            return policy.AsAsyncPolicy<TResult>();
        }
    }
}
