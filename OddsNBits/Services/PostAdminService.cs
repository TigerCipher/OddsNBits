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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OddsNBits.Data;
using OddsNBits.Data.Entities;
using OddsNBits.Helpers;
using OddsNBits.Models;

namespace OddsNBits.Services;

public interface IPostAdminService
{
    Task<PagedResult<BlogPost>> GetAllAsync(int startIndex, int pageSize);
    Task<BlogPost?> GetByIdAsync(int id);
    Task<BlogPost> SaveAsync(BlogPost post, string userId);
    Task<int> GetTotalCount();
}

public class PostAdminService : IPostAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PostAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> func)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await func.Invoke(context);
    }

    public async Task<PagedResult<BlogPost>> GetAllAsync(int startIndex, int pageSize)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.BlogPosts.AsNoTracking();
            var records = await query.Include(b => b.Category)
                .OrderByDescending(b => b.Id)
                .Skip(startIndex)
                .Take(pageSize)
                .ToArrayAsync();
            var count = await query.CountAsync();

            return new PagedResult<BlogPost>(records, count);
        });
    }

    public async Task<BlogPost?> GetByIdAsync(int id) => await ExecuteOnContext(async context =>
        await context.BlogPosts.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id)
    );

    public async Task<BlogPost> SaveAsync(BlogPost post, string userId)
    {
        return await ExecuteOnContext(async ctx =>
        {
            // New post
            if (post.Id == 0)
            {
                if (await ctx.BlogPosts.AsNoTracking().AnyAsync(b => b.Title == post.Title))
                {
                    throw new InvalidOperationException($"A post with the title '{post.Title}' already exists");
                }

                post.Slug = await GenerateSlug(post);
                post.CreatedOn = DateTime.UtcNow;
                post.UserId = userId;

                if (post.IsPublished)
                {
                    post.PublishedOn = DateTime.UtcNow;
                }

                await ctx.BlogPosts.AddAsync(post);

            }
            else // Editing a post
            {
                if (await ctx.BlogPosts.AsNoTracking().AnyAsync(b => b.Title == post.Title && b.Id != post.Id))
                {
                    throw new InvalidOperationException($"A post with the title '{post.Title}' already exists");
                }

                var dbpost = await ctx.BlogPosts.FindAsync(post.Id);
                if (dbpost != null)
                {
                    dbpost.Title = post.Title;
                    dbpost.IsFeatured = post.IsFeatured;
                    dbpost.Introduction = post.Introduction;
                    dbpost.Content = post.Content;
                    dbpost.CategoryId = post.CategoryId;
                    dbpost.Image = post.Image;
                    dbpost.ModifiedOn = DateTime.UtcNow;

                    if (post.IsPublished && !dbpost.IsPublished)
                    {
                        dbpost.PublishedOn = DateTime.UtcNow;
                    }
                    dbpost.IsPublished = post.IsPublished;

                    if (!post.IsPublished)
                    {
                        dbpost.PublishedOn = null;
                    }
                }

            }

            await ctx.SaveChangesAsync();
            return post;
        });
    }

    public async Task<int> GetTotalCount()
    {
        return await ExecuteOnContext(async context => await context.BlogPosts.CountAsync());
    }

    private async Task<string> GenerateSlug(BlogPost post)
    {
        return await ExecuteOnContext(async ctx =>
        {
            var desiredSlug = post.Title.Slugify();
            var slug = desiredSlug;
            var num = 1;
            while (await ctx.BlogPosts.AsNoTracking().AnyAsync(b => b.Slug == slug))
            {
                slug = $"{desiredSlug}-{num++}";
            }

            return slug;
        });
    }
}