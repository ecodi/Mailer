using Mailer.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mailer.Domain.Models;
using Mailer.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Mailer.Api.GrpcServices
{
    [GrpcService]
    [Authorize]
    public abstract class BaseGrpcService<TModel, TGrpcModel, TAddData, TUpdateData>
        where TModel : class, IMailerModel
        where TGrpcModel : class
    {
        protected readonly IService<TModel> Service;
        protected readonly IMapper Mapper;

        protected BaseGrpcService(IService<TModel> service, IMapper mapper)
        {
            Service = service;
            Mapper = mapper;
        }

        protected async Task<TGrpcModel?> GetImpl(string id, ServerCallContext context)
        {
            return Mapper.Map<TModel?, TGrpcModel?>(await Service.GetAsync(context.GetMailerContext(), id.ToGuid()));
        }

        protected async Task GetManyImpl(IServerStreamWriter<TGrpcModel> responseStream, ServerCallContext context)
        {
            await foreach (var result in Service.GetListAsync(context.GetMailerContext()))
                await responseStream.WriteAsync(Mapper.Map<TModel, TGrpcModel>(result));
        }

        protected async Task<TGrpcModel> AddImpl(TAddData data, ServerCallContext context)
        {
            var model = Mapper.Map<TAddData, TModel>(data);
            await Service.SaveAsync(context.GetMailerContext(), model);
            return Mapper.Map<TModel, TGrpcModel>(model!);
        }

        protected async Task<TGrpcModel> UpdateImpl(string id, int rowVersion, TUpdateData data, ServerCallContext context)
        {
            var model = await Service.GetAndEnsureVersionAsync(context.GetMailerContext(), id.ToGuid(), rowVersion);
            Mapper.Map(data, model);
            await Service.SaveAsync(context.GetMailerContext(), model);
            return Mapper.Map<TModel, TGrpcModel>(model!);
        }

        protected async Task<Empty> DeleteImpl(string id, int rowVersion, ServerCallContext context)
        {
            var model = await Service.GetAndEnsureVersionAsync(context.GetMailerContext(), id.ToGuid(), rowVersion);
            await Service.DeleteAsync(context.GetMailerContext(), model);
            return new Empty();
        }
    }
}
