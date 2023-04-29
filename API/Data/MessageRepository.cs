using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository : IMessageRepository
{
    private readonly DataContext context;
    private readonly IMapper mapper;

    public MessageRepository(DataContext context, IMapper mapper)
    {
        this.context = context;
        this.mapper = mapper;
    }
    
    public void AddMessage(Message message)
    {
        context.Messages.Add(message);
    }

    public void DeleteMessage(Message message)
    {
        context.Messages.Remove(message);
    }

    public async Task<Message> GetMessageAsync(int id)
    {
        return await context.Messages.FindAsync(id);
    }

    public async Task<PagedList<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams)
    {
        var query = context.Messages.OrderByDescending(m => m.MessageSent).AsQueryable();
        query = messageParams.Container switch
        {
            "Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username && !u.RecipientDeleted),
            "Outbox" => query.Where(u => u.SenderUsername == messageParams.Username && !u.SenderDeleted),
            _ => query.Where(u => u.RecipientUsername == messageParams.Username && !u.RecipientDeleted && u.DateRead == null)
        };

        var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

        return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }

    public async Task<IEnumerable<MessageDto>> GetMessageThreadAsync(string currentUsername, string recipientUsername)
    {
        var messages = await context.Messages
            .Include(m => m.Sender)
            .ThenInclude(p => p.Photos)
            .Include(m => m.Recipient)
            .ThenInclude(p => p.Photos)
            .Where(m => m.RecipientUsername == currentUsername && !m.RecipientDeleted
                        && m.SenderUsername == recipientUsername
                        || m.RecipientUsername == recipientUsername && !m.SenderDeleted
                        && m.SenderUsername == currentUsername)
            .OrderBy(m => m.MessageSent)
            .ToListAsync();
        
        var unreadMessages = messages.Where(m => !m.DateRead.HasValue && m.RecipientUsername == currentUsername).ToList();
        if (unreadMessages.Any())
        {
            foreach (var message in unreadMessages)
            {
                message.DateRead = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        return mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    public async Task<bool> SaveAllAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public void AddGroup(Group group)
    {
        context.Groups.Add(group);
    }

    public void RemoveConnection(Connection connection)
    {
        context.Connections.Remove(connection);
    }

    public async Task<Connection> GetConnectionAsync(string connectionId)
    {
        return await context.Connections.FindAsync(connectionId);
    }

    public Task<Group> GetMessageGroupAsync(string groupName)
    {
        return context.Groups.Include(g => g.Connections).FirstOrDefaultAsync(g => g.Name == groupName);
    }

    public Task<Group> GetGroupForConnection(string connectionId)
    {
        return context.Groups.Include(g => g.Connections)
            .Where(g => g.Connections.Any(c => c.ConnectionId == connectionId))
            .FirstOrDefaultAsync();
    }
}