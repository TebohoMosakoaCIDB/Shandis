using Microsoft.Extensions.DependencyInjection;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Infrastructure.Persistence.Repositories;

namespace Shandis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddScoped<IMemoryRepository, MemoryRepository>();

            return services;
        }
    }
}
