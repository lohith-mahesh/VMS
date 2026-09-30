using RRVMS.Application.Common;
using RRVMS.Application.Contracts;
using RRVMS.Application.Validation;
using RRVMS.Domain.Enums;

namespace RRVMS.Application.Tests;

public sealed class RequestValidatorTests
{
    [Fact]
    public void InvalidCreateCommandReturnsFieldErrors()
    {
        var command = new CreateVisitorRequestCommand(
            VisitorType.External,
            ContractorType.NormalVisitor,
            string.Empty,
            VisitPurposeType.Technical,
            string.Empty,
            string.Empty,
            "host",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(2));

        var exception = Assert.Throws<ValidationException>(() => RequestValidator.Validate(command));

        Assert.Contains("siteCode", exception.Errors.Keys);
        Assert.Contains("purpose", exception.Errors.Keys);
        Assert.Contains("numberOfVisitors", exception.Errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(9)]
    public void RowVersionMustBeEightBytes(int length)
    {
        Assert.Throws<ValidationException>(() => RequestValidator.ValidateRowVersion(new byte[length]));
    }

    [Fact]
    public void EightByteRowVersionIsAccepted() => RequestValidator.ValidateRowVersion(new byte[8]);
}
