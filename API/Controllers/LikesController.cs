using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LikesController : BaseApiController
{
    private readonly ILikesRepository likesRepository;
    private readonly IUserRepository userRepository;

    public LikesController(ILikesRepository likesRepository, IUserRepository userRepository)
    {
        this.likesRepository = likesRepository;
        this.userRepository = userRepository;
    }

    [HttpPost("{username}")]
    public async Task<ActionResult> AddLike(string username)
    {
        var sourceUserId = User.GetUserId();
        var likedUser = await userRepository.GetUserByUsernameAsync(username);
        var sourceUser = await likesRepository.GetUserWithLikesAsync(sourceUserId);
        if (likedUser == null)
        {
            return NotFound();
        }

        if (username == sourceUser.UserName)
        {
            return BadRequest("You cannot like yourself");
        }

        var userLike = await likesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);
        if (userLike != null)
        {
            return BadRequest("You already like this user");
        }

        userLike = new UserLike
        {
            SourceUserId = sourceUserId,
            TargetUserId = likedUser.Id
        };
        
        sourceUser.LikedUsers.Add(userLike);
        if (await userRepository.SaveAllAsync())
        {
            return Ok();
        }

        return BadRequest("Failed to like user");
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<LikeDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
    {
        likesParams.UserId = User.GetUserId();
        var users = await likesRepository.GetUserLikesAsync(likesParams);
        Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
        return Ok(users);
    }
}