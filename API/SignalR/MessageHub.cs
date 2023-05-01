using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

[Authorize]
public class MessageHub : Hub
{
    private readonly IMapper mapper;
    private readonly IHubContext<PresenceHub> presenceHub;
    private readonly IUnitOfWork unitOfWork;

    public MessageHub(IMapper mapper,
        IHubContext<PresenceHub> presenceHub,
        IUnitOfWork unitOfWork)
    {
        this.mapper = mapper;
        this.presenceHub = presenceHub;
        this.unitOfWork = unitOfWork;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var otherUser = httpContext.Request.Query["user"];
        var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        var group = await AddToGroupAsync(groupName);
        
        await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

        var messages = await unitOfWork.MessageRepository.GetMessageThreadAsync(Context.User.GetUsername(), otherUser);
        if (unitOfWork.HasChanges())
        {
            await unitOfWork.CompleteAsync();
        }
        
        await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
    }

    public async Task SendMessage(CreateMessageDto createMessageDto)
    {
        var username = Context.User.GetUsername();
        if (username == createMessageDto.RecipientUsername.ToLower())
        {
            throw new HubException("You cannot send messages to yourself");
        }

        var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        var recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);
        if (recipient == null)
        {
            throw new HubException("Not found user");
        }

        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = createMessageDto.Content
        };

        var groupName = GetGroupName(sender.UserName, recipient.UserName);
        var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);
        if (group.Connections.Any(c => c.Username == recipient.UserName))
        {
            message.DateRead = DateTime.UtcNow;
        }
        else
        {
            var connections = await PresenceTracker.GetConnectionsForUserAsync(recipient.UserName);
            if (connections != null)
            {
                await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new
                {
                    username = sender.UserName,
                    knownAs = sender.KnownAs
                });
            }
        }
        
        unitOfWork.MessageRepository.AddMessage(message);
        if (await unitOfWork.CompleteAsync())
        {
            await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var group = await RemoveFromMessageGroupAsync();
        await Clients.Group(group.Name).SendAsync("UpdatedGroup");
        await base.OnDisconnectedAsync(exception);
    }

    private string GetGroupName(string caller, string other)
    {
        var stringCompare = string.CompareOrdinal(caller, other) < 0;
        return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
    }

    private async Task<Group> AddToGroupAsync(string groupName)
    {
        var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);
        var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());
        if (group == null)
        {
            group = new Group(groupName);
            unitOfWork.MessageRepository.AddGroup(group);
        }
        
        group.Connections.Add(connection);

        if (await unitOfWork.CompleteAsync())
        {
            return group;
        }
        
        throw new HubException("Failed to add to group");
    }

    private async Task<Group> RemoveFromMessageGroupAsync()
    {
        var group = await unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
        var connection = group.Connections.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);
        unitOfWork.MessageRepository.RemoveConnection(connection);
        if (await unitOfWork.CompleteAsync())
        {
            return group;
        }

        throw new HubException("Failed to remove from group");
    }
}