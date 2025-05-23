using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a supplier entity.
/// </summary>
public class Supplier : EntityBase
{
    /// <summary>Gets or sets the name of the supplier.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the address of the supplier.</summary>
    public string Address { get; set; }

    /// <summary>Gets or sets the email address of the supplier.</summary>
    public string Email { get; set; }

    /// <summary>Gets or sets the name of the company associated with the supplier.</summary>
    public string CompanyName { get; set; }

    /// <summary>Gets or sets the supplier's status.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the supplier's phone numbers.</summary>
    public List<string> PhoneNumbers { get; set; }
}
