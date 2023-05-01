using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LikesController : BaseApiController
{
    private readonly IUnitOfWork unitOfWork;

    public LikesController(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    [HttpPost("{username}")]
    public async Task<ActionResult> AddLike(string username)
    {
        var sourceUserId = User.GetUserId();
        var likedUser = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        var sourceUser = await unitOfWork.LikesRepository.GetUserWithLikesAsync(sourceUserId);
        if (likedUser == null)
        {
            return NotFound();
        }

        if (username == sourceUser.UserName)
        {
            return BadRequest("You cannot like yourself");
        }

        var userLike = await unitOfWork.LikesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);
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
        if (await unitOfWork.CompleteAsync())
        {
            return Ok();
        }

        return BadRequest("Failed to like user");
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<LikeDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
    {
        likesParams.UserId = User.GetUserId();
        var users = await unitOfWork.LikesRepository.GetUserLikesAsync(likesParams);
        Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
        return Ok(users);
    }
}