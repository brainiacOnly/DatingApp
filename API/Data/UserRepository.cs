using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository : IUserRepository
{
    private readonly DataContext context;
    private readonly IMapper mapper;

    public UserRepository(DataContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }
    
    public void Update(AppUser user)
    {
        context.Entry(user).State = EntityState.Modified;
    }

    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<AppUser>> GetUsersAsync()
    {
        return await context.Users
            .Include(u => u.Photos)
            .ToListAsync();
    }

    public async Task<AppUser> GetUserByIdAsync(int id)
    {
        return await context.Users.FindAsync(id);
    }

    public Task<AppUser> GetUserByUsernameAsync(string username)
    {
        return context.Users.Include(u => u.Photos).SingleOrDefaultAsync(u => u.UserName == username);
    }

    public async Task<IEnumerable<MemberDto>> GetMembersAsync()
    {
        return await context.Users.ProjectTo<MemberDto>(mapper.ConfigurationProvider).ToListAsync();
    }

    public Task<MemberDto> GetMemberAsync(string username)
    {
        return context.Users.Where(u => u.UserName == username).ProjectTo<MemberDto>(mapper.ConfigurationProvider).SingleOrDefaultAsync();
    }
}