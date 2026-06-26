namespace Ombor.Application.Interfaces;

/// <summary>
/// Seeds the ordinary starter records a brand-new organization needs to transact immediately
/// (rule 42): one Cash wallet, one warehouse, one category, one partner. They carry no system
/// flag and are editable / archivable / deletable like any other row.
/// </summary>
public interface IOrganizationSetupService
{
    /// <param name="language">
    /// The user's registration language (a supported code, e.g. <c>ru</c>); determines the starter names.
    /// </param>
    Task SeedStarterDataAsync(int organizationId, string language);
}
