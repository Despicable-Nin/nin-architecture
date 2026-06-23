using espasyo.Application;
using espasyo.Infrastructure;
using espasyo.Infrastructure.Data;
using espasyo.WebAPI.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<MyExceptionFilter>();
});

// Load JWT settings from configuration
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
var jwtIssuer = builder.Configuration["JwtSettings:ValidIssuer"];
var jwtAudience = builder.Configuration["JwtSettings:ValidAudience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "https://localhost:17000",
                "http://localhost:17001",
                "https://127.0.0.1:17000",
                "http://127.0.0.1:17001")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
    
    // Fallback policy for development — allow any origin with credentials support
    opt.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Ensure database is created (development mode — recreates if schema changed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
    
    if (databaseProvider.ToLower() == "sqlite")
    {
        var sqliteContext = services.GetService<espasyo.Infrastructure.Data.SqliteApplicationDbContext>();
        sqliteContext?.Database.EnsureCreated();
    }
    else
    {
        var sqlServerContext = services.GetService<ApplicationDbContext>();
        sqlServerContext?.Database.EnsureCreated();
    }
    
    try
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedAdminUserAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding data: {ex.Message}");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
    app.MapGet("/swagger", async (HttpContext context) =>
    {
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Swagger UI</title>
    <meta charset=""utf-8""/>
    <link rel=""stylesheet"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"" />
</head>
<body>
    <div id=""swagger-ui""></div>
    <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
    <script>
        SwaggerUIBundle({
            url: '/openapi/v1.json',
            dom_id: '#swagger-ui'
        });
    </script>
</body>
</html>
");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS must be configured before authentication/authorization
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowFrontend");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
{
    const string adminRole = "Admin";
    const string adminEmail = "admin@example.com";
    const string adminPassword = "Admin@123";

    // 1️⃣ Ensure Admin Role Exists
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }

    // 2️⃣ Ensure Admin User Exists
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRole);
            Console.WriteLine("Admin user created successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}