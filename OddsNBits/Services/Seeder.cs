// -------------------------------------------------------------------------------
//     OddsNBits - A Blazor Web App serving as my dev log / blog site
//     Copyright (C) 2024  Matt Rogers
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// -------------------------------------------------------------------------------
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        // Should only use in dev environment
        private async Task ApplyMigration()
        {
#if DEBUG
            if((await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                await _context.Database.MigrateAsync();
            }
#endif
        }

        public async Task SeedDataAsync()
        {
            await ApplyMigration();

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
