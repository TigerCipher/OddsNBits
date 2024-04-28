using Microsoft.AspNetCore.Identity;
using OddsNBits.Data;

namespace OddsNBits.Services
{
    public interface ISeederService
    {
        Task SeedDataAsync();
    }

    public class Seeder : ISeederService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Seeder(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task SeedDataAsync()
        {
            // Roles
            if(await _roleManager.FindByNameAsync("Admin") is null)
            {
                var role = new IdentityRole("Admin");
                var result = await _roleManager.CreateAsync(role);
                if(!result.Succeeded)
                {
                    var err = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
                    throw new Exception(err);
                }
            }

            if (await _roleManager.FindByNameAsync("Author") is null)
            {
                var role = new IdentityRole("Author");
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var err = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
                    throw new Exception(err);
                }
            }

            if (await _roleManager.FindByNameAsync("DefaultUser") is null)
            {
                var role = new IdentityRole("DefaultUser");
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var err = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
                    throw new Exception(err);
                }
            }
        }
    }
}
