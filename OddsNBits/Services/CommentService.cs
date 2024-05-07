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

using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OddsNBits.Data;
using OddsNBits.Data.Entities;
using OddsNBits.Models;

namespace OddsNBits.Services;

public interface ICommentService
{
    Task<ICollection<Comment>> GetAllAsync(int postId);
    Task<Comment> AddNewAsync(Comment cmt, string userid, int blogPostId);
    Task<Comment> AddNewReply(Comment cmt, Comment parentCmt, string userid, int blogPostId);
    Task<ICollection<Comment>> GetAllReplies(int parentId);
}

public class CommentService : ICommentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public CommentService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private async Task<TResult> ExecuteOnContext<TResult>(Func<ApplicationDbContext, Task<TResult>> func)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await func.Invoke(context);
    }

    public async Task<ICollection<Comment>> GetAllAsync(int postId)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.Comments.AsNoTracking();
            var records = await query.Include(c=>c.User)
                .OrderByDescending(b => b.CreatedDate)
                .ToArrayAsync();

            return records;
        });
    }

    public async Task<ICollection<Comment>> GetAllReplies(int parentId)
    {
        return await ExecuteOnContext(async context =>
        {
            var query = context.Comments.AsNoTracking();
            var records = await query.Include(c => c.User)
                .Where(c=> c.ParentCommentId == parentId)
                .OrderByDescending(b => b.CreatedDate)
                .ToArrayAsync();

            return records;
        });
    }

    public async Task<Comment> AddNewAsync(Comment cmt, string userid, int blogPostId)
    {
        return await ExecuteOnContext(async ctx =>
        {
            if(await ctx.BlogPosts.FindAsync(blogPostId) is null)
            {
                throw new InvalidOperationException("Attempted to save a comment to an invalid blog post");
            }
            cmt.CreatedDate = DateTime.UtcNow;
            cmt.UserId = userid;
            cmt.BlogPostId = blogPostId;

            await ctx.Comments.AddAsync(cmt);

            await ctx.SaveChangesAsync();
            return cmt;
        });
    }

    public async Task<Comment> AddNewReply(Comment cmt, Comment parentCmt, string userid, int blogPostId)
    {
        return await ExecuteOnContext(async ctx =>
        {
            if (await ctx.BlogPosts.FindAsync(blogPostId) is null)
            {
                throw new InvalidOperationException("Attempted to save a comment to an invalid blog post");
            }

            if(await ctx.Comments.FindAsync(parentCmt.Id) is null)
            {
                throw new InvalidOperationException("Failed to save a comment to an invalid parent comment");
            }
            cmt.CreatedDate = DateTime.UtcNow;
            cmt.UserId = userid;
            cmt.BlogPostId = blogPostId;
            cmt.ParentCommentId = parentCmt.Id;

            if(parentCmt.FirstLevelParentCommentId.HasValue)
            {
                cmt.FirstLevelParentCommentId = parentCmt.FirstLevelParentCommentId;
            }else if(parentCmt.ParentCommentId.HasValue)
            {
                cmt.FirstLevelParentCommentId = parentCmt.ParentCommentId;
            }else
            {
                cmt.FirstLevelParentCommentId = parentCmt.Id;
            }
            

            await ctx.Comments.AddAsync(cmt);

            await ctx.SaveChangesAsync();
            return cmt;
        });
    }
}