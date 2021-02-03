using Microsoft.Extensions.DependencyInjection;

namespace Mailer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScoped<TService1, TService2, TImplementation>(this IServiceCollection services)
            where TImplementation : class, TService1, TService2
            where TService1 : class
            where TService2 : class
        {
            return services
                .AddScoped<TImplementation>()
                .AddScoped<TService1>(s => s.GetRequiredService<TImplementation>())
                .AddScoped<TService2>(s => s.GetRequiredService<TImplementation>());
        }
        public static IServiceCollection AddScoped<TService1, TService2, TService3, TImplementation>(this IServiceCollection services)
            where TImplementation : class, TService1, TService2, TService3
            where TService1 : class
            where TService2 : class
            where TService3 : class
        {
            return services
                .AddScoped<TService1, TService2, TImplementation>()
                .AddScoped<TService3>(s => s.GetRequiredService<TImplementation>());
        }
    }
}
