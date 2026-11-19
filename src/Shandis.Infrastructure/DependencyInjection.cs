using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shandis.Application.Abstractions.Email;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Abstractions.Storage;
using Shandis.Infrastructure.Options;
using Shandis.Infrastructure.Persistence.Repositories;
using Shandis.Infrastructure.Services;
using Shandis.Infrastructure.Services.Storage;

namespace Shandis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IMemoryRepository, MemoryRepository>();
            services.AddScoped<IInvitationRepository, InvitationRepository>();
            services.AddScoped<IEmailService, LoggingEmailService>();
            services.AddScoped<IMomentRepository, MomentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

            services.AddOptions<SupabaseStorageOptions>()
                .Bind(configuration.GetSection(SupabaseStorageOptions.SectionName))
                .Validate(
                    options => Uri.TryCreate(
                        options.Url,
                        UriKind.Absolute,
                        out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttps ||
                         uri.Scheme == Uri.UriSchemeHttp),
                    "A valid Supabase project URL is required.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.SecretKey),
                    "The Supabase secret key is required.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.BucketName),
                    "The Supabase bucket name is required.")
                .ValidateOnStart();

            services.AddHttpClient<IMediaStorageService, SupabaseMediaStorageService>();

            return services;
        }
    }
}
