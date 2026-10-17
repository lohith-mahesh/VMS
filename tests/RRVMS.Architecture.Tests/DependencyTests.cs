using RRVMS.Application.Services;
using RRVMS.Domain.Entities;
using RRVMS.Infrastructure.Persistence;

namespace RRVMS.Architecture.Tests;

public sealed class DependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        var references = typeof(VisitorRequest).Assembly.GetReferencedAssemblies().Select(name => name.Name).ToArray();

        Assert.DoesNotContain("RRVMS.Application", references);
        Assert.DoesNotContain("RRVMS.Infrastructure", references);
        Assert.DoesNotContain("RRVMS.Api", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = typeof(VisitorRequestService).Assembly.GetReferencedAssemblies().Select(name => name.Name).ToArray();

        Assert.DoesNotContain("RRVMS.Infrastructure", references);
        Assert.DoesNotContain("RRVMS.Api", references);
    }

    [Fact]
    public void InfrastructureReferencesApplicationAndDomain()
    {
        var references = typeof(RrvmsDbContext).Assembly.GetReferencedAssemblies().Select(name => name.Name).ToArray();

        Assert.Contains("RRVMS.Application", references);
        Assert.Contains("RRVMS.Domain", references);
    }
}
