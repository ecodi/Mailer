using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Reflection;

namespace Mailer.Api
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GrpcService : Attribute { }

    public static class EndpointRouteBuilderExtensions
    {
        public static void MapGrpcServices(this IEndpointRouteBuilder endpoints)
        {
            var attrType = typeof(GrpcService);
            foreach(var serviceType in attrType.Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(attrType, true)))
            {
                typeof(GrpcEndpointRouteBuilderExtensions).GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService), BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(serviceType).Invoke(endpoints, new object[] { endpoints });
            }
        }
    }
}
