using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Infrastructure.Persistence.Converters;

/// <summary>
/// Value converters for domain value objects to support EF Core mapping.
/// These converters handle the serialization/deserialization of value objects.
/// </summary>

#region Identity Value Object Converters

public class UserEmailConverter : ValueConverter<UserEmail, string>
{
    public UserEmailConverter() : base(
        v => v.Value,
        v => UserEmail.Create(v).Value)
    { }
}

public class UserNameConverter : ValueConverter<UserName, string>
{
    public UserNameConverter() : base(
        v => v.Value,
        v => UserName.Create(v).Value)
    { }
}

public class FirstNameConverter : ValueConverter<FirstName, string>
{
    public FirstNameConverter() : base(
        v => v.Value,
        v => FirstName.Create(v).Value)
    { }
}

public class LastNameConverter : ValueConverter<LastName, string>
{
    public LastNameConverter() : base(
        v => v.Value,
        v => LastName.Create(v).Value)
    { }
}

#endregion

#region Shared Value Object Converters

public class TaxIdentifierConverter : ValueConverter<TaxIdentifier, string>
{
    public TaxIdentifierConverter() : base(
        v => v.TaxId,
        v => TaxIdentifier.Create(v).Value)
    { }
}

public class OrganizationDomainConverter : ValueConverter<OrganizationDomain, string>
{
    public OrganizationDomainConverter() : base(
        v => v.Domain,
        v => OrganizationDomain.Create(v).Value)
    { }
}

#endregion
