using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Shandis.Application;
using Shandis.Infrastructure;
using Shandis.Infrastructure.Identity;
using Shandis.Infrastructure.Persistence;
using Shandis.Infrastructure.Persistence.Seeders;

var builder = WebApplication.CreateBuilder(args);

//Dependency Injection
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Data Protection (required by Identity token providers)
builder.Services.AddDataProtection();

// Database
builder.Services.AddDbContextFactory<ShandisDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services
    .AddIdentityApiEndpoints<ShandisUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ShandisDbContext>();

// Authorization
builder.Services.AddAuthorization();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Shandis API",
        Version = "v1",
        Description = "API for the Shandis collective memory platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Enter your Shandis Identity access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ShandisDbContext>();

    await MemoryCategorySeeder.SeedAsync(dbContext);

    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ShandisUser>>();

        var testPassword = builder.Configuration["DevelopmentUsers:Password"]
            ?? "Shandis123!";

        await DevelopmentUserSeeder.SeedAsync(
            userManager,
            testPassword);
    }
}

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Shandis API v1");

        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapGroup("/api/auth")
    .MapIdentityApi<ShandisUser>();

app.MapControllers();

app.Run();
