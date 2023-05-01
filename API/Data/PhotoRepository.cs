using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class PhotoRepository : IPhotoRepository
{
    private readonly DataContext context;
    private readonly IMapper mapper;

    public PhotoRepository(DataContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }
    
    public async Task<IEnumerable<PhotoForApprovalDto>> GetUnapprovedPhotosAsync()
    {
        return await context.Photos
            .Where(p => !p.IsApproved)
            .IgnoreQueryFilters()
            .ProjectTo<PhotoForApprovalDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Photo> GetPhotoByIdAsync(int id)
    {
        return await context.Photos.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id);
    }

    public void RemovePhoto(Photo photo)
    {
        context.Photos.Remove(photo);
    }
}