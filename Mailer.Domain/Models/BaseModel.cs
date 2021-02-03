using System;
using System.Runtime.CompilerServices;

namespace Mailer.Domain.Models
{
    public abstract class BaseModel
    {
        private Guid _id;
        public Guid Id
        {
            get => _id;
            set
            {
                if (_id.Equals(value)) return;
                if (!_id.IsEmpty())
                    throw new ArgumentException("Id already set");
                _id = value;
            }
        }
        public int RowVersion { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null || obj.GetType() != GetType()) return false;
            var typedObj = (BaseModel) obj;
            if (typedObj.Id.IsEmpty() && Id.IsEmpty()) return ReferenceEquals(this, obj);
            return typedObj.Id.Equals(Id);
        }

        public override int GetHashCode()
            => Id.IsEmpty() ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(Id, GetType());
    }
}
