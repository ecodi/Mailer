using System.Collections.Generic;

namespace Mailer.Mapping
{
    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        IAsyncEnumerable<TDestination> MapAsyncEnumerable<TSource, TDestination>(IAsyncEnumerable<TSource> sourceEnum);
    }
}
