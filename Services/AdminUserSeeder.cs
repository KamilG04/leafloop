// Services/AdminUserSeeder.cs
using LeafLoop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace LeafLoop.Services
{
    public static class AdminUserSeeder
    {
        public static async Task SeedAdminUser(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            
            var adminEmail = configuration["AdminUser:Email"] ?? "admin@leafloop.com";
            var adminPassword = configuration["AdminUser:Password"] ?? "Admin123!@#";
            
            // Check if admin user already exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                // Create admin user
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    EcoScore = 0
                };
                
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    // Assign admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Admin user created successfully with email: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors)}");
                }
            }
            else
            {
                // Ensure existing user has admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Admin role added to existing user: {adminEmail}");
                }
            }
        }
    }
}