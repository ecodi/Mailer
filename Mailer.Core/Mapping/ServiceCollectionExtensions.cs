using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Mailer.Mapping
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMappings(this IServiceCollection services, Action<IServiceProvider, IMapperConfigurationExpression> configAction)
            => services
                .AddAutoMapper(configAction, Enumerable.Empty<Assembly>())
                .AddTransient<IMapper, MailerMapper>();
    }
}
