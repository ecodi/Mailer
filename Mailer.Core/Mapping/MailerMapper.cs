using System.Collections.Generic;
using System.Linq;

namespace Mailer.Mapping
{
    public class MailerMapper : IMapper
    {
        private readonly AutoMapper.IMapper _engine;

        public MailerMapper(AutoMapper.IMapper engine)
        {
            _engine = engine;
        }

        public TDestination Map<TDestination>(object source)
            => _engine.Map<TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source)
            => _engine.Map<TSource, TDestination>(source);

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
            => _engine.Map(source, destination);

        public IAsyncEnumerable<TDestination> MapAsyncEnumerable<TSource, TDestination>(IAsyncEnumerable<TSource> sourceEnum)
            => sourceEnum.Select(_engine.Map<TSource, TDestination>);
    }
}
