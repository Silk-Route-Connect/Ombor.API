using AutoFixture;
using AutoFixture.AutoMoq;
using Bogus;

namespace Ombor.Tests.Unit;

public abstract class UnitTestsBase
{
    protected readonly Fixture _fixture;
    protected readonly Faker _faker;

    protected UnitTestsBase()
    {
        _fixture = CreateFixture();
        _faker = new();
    }

    private static Fixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new NullRecursionBehavior());
        fixture.Customize(new AutoMoqCustomization());

        fixture.Register(() =>
        {
            var date = fixture.Create<DateTime>();

            return DateOnly.FromDateTime(date);
        });

        fixture.Register(() =>
        {
            return fixture.Create<bool>()
                ? (DateOnly?)null
                : DateOnly.FromDateTime(fixture.Create<DateTime>());
        });

        return fixture;
    }
}
