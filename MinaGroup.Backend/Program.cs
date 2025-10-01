using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
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

builder.Services.AddDataProtection()
    .SetApplicationName("MinaGroup.Backend")
    // Produktionsråd: persister nøgler sikkert, fx Azure Blob/KeyVault. 
    // For dev kan du bruge filsystem:
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys")));

// Register vores krypteringsservice
builder.Services.AddSingleton<ICryptoService, DataProtectionCryptoService>();

// Connection string
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr), mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    }));

// Identity + cookies
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Email og token services
builder.Services.Configure<AuthMessageSenderOptions>(options =>
{
    options.SendGridKey = builder.Configuration["SendGrid:SendGridKey"];
});
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT setup til mobilapp
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

// Authentication: cookies til web, JWT til API
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // default for web
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // brug HTTPS i prod
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            return context.Response.WriteAsync("Unauthorized.");
        }
    };
});

// Razor Pages + MVC Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// CORS til mobilapp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Tilføj cookie policy service
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Brugeren skal give samtykke til ikke-nødvendige cookies
    options.CheckConsentNeeded = context => true;

    // SameSite policy (kan være Strict, Lax eller None afhængigt af krav)
    options.MinimumSameSitePolicy = SameSiteMode.Lax;

    // Sørger for at cookies markeres som HttpOnly og Secure hvor muligt
    options.Secure = CookieSecurePolicy.Always;
});

// PDF Service.
builder.Services.AddScoped<SelfEvaluationPdfService>();

var app = builder.Build();

// Migration + seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fejl under migration eller seeding.");
    }
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

// Cookie policy middleware SKAL være før Authentication/Authorization
app.UseCookiePolicy();

app.UseAuthentication(); // vigtigt: før Authorization
app.UseAuthorization();

app.MapRazorPages();     // Browser UI
app.MapControllers();    // API routes

app.Run();