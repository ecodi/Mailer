using Polly;
using System;

namespace Mailer.Infrastructure.Connectors
{
    public class PolicyConfig
    {
        public class CbConfig
        {
            public int ExceptionsAllowedBeforeBreaking { get; }
            public TimeSpan DurationOfBreak { get; }

            public CbConfig(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak)
            {
                ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
                DurationOfBreak = durationOfBreak;
            }
        }

        public class WrConfig
        {
            public int RetryCount { get; }
            public TimeSpan SleepDurationBase { get; }

            public WrConfig(int retryCount, TimeSpan sleepDurationBase)
            {
                RetryCount = retryCount;
                SleepDurationBase = sleepDurationBase;
            }
        }

        public CbConfig? Cb { get; set; }
        public WrConfig? Wr { get; set; }
    }

    public interface IPolicyProvider
    {
        IAsyncPolicy<TResult> Get<TResult>(string policyName, PolicyConfig defaultConfig, params Type[] handledExceptionsTypes);
    }
}
