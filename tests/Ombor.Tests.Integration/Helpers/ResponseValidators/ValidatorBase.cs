using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public abstract class ValidatorBase(IApplicationDbContext context, FileSettings fileSettings, string webRootPath)
{
    protected readonly IApplicationDbContext context = context;
    protected readonly FileSettings fileSettings = fileSettings;
    protected readonly string webRootPath = webRootPath;
}
