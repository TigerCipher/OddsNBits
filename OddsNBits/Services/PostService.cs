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
using OddsNBits.Data;
using OddsNBits.Data.Entities;
using OddsNBits.Models;

namespace OddsNBits.Services;

public interface IPostService
{
    Task<BlogPost[]> GetFeaturedAsync(int count, int categoryId = 0);
    Task<BlogPost[]> GetPopularAsync(int count, int categoryId = 0);
    Task<BlogPost[]> GetLatestAsync(int count, int categoryId = 0);
    Task<DetailPageModel> GetBySlugAsync(string slug, int count = 4);
    Task<BlogPost?> GetMainFeatureAsync();
}

public class PostService : IPostService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PostService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> func)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await func.Invoke(context);
    }


    public async Task<BlogPost[]> GetFeaturedAsync(int count, int categoryId = 0)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.BlogPosts.AsNoTracking()
                .Include(b => b.User).Include(b => b.Category)
                .Where(b => b.IsPublished);
            if (categoryId > 0)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }

            var result = await query.Where(b=>b.IsFeatured && !b.IsMainFeature).OrderBy(_ => Guid.NewGuid()).Take(count).ToArrayAsync();
            if(result.Length < count)
            {
                // not enough featured posts
                var fillers = await query.Where(b => !b.IsFeatured && !b.IsMainFeature).OrderBy(_ => Guid.NewGuid()).Take(count - result.Length).ToArrayAsync();
                result = [.. result, .. fillers];
            }

            return result;
        });
    }

    public async Task<BlogPost?> GetMainFeatureAsync()
    {
        return await ExecuteOnContext(async context =>
        {
            return await context.BlogPosts.AsNoTracking()
                .Include(b => b.User).Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.IsPublished && b.IsMainFeature);

        });
    }

    public async Task<BlogPost[]> GetPopularAsync(int count, int categoryId = 0)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.BlogPosts.AsNoTracking()
                .Include(b => b.User).Include(b => b.Category)
                .Where(b => b.IsPublished);
            if (categoryId > 0)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }

            return await query.OrderByDescending(b => b.ViewCount).Take(count).ToArrayAsync();
        });
    }

    public async Task<BlogPost[]> GetLatestAsync(int count, int categoryId = 0)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.BlogPosts.AsNoTracking()
                .Include(b => b.User).Include(b => b.Category)
                .Where(b => b.IsPublished);
            if (categoryId > 0)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }

            return await query.OrderByDescending(b => b.PublishedOn).Take(count).ToArrayAsync();
        });
    }

    public async Task<DetailPageModel> GetBySlugAsync(string slug, int count = 4)
    {
        return await ExecuteOnContext(async context =>
        {
            var post = await context.BlogPosts.AsNoTracking()
                .Include(b => b.Category).Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);

            if(post is null)
            {
                return DetailPageModel.Empty();
            }

            var relatedPosts = await context.BlogPosts.AsNoTracking()
                .Include(b => b.Category).Include(b => b.User)
                .Where(b => b.CategoryId == post.CategoryId && b.IsPublished)
                .OrderBy(_ => Guid.NewGuid())
                .Take(count).ToArrayAsync();

            return new DetailPageModel(post, relatedPosts);
        });
    }
}