using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// A per-organization counter for one document series. Numbers are the human handle of the
/// dispute trail, so they are allocated atomically (never a stored balance that can drift).
/// </summary>
public class NumberSequence : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    public NumberSeriesType SeriesType { get; set; }

    /// <summary>The most recently allocated number for this (organization, series). Next allocation = <see cref="LastValue"/> + 1.</summary>
    public int LastValue { get; set; }
}
