using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public class SupplierTestsBase : ServiceTestsBase
{
    protected readonly int SupplierId = 1_000;
    protected readonly Partner[] _defaultSuppliers;
    private protected readonly SupplierService _service;

    protected SupplierTestsBase()
    {
        _defaultSuppliers = GenerateRandomSuppliers();
        SetupSuppliers(_defaultSuppliers);

        _service = new SupplierService(_mockContext.Object, _mockValidator.Object);
    }

    protected Partner[] GenerateRandomSuppliers(int count = 5)
    => Enumerable.Range(1, count)
    .Select(i => CreateSupplier(i))
    .ToArray();

    protected Partner CreateSupplier(int? id = null)
    => _builder.SupplierBuilder
    .WithId(id ?? SupplierId)
    .BuildAndPopulate();
}
