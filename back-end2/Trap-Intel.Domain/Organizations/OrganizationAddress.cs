using System;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Entity representing the association between an Organization and an Address.
    /// </summary>
    public class OrganizationAddress : Abstractions.Entity<Guid>
    {
        private OrganizationAddress() { }

        public OrganizationAddress(Guid organizationId, Address address, AddressType addressType)
        {
            OrganizationId = organizationId;
            Address = address;
            AddressType = addressType;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid OrganizationId { get; private set; }
        public Address Address { get; private set; } = null!;
        public AddressType AddressType { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public void UpdateAddress(Address address)
        {
            Address = address;
        }

        public void ChangeAddressType(AddressType addressType)
        {
            AddressType = addressType;
        }
    }
}
