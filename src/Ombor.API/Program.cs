using Ombor.API.Extensions;
using Ombor.Application.Extensions;
using Ombor.Infrastructure.Extensions;
using Ombor.TestDataGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddTestDataGenerator(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(o => { });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.UseDatabaseSeederAsync();

await app.RunAsync();

#pragma warning disable S1118 // For API tests
public partial class Program;
#pragma warning restore S1118