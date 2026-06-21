using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class ArchiveWarehouseTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task ArchiveAsync_ShouldMarkArchived_ButKeepWarehouseListedAndFetchable()
    {
        // Arrange
        var warehouse = await CreateWarehouseAsync();
        var archiveUrl = $"{GetUrl(warehouse.Id)}/archive";

        // Act
        var archived = await _client.PostAsync<WarehouseDto>(archiveUrl, content: new { }, HttpStatusCode.OK);

        // Assert — archive response and a fresh GET both report archived.
        Assert.True(archived.IsArchived);

        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.True(fetched.IsArchived);

        // Still appears in the list (rule 31).
        var list = await _client.GetAsync<WarehouseDto[]>(GetUrl());
        Assert.Contains(list, x => x.Id == warehouse.Id);
    }

    [Fact]
    public async Task RestoreAsync_ShouldClearArchivedFlag()
    {
        // Arrange — start from an archived warehouse.
        var warehouse = await CreateWarehouseAsync(new Warehouse
        {
            Name = $"Warehouse {Guid.NewGuid():N}",
            Location = "Tashkent",
            IsArchived = true,
        });
        var restoreUrl = $"{GetUrl(warehouse)}/restore";

        // Act
        var restored = await _client.PostAsync<WarehouseDto>(restoreUrl, content: new { }, HttpStatusCode.OK);

        // Assert
        Assert.False(restored.IsArchived);

        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse));
        Assert.False(fetched.IsArchived);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldReturnNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var url = $"{NotFoundUrl}/archive";

        // Act
        var response = await _client.PostAsync<ProblemDetails>(url, content: new { }, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Warehouse>(NonExistentEntityId);
    }
}
