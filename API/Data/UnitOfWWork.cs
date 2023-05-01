using API.Interfaces;
using AutoMapper;

namespace API.Data;

public class UnitOfWWork : IUnitOfWork
{
    private readonly DataContext context;
    private readonly IMapper mapper;

    public UnitOfWWork(DataContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }

    public IUserRepository UserRepository => new UserRepository(context, mapper);
    
    public IMessageRepository MessageRepository => new MessageRepository(context, mapper);
    
    public ILikesRepository LikesRepository => new LikesRepository(context);

    public IPhotoRepository PhotoRepository => new PhotoRepository(context, mapper);

    public async Task<bool> CompleteAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return context.ChangeTracker.HasChanges();
    }
}