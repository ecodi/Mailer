using Mailer.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mailer.API.Protos.Recipients;
using Mailer.Domain.Services;
using System.Threading.Tasks;

namespace Mailer.API.Protos.Recipients
{
    public static partial class Recipients
    {
        public abstract partial class RecipientsBase : Api.GrpcServices.BaseGrpcService<Domain.Models.Recipient, Recipient, AddRecipientRequest.Types.Data, UpdateRecipientRequest.Types.Data>
        {
            protected RecipientsBase(IService<Domain.Models.Recipient> service, IMapper mapper)
                : base(service, mapper)
            {
            }
        }
    }
}

namespace Mailer.Api.GrpcServices
{
    public class RecipientsImpl : Recipients.RecipientsBase
    {
        public RecipientsImpl(IRecipientService service, IMapper mapper) : base(service, mapper)
        {
        }

        public override Task<Recipient?> GetRecipient(GetRecipientRequest request, ServerCallContext context)
            => GetImpl(request.Id, context);

        public override Task GetRecipients(GetRecipientsRequest request, IServerStreamWriter<Recipient> responseStream, ServerCallContext context)
            => GetManyImpl(responseStream, context);

        public override Task<Recipient> AddRecipient(AddRecipientRequest request, ServerCallContext context)
            => AddImpl(request.Data, context);

        public override Task<Recipient> UpdateRecipient(UpdateRecipientRequest request, ServerCallContext context)
            => UpdateImpl(request.Id, request.RowVersion, request.Data, context);

        public override Task<Empty> DeleteRecipient(DeleteRecipientRequest request, ServerCallContext context)
            => DeleteImpl(request.Id, request.RowVersion, context);
    }
}
