using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Domain.Entities;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;

namespace Ombor.TestDataGenerator.Seeders;

internal abstract class SeederBase(
    DataSeedSettings seedSettings,
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer,
    IPasswordHasher passwordHasher)
{
    private const string imagesNamespace = "Ombor.TestDataGenerator.Resources.Images.";
    private readonly Assembly currentAssembly = typeof(ProductGenerator).Assembly;

    protected static readonly Random _random = new();
    protected readonly FileSettings fileSettings = fileSettings;

    /// <summary>
    /// Ensures <see cref="DataSeedSettings.NumberOfTenants"/> tenants exist, each with a
    /// loginable owner <see cref="User"/> and <see cref="Role"/>. Tenants, users and roles
    /// are not <c>ITenantScoped</c>, so they are created with no tenant pinned (the global
    /// query filter and insert stamping are bypassed). Returns the tenant ids to seed.
    /// </summary>
    protected async Task<int[]> EnsureTenantsWithUsersAsync(IApplicationDbContext context)
    {
        var tenants = context.Tenants
            .OrderBy(t => t.Id)
            .ToList();

        for (var index = tenants.Count + 1; index <= seedSettings.NumberOfTenants; index++)
        {
            var tenant = new Tenant
            {
                Name = $"Demo Tenant {index}",
                IsActive = true,
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(); // need tenant.Id for the role/user FKs

            var role = new Role
            {
                Name = "Owner",
                Description = "Seeded owner role.",
                TenantId = tenant.Id,
                Tenant = null! // set by EF Core via TenantId
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
                TenantId = tenant.Id,
                Tenant = null! // set by EF Core via TenantId
            };
            user.Roles.Add(role);

            context.Roles.Add(role);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            tenants.Add(tenant);
        }

        return tenants
            .Take(seedSettings.NumberOfTenants)
            .Select(t => t.Id)
            .ToArray();
    }

    protected Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    {
        var originalsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.OriginalsSubfolder);
        var thumbsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.ThumbnailsSubfolder);

        if (Directory.Exists(originalsDirectory))
        {
            Directory.Delete(originalsDirectory, true);
        }

        if (Directory.Exists(thumbsDirectory))
        {
            Directory.Delete(thumbsDirectory, true);
        }

        Directory.CreateDirectory(originalsDirectory);
        Directory.CreateDirectory(thumbsDirectory);

        return ExtractAndSaveImagesAsync(originalsDirectory, thumbsDirectory);
    }

    private async Task<Dictionary<string, string>> ExtractAndSaveImagesAsync(string originalsDir, string thumbsDir)
    {
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
            await using var thumbnailStream = await thumbnailer.GenerateThumbnailAsync(originalImageFileStream, format);
            await using var thumbnailImageFileStream = File.Create(Path.Combine(thumbsDir, storageFileName));
            await thumbnailStream.CopyToAsync(thumbnailImageFileStream);
        }

        return nameMap;
    }
}
