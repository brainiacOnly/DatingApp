using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AdminController : BaseApiController
{
    private readonly UserManager<AppUser> userManager;
    private readonly IUnitOfWork unitOfWork;
    private readonly IPhotoService photoService;

    public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IPhotoService photoService)
    {
        this.userManager = userManager;
        this.unitOfWork = unitOfWork;
        this.photoService = photoService;
    }
    
    [Authorize(Policy = "RequiredAdminRole")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await userManager.Users
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                Username = u.UserName,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync();
        
        return Ok(users);
    }

    [Authorize(Policy = "RequiredAdminRole")]
    [HttpPost("edit-roles/{username}")]
    public async Task<ActionResult> EditRoles(string username, [FromQuery]string roles)
    {
        if (string.IsNullOrEmpty(roles))
        {
            return BadRequest("You must select at least one role");
        }

        var selectedRoles = roles.Split(",").ToArray();
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await userManager.GetRolesAsync(user);
        var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
        if (!result.Succeeded)
        {
            BadRequest("Failed to add to roles");
        }

        result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
        if (!result.Succeeded)
        {
            BadRequest("Failed to remove from roles");
        }

        return Ok(await userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photos-to-moderate")]
    public async Task<ActionResult<PhotoForApprovalDto>> GetPhotosForModeration()
    {
        var photos = await unitOfWork.PhotoRepository.GetUnapprovedPhotosAsync();
        return Ok(photos);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approve-photo/{id}")]
    public async Task<ActionResult> ApprovePhoto(int id)
    {
        var photo = await unitOfWork.PhotoRepository.GetPhotoByIdAsync(id);
        if (photo == null)
        {
            return NotFound();
        }

        photo.IsApproved = true;
        var user = await unitOfWork.UserRepository.GetUserByPhotoId(id);
        if (!user.Photos.Any(p => p.IsMain))
        {
            photo.IsMain = true;
        }

        if (await unitOfWork.CompleteAsync())
        {
            return Ok();
        }
        
        return BadRequest("Problem approving photo");
    }
    
    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("reject-photo/{id}")]
    public async Task<ActionResult> RejectPhoto(int id)
    {
        var photo = await unitOfWork.PhotoRepository.GetPhotoByIdAsync(id);
        if (photo == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(photo.PublicId))
        {
            var result = await photoService.DeletePhotoAsync(photo.PublicId);
            if (result.Result == "ok")
            {
                unitOfWork.PhotoRepository.RemovePhoto(photo);
            }
        }
        else
        {
            unitOfWork.PhotoRepository.RemovePhoto(photo);
        }

        if (await unitOfWork.CompleteAsync())
        {
            return Ok();
        }
        
        return BadRequest("Problem rejecting photo");
    }
}