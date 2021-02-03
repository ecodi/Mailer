using Mailer.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mailer.API.Protos.Mailings;
using Mailer.Domain.Services;
using System.Threading.Tasks;

namespace Mailer.API.Protos.Mailings
{
    public static partial class Mailings
    {
        public abstract partial class MailingsBase : Api.GrpcServices.BaseGrpcService<Domain.Models.Mailing, Mailing, AddMailingRequest.Types.Data, UpdateMailingRequest.Types.Data>
        {
            protected MailingsBase(IService<Domain.Models.Mailing> service, IMapper mapper)
                : base(service, mapper)
            {
            }
        }
    }
}

namespace Mailer.Api.GrpcServices
{
    public class MailingsImpl : Mailings.MailingsBase
    {
        private readonly IMailingService _mailingService;

        public MailingsImpl(IMailingService service, IMapper mapper) : base(service, mapper)
        {
            _mailingService = service;
        }

        public override async Task SendMailing(SendMailingRequest request, IServerStreamWriter<Mailing> responseStream, ServerCallContext context)
        {
            var mailerContext = context.GetMailerContext();
            var model = await _mailingService.GetAndEnsureVersionAsync(mailerContext, request.Id.ToGuid(), request.RowVersion);
            await _mailingService.ExecuteAsync(mailerContext, model);
            await responseStream.WriteAsync(Mapper.Map<Domain.Models.Mailing, Mailing>(model));
            var prevStatus = model.Status;
            while (model != null && model.Status?.StatusCode != Domain.Models.MailingStatusCode.Done)
            {
                await Task.Delay(300);
                model = await _mailingService.GetAsync(mailerContext, model.Id);
                if (model is null || model.Status.StatusCode == prevStatus.StatusCode
                    && model.Status.Message == prevStatus.Message) continue;
                await responseStream.WriteAsync(Mapper.Map<Domain.Models.Mailing, Mailing>(model));
                prevStatus = model.Status;
            }
        }

        public override Task<Mailing?> GetMailing(GetMailingRequest request, ServerCallContext context)
            => GetImpl(request.Id, context);

        public override Task GetMailings(GetMailingsRequest request, IServerStreamWriter<Mailing> responseStream, ServerCallContext context)
            => GetManyImpl(responseStream, context);

        public override Task<Mailing> AddMailing(AddMailingRequest request, ServerCallContext context)
            => AddImpl(request.Data, context);

        public override Task<Mailing> UpdateMailing(UpdateMailingRequest request, ServerCallContext context)
            => UpdateImpl(request.Id, request.RowVersion, request.Data, context);

        public override Task<Empty> DeleteMailing(DeleteMailingRequest request, ServerCallContext context)
            => DeleteImpl(request.Id, request.RowVersion, context);
    }
}
