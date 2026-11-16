using Microsoft.Extensions.DependencyInjection;
using Shandis.Application.Features.Invitations;
using Shandis.Application.Features.Memories;
using Shandis.Application.Features.Moments;

namespace Shandis.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IMemoryService, MemoryService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<IMomentService, MomentService>();

            return services;
        }
    }
}
