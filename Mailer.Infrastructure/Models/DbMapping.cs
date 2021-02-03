using Mailer.Mapping;
using Mailer.Domain.Models;
using System.Linq;

namespace Mailer.Infrastructure.Models
{
    public class DbMapping : BaseMapping
    {
        public DbMapping()
        {
            CreateMap<Recipient, RecipientDbModel>().ReverseMap();
            CreateMap<Sender, SenderDbModel>().ReverseMap();

            CreateMap<MailingStatus, MailingStatusDbModel>().ReverseMap();
            CreateMap<Mailing, MailingDbModel>()
                .ForMember(m => m.RecipientsIds, e => e.MapFrom(m => m.Recipients.Select(r => r.Id)))
                .ForMember(m => m.SenderId, e => e.MapFrom(m => m.Sender.Id))
                .ReverseMap()
                    .ForMember(m => m.Recipients, e => e.MapFrom(m => m.RecipientsIds.Select(id => new Recipient { Id = id })))
                    .ForMember(m => m.Sender, e => e.MapFrom(m => new Sender { Id = m.SenderId }));

            CreateMap<Email.Entity, EmailDbModel.Entity>().ReverseMap();
            CreateMap<Email, EmailDbModel>().ReverseMap();
        }
    }
}
