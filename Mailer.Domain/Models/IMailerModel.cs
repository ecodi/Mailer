using System;

namespace Mailer.Domain.Models
{
    public interface IMailerModel
    {
        Guid Id { get; set; }
        int RowVersion { get; set; }

        void Validate();
    }
}
