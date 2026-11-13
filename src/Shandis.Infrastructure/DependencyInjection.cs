using Microsoft.Extensions.DependencyInjection;
using Shandis.Application.Abstractions.Email;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Infrastructure.Persistence.Repositories;
using Shandis.Infrastructure.Services;

namespace Shandis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddScoped<IMemoryRepository, MemoryRepository>();
            services.AddScoped<IInvitationRepository, InvitationRepository>();
            services.AddScoped<IEmailService, LoggingEmailService>();

            return services;
        }
    }
}
