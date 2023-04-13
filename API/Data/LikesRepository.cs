using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository : ILikesRepository
{
    private readonly DataContext context;

    public LikesRepository(DataContext context)
    {
        this.context = context;
    }
    
    public async Task<UserLike> GetUserLikeAsync(int sourceUserId, int targetUserId)
    {
        return await context.Likes.FindAsync(sourceUserId, targetUserId);
    }

    public Task<AppUser> GetUserWithLikesAsync(int userId)
    {
        return context.Users.Include(u => u.LikedUsers).FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<PagedList<LikeDto>> GetUserLikesAsync(LikesParams likesParams)
    {
        var users = context.Users.OrderBy(u => u.UserName).AsQueryable();
        var likes = context.Likes.AsQueryable();
        if (likesParams.Predicate == "liked")
        {
            likes = likes.Where(l => l.SourceUserId == likesParams.UserId);
            users = likes.Select(l => l.TargetUser);
        }
        
        if (likesParams.Predicate == "likedBy")
        {
            likes = likes.Where(l => l.TargetUserId == likesParams.UserId);
            users = likes.Select(l => l.SourceUser);
        }

        var likedUsers = users.Select(u => new LikeDto
        {
            UserName = u.UserName,
            KnownAs = u.KnownAs,
            Age = u.DateOfBirth.CalculateAge(),
            PhotoUrl = u.Photos.FirstOrDefault(p => p.IsMain).Url,
            City = u.City,
            Id = u.Id
        });

        return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
    }
}