using Azure.Identity;
using Azure.Extensions.AspNetCore.DataProtection.Blobs;
using Azure.Extensions.AspNetCore.DataProtection.Keys;
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

// ---------- Azure AD Service Principal credentials ----------
var tenantId = builder.Configuration["AzureAd:TenantId"];
var clientId = builder.Configuration["AzureAd:ClientId"];
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];

if (string.IsNullOrWhiteSpace(tenantId) ||
    string.IsNullOrWhiteSpace(clientId) ||
    string.IsNullOrWhiteSpace(clientSecret))
{
    throw new InvalidOperationException(
        "AzureAd:TenantId, AzureAd:ClientId eller AzureAd:ClientSecret mangler i konfigurationen.");
}

// Brug en ren ClientSecretCredential (samme i dev og prod)
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

// ---------- Konfiguration til Data Protection / Key Vault ----------
var dpContainerUri = builder.Configuration["DataProtection:BlobUri"]; // fx https://samgprod.blob.core.windows.net/dataprotection-keys
var kvVaultUri = builder.Configuration["KeyVault:VaultUri"];      // fx https://kv-minagroup-prod.vault.azure.net/
var kvKeyName = builder.Configuration["KeyVault:KeyName"];       // fx dataprotection-key

if (string.IsNullOrWhiteSpace(dpContainerUri) ||
    string.IsNullOrWhiteSpace(kvVaultUri) ||
    string.IsNullOrWhiteSpace(kvKeyName))
{
    throw new InvalidOperationException(
        "DataProtection:BlobUri, KeyVault:VaultUri eller KeyVault:KeyName mangler i konfigurationen.");
}

// Blob hvor Data Protection keyring gemmes (én xml-fil i containeren)
var blobUri = new Uri($"{dpContainerUri.TrimEnd('/')}/keys.xml");

// Key Vault key identifier
var keyIdentifier = new Uri($"{kvVaultUri.TrimEnd('/')}/keys/{kvKeyName}");

// ---------- Data Protection ----------
builder.Services.AddDataProtection()
    .SetApplicationName("MinaGroup.Backend")
    .PersistKeysToAzureBlobStorage(blobUri, credential)
    .ProtectKeysWithAzureKeyVault(keyIdentifier, credential);

// Register vores krypteringsservice, der bruger Data Protection
builder.Services.AddSingleton<ICryptoService, DataProtectionCryptoService>();

// ---------- Database ----------
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr), mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    }));

// ---------- Identity + cookies ----------
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

// ---------- Email og token services ----------
var sendGridKey = builder.Configuration["SendGrid:SendGridKey"];
if (string.IsNullOrWhiteSpace(sendGridKey))
{
    throw new InvalidOperationException("SendGrid:SendGridKey er ikke sat i konfigurationen.");
}

builder.Services.Configure<AuthMessageSenderOptions>(options =>
{
    options.SendGridKey = sendGridKey;
});
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ---------- JWT setup til mobilapp ----------
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key er ikke sat i konfigurationen.");
}

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // skru evt. op til Always i prod
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

// ---------- MVC / Razor Pages ----------
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// ---------- CORS (til mobilapp) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ---------- Cookie policy ----------
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

// ---------- PDF Service ----------
builder.Services.AddScoped<SelfEvaluationPdfService>();

var app = builder.Build();

// ---------- Migration + seeding ----------
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

// ---------- Middleware pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

// Cookie policy før auth
app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();