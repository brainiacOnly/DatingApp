using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly DataContext context;
    private readonly ITokenService tokenService;
    private readonly IMapper mapper;

    public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
    {
        this.context = context;
        this.tokenService = tokenService;
        this.mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await IsUserExists(registerDto.Username))
        {
            return BadRequest("Username is taken");
        }

        var user = mapper.Map<AppUser>(registerDto);
        
        using var hmac = new HMACSHA512();
        user.UserName = registerDto.Username.ToLower();
        user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        user.PasswordSalt = hmac.Key;

        context.Users.Add(user);

        await context.SaveChangesAsync();
        
        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
            KnownAs = user.KnownAs
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await context.Users.Include(u => u.Photos).SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
        if (user == null)
        {
            return Unauthorized("invalid username");
        }
        
        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password)); 
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i])
            {
                return Unauthorized("invalid password");
            }
        }

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain)?.Url,
            KnownAs = user.KnownAs
        };
    }

    private Task<bool> IsUserExists(string username)
    {
        return context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }
}