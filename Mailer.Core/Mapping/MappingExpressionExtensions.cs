using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using System;


namespace Mailer.Mapping
{
    public static class MappingExpressionExtensions
    {
        public static IEnumMappingExpression<TSource, TDestination> ConvertUsingEnumMapping<TSource, TDestination>(this IMappingExpression<TSource, TDestination> mappingExpression, Action<IEnumConfigurationExpression<TSource, TDestination>> options)
            where TSource : struct, Enum
            where TDestination : struct, Enum
            => EnumMappingExpressionExtensions.ConvertUsingEnumMapping(mappingExpression, options);
    }
}
