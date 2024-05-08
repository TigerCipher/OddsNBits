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

using Havit.Linq;
using Microsoft.EntityFrameworkCore;
using OddsNBits.Data;
using OddsNBits.Data.Entities;
using OddsNBits.Helpers;
using OddsNBits.Models;
using System;

namespace OddsNBits.Services;

public interface IPostAdminService
{
    Task<PagedResult<BlogPost>> GetAllAsync(int startIndex, int pageSize);
    Task<PagedResult<BlogPost>> GetFilteredAsync(BlogPostFilter filter, int startIndex, int pageSize);
    Task<BlogPost?> GetByIdAsync(int id);
    Task<BlogPost?> GetBySlugAsync(string slug);
    Task<BlogPost> SaveAsync(BlogPost post, string userId);
    Task<int> GetTotalCount();
    Task DeleteAsync(BlogPost post);
    BlogPostModel ModelFromEntity(BlogPost entity);
    BlogPost EntityFromModel(BlogPostModel model, BlogPost entity);
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

    public async Task<PagedResult<BlogPost>> GetFilteredAsync(BlogPostFilter filter, int startIndex, int pageSize)
    {
        // .WhereIf(!string.IsNullOrWhiteSpace(filter.Title), e=> e.Title.Contains(filter.Title, StringComparison.CurrentCultureIgnoreCase))
        int count = 0;
        var ctx = await ExecuteOnContext(async context =>
        {
            var query = context.BlogPosts.AsNoTracking();
            var records = await query.Include(b => b.Category)
                .OrderByDescending(b => b.Id)
                .ToArrayAsync();
            count = await query.CountAsync();

            return records;
        });
        var results = ctx
            .WhereIf(!string.IsNullOrWhiteSpace(filter.Title), e => e.Title.Contains(filter.Title, StringComparison.CurrentCultureIgnoreCase))
            // Filter if start date is given but not end date, this means no range, look for single day
            .WhereIf(filter.CreationDate.StartDate is not null && filter.CreationDate.EndDate is null,
                e => e.CreatedOn.ToShortDateString() == filter.CreationDate.StartDate?.ToShortDateString())
            // Filter if both start and end date are given
            .WhereIf(filter.CreationDate.StartDate is not null && filter.CreationDate.EndDate is not null,
                e => filter.CreationDate.StartDate <= e.CreatedOn && e.CreatedOn < filter.CreationDate.EndDate!.Value.AddDays(1))
            .WhereIf(filter.ShowOnlyFeatured, e => e.IsFeatured)
            .WhereIf(filter.ShowOnlyPublished, e => e.IsPublished)
            .WhereIf(filter.ShowOnlyMainFeature, e => e.IsMainFeature)
            .Skip(startIndex)
            .Take(pageSize)
            .ToArray();


        return new PagedResult<BlogPost>(results, count);
    }

    public async Task<BlogPost?> GetByIdAsync(int id) => await ExecuteOnContext(async context =>
        await context.BlogPosts.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id)
    );

    public async Task<BlogPost?> GetBySlugAsync(string slug) => await ExecuteOnContext(async context =>
        await context.BlogPosts.AsNoTracking().Include(c => c.Category).Include(b => b.User).FirstOrDefaultAsync(c => c.Slug == slug)
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

                    if (dbpost.Title != post.Title ||
                       dbpost.Introduction != post.Introduction ||
                       dbpost.Content != post.Content ||
                       dbpost.Image != post.Image)
                    {
                        dbpost.ModifiedOn = DateTime.UtcNow;
                    }

                    dbpost.Title = post.Title;
                    dbpost.IsFeatured = post.IsFeatured;
                    dbpost.Introduction = post.Introduction;
                    dbpost.Content = post.Content;
                    dbpost.CategoryId = post.CategoryId;
                    dbpost.Image = post.Image;


                    if (post.IsPublished && !dbpost.IsPublished)
                    {
                        dbpost.PublishedOn = DateTime.UtcNow;
                    }
                    dbpost.IsPublished = post.IsPublished;

                    if (!post.IsPublished)
                    {
                        dbpost.PublishedOn = null;
                    }

                    // Main feature flag has changed, make sure all other posts have this flag set to false if we want this true
                    if (post.IsMainFeature && dbpost.IsMainFeature != post.IsMainFeature)
                    {
                        var currentMain = await ctx.BlogPosts.FirstOrDefaultAsync(b => b.IsMainFeature);
                        if (currentMain is not null)
                        {
                            currentMain.IsMainFeature = false;
                        }
                    }
                    dbpost.IsMainFeature = post.IsMainFeature;
                }

            }

            await ctx.SaveChangesAsync();
            return post;
        });
    }

    public async Task DeleteAsync(BlogPost post)
    {
        await ExecuteOnContext<Task?>(async context =>
        {
            if (post.Id != 0)
            {
                var dbpost = await context.BlogPosts.FindAsync(post.Id);
                context.BlogPosts.Remove(dbpost!);
                await context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Cannot delete a non-existent post");
            }
            return null;
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

    public BlogPost EntityFromModel(BlogPostModel model, BlogPost entity)
    {
        entity.Title = model.Title;
        entity.Introduction = model.Introduction;
        entity.Content = model.Content;
        entity.CategoryId = model.CategoryId;
        entity.IsPublished = model.IsPublished;
        entity.IsFeatured = model.IsFeatured;
        entity.IsMainFeature = model.IsMainFeature;
        return entity;
    }

    public BlogPostModel ModelFromEntity(BlogPost entity)
    {
        return new BlogPostModel
        {
            Title = entity.Title,
            Introduction = entity.Introduction,
            Content = entity.Content,
            CategoryId = entity.CategoryId,
            IsPublished = entity.IsPublished,
            IsFeatured = entity.IsFeatured,
            IsMainFeature = entity.IsMainFeature,
        };
    }
}