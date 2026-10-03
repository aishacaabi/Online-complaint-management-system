using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database: SQL Server by default; PostgreSQL when Database:Provider is "Postgres" (used on the
// hosted deployment, where SQL Server's memory requirement cannot be met).
var usePostgres = string.Equals(builder.Configuration["Database:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase);
if (usePostgres)
{
    // Store DateTime values as plain timestamps, the same way SQL Server's datetime2 does.
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Re-check each signed-in user's security stamp every minute, so a user the administrator deactivates
// is signed out promptly instead of keeping their session for hours.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Authorization: Role-Based Access Control
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy => policy.RequireRole(RoleNames.SuperAdministrator));
    options.AddPolicy("RequireOfficer", policy => policy.RequireRole(RoleNames.ComplaintOfficer));
    options.AddPolicy("RequireComplainant", policy => policy.RequireRole(RoleNames.Complainant));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole(RoleNames.SuperAdministrator, RoleNames.ComplaintOfficer));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "RequireSuperAdmin");
    options.Conventions.AuthorizeFolder("/Officer", "RequireOfficer");
    options.Conventions.AuthorizeFolder("/Complainant", "RequireComplainant");
    options.Conventions.AuthorizeFolder("/Notifications");
    options.Conventions.AuthorizeFolder("/Account/Manage");

    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Complaints/Track");
});

// Application services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// If another copy of the app is already running, say so plainly instead of failing with a long stack trace.
var urlInUse = FindUrlInUse(app.Configuration);
if (urlInUse is not null)
{
    Console.Error.WriteLine($"The application is already running at {urlInUse}");
    Console.Error.WriteLine("Open that address in your browser, or stop the other copy (Ctrl+C in its window) and run this again.");
    Environment.ExitCode = 1;
    return;
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// On a hosting platform the database container can take longer to start than the web app, so keep
// trying for a while instead of crashing on the first failed connection.
const int maxDatabaseAttempts = 24;
for (var attempt = 1; ; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        await SeedData.InitializeAsync(scope.ServiceProvider);
        break;
    }
    catch (Exception ex) when (ex is System.Data.Common.DbException or System.Net.Sockets.SocketException or InvalidOperationException or ArgumentException or PlatformNotSupportedException)
    {
        var reason = ex.GetBaseException().Message;
        if (attempt >= maxDatabaseAttempts || app.Environment.IsDevelopment())
        {
            Console.Error.WriteLine($"DATABASE ERROR: could not connect to or set up the database. {reason}");
            Console.Error.WriteLine("Check the ConnectionStrings__DefaultConnection setting (server address, database name, user and password).");
            Environment.ExitCode = 1;
            return;
        }

        Console.Error.WriteLine($"Database not ready (attempt {attempt} of {maxDatabaseAttempts}): {reason} Retrying in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

app.Run();

// Returns the first configured URL whose port is already taken (usually by another running copy of
// this app), or null when every port is free.
static string? FindUrlInUse(IConfiguration configuration)
{
    var urls = (configuration["urls"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var url in urls)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) continue;

        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, uri.Port);
            listener.Start();
            listener.Stop();
        }
        catch (System.Net.Sockets.SocketException)
        {
            return url;
        }
    }
    return null;
}
