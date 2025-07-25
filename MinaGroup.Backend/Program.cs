using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Infrastructure.Identity;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Options;
using MinaGroup.Backend.Services;
using MinaGroup.Backend.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext med retry-policies
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr), mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    }));


// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz���ABCDEFGHIJKLMNOPQRSTUVWXYZ���0123456789-._@+ ";
    options.User.RequireUniqueEmail = true;
});

// JWT-konfiguration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT nøgle er ikke sat. Tjek miljøvariabler eller user secrets.");
}

if (string.IsNullOrEmpty(jwtIssuer))
{
    throw new Exception("JWT issuer er ikke sat. Tjek miljøvariabler eller user secrets.");
}

// Autentificering: b�de Cookie og JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Load Send Grid API Key.
var sendGridKey = builder.Configuration["SendGrid:SendGridKey"];

if (string.IsNullOrEmpty(sendGridKey))
{
    throw new Exception("Send Grid Key er ikke sat. Tjek miljøvariabler eller user secrets.");
}

// Add Email service.
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.Configure<AuthMessageSenderOptions>(authMessageSenderOptions =>
{
    authMessageSenderOptions.SendGridKey = builder.Configuration["SendGrid:SendGridKey"];
});

// Add Token Service.
builder.Services.AddScoped<ITokenService, TokenService>();

// MVC og Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

// Migration og seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        await DataSeeder.SeedAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during migration or seeding.");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();