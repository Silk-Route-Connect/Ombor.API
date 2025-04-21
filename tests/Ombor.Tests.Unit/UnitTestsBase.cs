using AutoFixture;
using AutoFixture.AutoMoq;

namespace Ombor.Tests.Unit;

public abstract class UnitTestsBase
{
    protected readonly Fixture _fixture;

    protected UnitTestsBase()
    {
        _fixture = CreateFixture();
    }

    private static Fixture CreateFixture()
    {
        var fixture = new Fixture();
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new NullRecursionBehavior());
        fixture.Customize(new AutoMoqCustomization());

        fixture.Register(() =>
        {
            var dt = fixture.Create<DateTime>();
            return DateOnly.FromDateTime(dt);
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
