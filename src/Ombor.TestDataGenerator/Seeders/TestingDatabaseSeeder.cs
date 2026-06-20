using Bogus;
using Microsoft.AspNetCore.Hosting;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

internal sealed class TestingDatabaseSeeder(
    DataSeedSettings seedSettings,
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer,
    IPasswordHasher passwordHasher) : IDatabaseSeeder
{
    private readonly Random _random = new();
    private readonly Faker _faker = new(seedSettings.Locale);

    public async Task SeedDatabaseAsync(IApplicationDbContext context, IOrganizationAccessor organizationAccessor)
    {
        var organizationIds = await EnsureOrganizationsWithUsersAsync(context);
        var nameMap = await EnsureImagesCopiedAsync();

        foreach (var organizationId in organizationIds)
        {
            organizationAccessor.SetOrganization(organizationId);

            await CreateCategoriesAsync(context);
            await CreateProductsAsync(context);
            await CreateProductImagesAsync(context, nameMap);
            await CreatePartners(context);
            await AppEmployeesAsync(context);
            await CreateInventoriesAsync(context);
            await CreateInventoryItemsAsync(context);
        }
    }

    private async Task<int[]> EnsureOrganizationsWithUsersAsync(IApplicationDbContext context)
    {
        var organizations = context.Organizations
            .OrderBy(t => t.Id)
            .ToList();

        for (var index = organizations.Count + 1; index <= seedSettings.NumberOfOrganizations; index++)
        {
            var organization = new Organization
            {
                Name = $"Demo Organization {index}",
                IsActive = true,
            };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync(); // need organization.Id for the role/user FKs

            var role = new Role
            {
                Name = "Owner",
                Description = "Seeded owner role.",
                OrganizationId = organization.Id,
                Organization = null! // set by EF Core via OrganizationId
            };

            var password = passwordHasher.HashPassword(seedSettings.SeedUserPassword);
            var user = new User
            {
                FirstName = "Demo",
                LastName = $"User {index}",
                PhoneNumber = $"+9989000000{index:00}",
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                IsPhoneNumberConfirmed = true,
                OrganizationId = organization.Id,
                Organization = null! // set by EF Core via OrganizationId
            };
            user.Roles.Add(role);

            context.Roles.Add(role);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            organizations.Add(organization);
        }

        return organizations
            .Take(seedSettings.NumberOfOrganizations)
            .Select(t => t.Id)
            .ToArray();
    }

    private async Task CreateCategoriesAsync(IApplicationDbContext context)
    {
        if (context.Categories.Any())
        {
            return;
        }

        var categories = Enumerable.Range(1, seedSettings.NumberOfCategories)
            .Select(i => new Category
            {
                Name = $"Test Category {i}",
                Description = _faker.Lorem.Sentence(),
            });

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private async Task CreateProductsAsync(IApplicationDbContext context)
    {
        if (context.Products.Any())
        {
            return;
        }

        var categoryIds = context.Categories.Select(i => i.Id);

        var products = Enumerable.Range(1, seedSettings.NumberOfProducts)
            .Select(i => new Product
            {
                Name = $"Test Product {i}",
                SKU = _faker.Random.Guid().ToString(),
                Description = _faker.Commerce.ProductDescription(),
                Barcode = _faker.Commerce.Ean13(),
                SalePrice = _faker.Finance.Amount(),
                SupplyPrice = _faker.Finance.Amount(),
                RetailPrice = _faker.Finance.Amount(),
                QuantityInStock = _faker.Random.Number(100, 1_000),
                LowStockThreshold = _faker.Random.Number(),
                Measurement = _faker.Random.Enum<UnitOfMeasurement>(),
                CategoryId = _faker.PickRandom<int>(categoryIds),
                Category = null!
            });

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private async Task CreateProductImagesAsync(IApplicationDbContext context, Dictionary<string, string> nameMap)
    {
        if (context.ProductImages.Any())
        {
            return;
        }

        var fileNames = nameMap.Keys.ToArray();
        if (fileNames.Length == 0)
        {
            throw new InvalidOperationException("No seed images were loaded.");
        }

        var productIds = context.Products.Select(p => p.Id).ToArray();
        var images = new List<ProductImage>();

        foreach (var productId in productIds)
        {
            int imagesCount = _random.Next(1, seedSettings.NumberOfMaxImagesPerProduct + 1);

            foreach (var fileName in fileNames.Take(imagesCount))
            {
                images.Add(new ProductImage
                {
                    ProductId = productId,
                    FileName = fileName,
                    ImageName = nameMap[fileName],
                    OriginalUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.OriginalsSubfolder}/{fileName}",
                    ThumbnailUrl = $"{fileSettings.PublicUrlPrefix}/{fileSettings.ProductUploadsSection}/{fileSettings.ThumbnailsSubfolder}/{fileName}",
                    Product = null! // EF Core will set this automatically
                });
            }
        }

        context.ProductImages.AddRange(images);
        await context.SaveChangesAsync();
    }

    private async Task CreatePartners(IApplicationDbContext context)
    {
        if (context.Partners.Any())
        {
            return;
        }

        var partners = Enumerable.Range(1, seedSettings.NumberOfPartners)
            .Select(i => new Partner
            {
                Name = $"Test Partner {i}",
                Address = "Test Partner address",
                CompanyName = "Random Company",
                OpeningBalance = 5_000m,
                OpeningDate = new DateOnly(2026, 1, 1),
                Type = PartnerType.Both,
                Email = $"partner{i}@test.com",
                PhoneNumbers = ["+99890-100-00-00"]
            });

        context.Partners.AddRange(partners);
        await context.SaveChangesAsync();
    }

    private async Task AppEmployeesAsync(IApplicationDbContext context)
    {
        if (context.Employees.Any())
        {
            return;
        }

        var employees = Enumerable.Range(1, seedSettings.NumberOfEmployees)
            .Select(i => new Employee
            {
                FullName = $"test employee {i}",
                Position = $"Test employee position {i}",
                Salary = 1000 + i,
                DateOfEmployment = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                Status = EmployeeStatus.Active,
            });

        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();
    }

    private async Task CreateInventoriesAsync(IApplicationDbContext context)
    {
        if (context.Inventories.Any())
        {
            return;
        }

        var inventories = Enumerable.Range(1, seedSettings.NumberOfInventories)
            .Select(i => new Inventory
            {
                Name = $"Test Inventory {i}",
                Location = _faker.Address.StreetAddress(),
                IsActive = _faker.Random.Bool(),
            });

        context.Inventories.AddRange(inventories);
        await context.SaveChangesAsync();
    }

    private async Task CreateInventoryItemsAsync(IApplicationDbContext context)
    {
        if (context.InventoryItems.Any())
        {
            return;
        }

        var products = context.Products.Select(p => p.Id).ToArray();
        var inventories = context.Inventories.Select(i => i.Id).ToArray();
        var inventoryItems = new List<InventoryItem>();

        foreach (var product in products.Take(seedSettings.NumberOfItemsPerInventory))
        {
            foreach (var inventory in inventories)
            {
                inventoryItems.Add(new InventoryItem
                {
                    Quantity = 10,
                    ProductId = product,
                    InventoryId = inventory,
                    Product = null!, // EF Core will set these automatically
                    Inventory = null! // EF Core will set these automatically
                });
            }
        }

        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    {
        var originalsDir = Path.Combine(env.WebRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.OriginalsSubfolder);
        var thumbsDir = Path.Combine(env.WebRootPath, fileSettings.BasePath, fileSettings.ProductUploadsSection, fileSettings.ThumbnailsSubfolder);

        if (Directory.Exists(originalsDir))
        {
            Directory.Delete(originalsDir, true);
        }

        if (Directory.Exists(thumbsDir))
        {
            Directory.Delete(thumbsDir, true);
        }

        Directory.CreateDirectory(originalsDir);
        Directory.CreateDirectory(thumbsDir);

        //if (Directory.EnumerateFiles(originalsDir).Any())
        //{
        //    return [];
        //}

        return await ExtractAndSaveSeedImagesAsync(originalsDir, thumbsDir);
    }

    private async Task<Dictionary<string, string>> ExtractAndSaveSeedImagesAsync(string originalsDir, string thumbsDir)
    {
        const string imagesNamespace = "Ombor.TestDataGenerator.Resources.Images.";

        var currentAssembly = typeof(ProductGenerator).Assembly;
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resourceNames = currentAssembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(imagesNamespace, StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var originalFileName = resourceName[imagesNamespace.Length..];
            var extension = Path.GetExtension(originalFileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";

            nameMap[storageFileName] = originalFileName;

            // copy original
            await using var originalImageStream = currentAssembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(resourceName);
            await using var originalImageFileStream = File.Create(Path.Combine(originalsDir, storageFileName));
            await originalImageStream.CopyToAsync(originalImageFileStream);

            // generate & save thumbnail
            originalImageStream.Position = 0;
            var format = ImageHelper.GetThumbnailFormat(extension);
            await using var thumbnailStream = await thumbnailer.GenerateThumbnailAsync(originalImageStream, format);
            await using var thumbnailImageFileStream = File.Create(Path.Combine(thumbsDir, storageFileName));
            await thumbnailStream.CopyToAsync(thumbnailImageFileStream);
        }

        return nameMap;
    }
}
