using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsNpgsql())
        {
            // The migrations are written for SQL Server, so on PostgreSQL the schema is created
            // directly from the model instead.
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { RoleNames.SuperAdministrator, RoleNames.ComplaintOfficer, RoleNames.Complainant })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { Name = "General Administration", Description = "Handles general administrative and clerical matters.", ContactEmail = "admin.dept@example.edu" },
                new Department { Name = "Academic Affairs", Description = "Handles academic-related complaints and grievances.", ContactEmail = "academic.dept@example.edu" },
                new Department { Name = "Finance Office", Description = "Handles billing, fees, and financial matters.", ContactEmail = "finance.dept@example.edu" },
                new Department { Name = "Facilities & Infrastructure", Description = "Handles building, maintenance, and infrastructure issues.", ContactEmail = "facilities.dept@example.edu" },
                new Department { Name = "Human Resources", Description = "Handles staff conduct and personnel matters.", ContactEmail = "hr.dept@example.edu" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.ComplaintCategories.AnyAsync())
        {
            context.ComplaintCategories.AddRange(
                new ComplaintCategory { Name = "Service Quality", Description = "Complaints about the quality of service received." },
                new ComplaintCategory { Name = "Staff Behavior", Description = "Complaints regarding staff conduct or attitude." },
                new ComplaintCategory { Name = "Delay in Service", Description = "Complaints about delays in processing or service delivery." },
                new ComplaintCategory { Name = "Infrastructure", Description = "Complaints about facilities, buildings, or equipment." },
                new ComplaintCategory { Name = "Financial Issues", Description = "Complaints related to billing, fees, or payments." },
                new ComplaintCategory { Name = "Academic Issues", Description = "Complaints related to academic matters." },
                new ComplaintCategory { Name = "Administrative Issues", Description = "Complaints related to administrative processes." },
                new ComplaintCategory { Name = "Other", Description = "Complaints that do not fit into other categories." }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.SystemSettings.AnyAsync())
        {
            context.SystemSettings.Add(new SystemSetting
            {
                OrganizationName = "Online Complaint Management System",
                DefaultResolutionDays = 14,
                SupportEmail = "support@cms.local"
            });
            await context.SaveChangesAsync();
        }

        var adminEmail = environment.IsDevelopment()
            ? configuration["BootstrapAdmin:Email"] ?? "admin@cms.local"
            : configuration["BootstrapAdmin:Email"];
        var adminPassword = environment.IsDevelopment()
            ? configuration["BootstrapAdmin:Password"] ?? "Admin@12345"
            : configuration["BootstrapAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword) &&
            await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, RoleNames.SuperAdministrator);
            }
        }

        const string officerEmail = "officer@cms.local";
        if (environment.IsDevelopment() && await userManager.FindByEmailAsync(officerEmail) is null)
        {
            // Departments can be renamed or removed by the administrator, so fall back to any active one
            // rather than failing application startup.
            var generalDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "General Administration")
                ?? await context.Departments.Where(d => d.IsActive).OrderBy(d => d.Id).FirstOrDefaultAsync();
            var officer = new ApplicationUser
            {
                UserName = officerEmail,
                Email = officerEmail,
                FullName = "Sample Complaint Officer",
                EmailConfirmed = true,
                IsActive = true,
                DepartmentId = generalDept?.Id,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(officer, "Officer@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(officer, RoleNames.ComplaintOfficer);
            }
        }
    }
}
