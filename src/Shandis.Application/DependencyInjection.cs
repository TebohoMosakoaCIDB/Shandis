using Microsoft.Extensions.DependencyInjection;
using Shandis.Application.Features.Memories;

namespace Shandis.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IMemoryService, MemoryService>();

            return services;
        }
    }
}
