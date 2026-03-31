using BusinessAiAgent.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessAiAgent.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = ["Admin", "Customer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@businessai.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        const string customerEmail = "customer@businessai.com";
        if (await userManager.FindByEmailAsync(customerEmail) == null)
        {
            var customer = new AppUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                FullName = "Demo Customer",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(customer, "Customer123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(customer, "Customer");
        }
    }
}
