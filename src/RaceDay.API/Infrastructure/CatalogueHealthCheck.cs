namespace RaceDay.API.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Repositories;

/// <summary>
/// Reports unhealthy when the embedded product or activity catalogue cannot be loaded
/// (the same condition the previous <c>/api/products</c> health probe detected).
/// </summary>
internal sealed class CatalogueHealthCheck(IProductRepository products, IActivityRepository activities) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await products.GetAllProductsAsync(cancellationToken);
            await activities.GetAllActivitiesAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (CatalogueLoadException ex)
        {
            return HealthCheckResult.Unhealthy("Catalogue could not be loaded", ex);
        }
    }
}
