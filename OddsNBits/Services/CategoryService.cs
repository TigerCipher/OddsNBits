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

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using OddsNBits.Data;
using OddsNBits.Data.Entities;
using OddsNBits.Helpers;

namespace OddsNBits.Services;

public interface ICategoryService
{
    Task<Category[]> GetCategoriesAsync();
    Task<Category> SaveCategoryAsync(Category category);
    Task<Category?> GetBySlugAsync(string slug);
    Task DeleteCategoryAsync(Category category);
}

public class CategoryService(IDbContextFactory<ApplicationDbContext> contextFactory) : ICategoryService
{
    private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> func)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await func.Invoke(context);
    }

    public async Task<Category[]> GetCategoriesAsync()
    {
        return await ExecuteOnContext(context =>
        {
            var categories = context.Categories.AsNoTracking().ToArrayAsync();
            return categories;
        });
    }

    public async Task<Category> SaveCategoryAsync(Category category)
    {
        return await ExecuteOnContext(async context =>
        {
            // New category
            if (category.Id == 0)
            {
                if (await context.Categories.AsNoTracking().AnyAsync(c => c.Name == category.Name))
                {
                    throw new InvalidOperationException($"A category with the name {category.Name} already exists");
                }

                category.Slug = category.Name.Slugify();
                await context.Categories.AddAsync(category);

            }
            else // existing category
            {
                if (await context.Categories.AsNoTracking().AnyAsync(c => c.Name == category.Name && c.Id != category.Id))
                {
                    throw new InvalidOperationException($"A category with the name {category.Name} already exists");
                }

                var dbcategory = await context.Categories.FindAsync(category.Id);
                category.Slug = dbcategory!.Slug; // just in case an admin somehow modifies a slug
                dbcategory.Name = category.Name;
                dbcategory.Featured = category.Featured;
            }

            await context.SaveChangesAsync();
            return category;
        });
    }

    public async Task DeleteCategoryAsync(Category category)
    {
        await ExecuteOnContext<Task?>(async context =>
        {
            if(category.Id != 0)
            {
                var dbcategory = await context.Categories.FindAsync(category.Id);
                context.Categories.Remove(dbcategory!);
                await context.SaveChangesAsync();
            }else
            {
                throw new Exception("Cannot delete a non-existent category");
            }
            return null;
        });
    }

    public async Task<Category?> GetBySlugAsync(string slug) => await ExecuteOnContext(async context =>
        await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug)
    );
}