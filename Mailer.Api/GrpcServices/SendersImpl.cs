using Mailer.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mailer.API.Protos.Senders;
using Mailer.Domain.Services;
using System.Threading.Tasks;

namespace Mailer.API.Protos.Senders
{
    public static partial class Senders
    {
        public abstract partial class SendersBase : Api.GrpcServices.BaseGrpcService<Domain.Models.Sender, Sender, AddSenderRequest.Types.Data, UpdateSenderRequest.Types.Data>
        {
            protected SendersBase(IService<Domain.Models.Sender> service, IMapper mapper)
                : base(service, mapper)
            {
            }
        }
    }
}

namespace Mailer.Api.GrpcServices
{
    public class SendersImpl : Senders.SendersBase
    {
        public SendersImpl(ISenderService service, IMapper mapper) : base(service, mapper)
        {
        }

        public override Task<Sender?> GetSender(GetSenderRequest request, ServerCallContext context)
            => GetImpl(request.Id, context);

        public override Task GetSenders(GetSendersRequest request, IServerStreamWriter<Sender> responseStream, ServerCallContext context)
            => GetManyImpl(responseStream, context);

        public override Task<Sender> AddSender(AddSenderRequest request, ServerCallContext context)
            => AddImpl(request.Data, context);

        public override Task<Sender> UpdateSender(UpdateSenderRequest request, ServerCallContext context)
            => UpdateImpl(request.Id, request.RowVersion, request.Data, context);

        public override Task<Empty> DeleteSender(DeleteSenderRequest request, ServerCallContext context)
            => DeleteImpl(request.Id, request.RowVersion, context);
    }
}
