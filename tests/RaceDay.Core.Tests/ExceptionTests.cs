namespace RaceDay.Core.Tests;

using RaceDay.Core.Exceptions;

public class ExceptionTests
{
    // ── RaceDayException ─────────────────────────────────────────────────────

    [Fact]
    public void RaceDayException_DefaultConstructor_CreatesInstance()
    {
        var ex = new RaceDayException();

        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<RaceDayException>();
    }

    [Fact]
    public void RaceDayException_MessageConstructor_SetsMessage()
    {
        var ex = new RaceDayException("test message");

        ex.Message.ShouldBe("test message");
    }

    [Fact]
    public void RaceDayException_InnerExceptionConstructor_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new RaceDayException("outer", inner);

        ex.Message.ShouldBe("outer");
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void RaceDayException_IsException()
    {
        var ex = new RaceDayException("msg");

        (ex is Exception).ShouldBeTrue();
    }

    // ── ValidationException ──────────────────────────────────────────────────

    [Fact]
    public void ValidationException_Constructor_SetsPropertyName()
    {
        var ex = new ValidationException("WeightKg", "must be positive");

        ex.PropertyName.ShouldBe("WeightKg");
    }

    [Fact]
    public void ValidationException_Constructor_SetsMessageContainingPropertyAndReason()
    {
        var ex = new ValidationException("WeightKg", "must be positive");

        ex.Message.ShouldContain("WeightKg");
        ex.Message.ShouldContain("must be positive");
    }

    [Fact]
    public void ValidationException_IsRaceDayException()
    {
        var ex = new ValidationException("Prop", "reason");

        (ex is RaceDayException).ShouldBeTrue();
    }

    // ── MissingProductException ──────────────────────────────────────────────

    [Fact]
    public void MissingProductException_Constructor_SetsProductType()
    {
        var ex = new MissingProductException("gel");

        ex.ProductType.ShouldBe("gel");
    }

    [Fact]
    public void MissingProductException_Constructor_SetsMessageContainingProductType()
    {
        var ex = new MissingProductException("drink");

        ex.Message.ShouldContain("drink");
    }

    [Fact]
    public void MissingProductException_IsRaceDayException()
    {
        var ex = new MissingProductException("gel");

        (ex is RaceDayException).ShouldBeTrue();
    }
}
