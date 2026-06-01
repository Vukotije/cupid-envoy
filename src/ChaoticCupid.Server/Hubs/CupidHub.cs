using ChaoticCupid.Server.Contracts;
using ChaoticCupid.Server.State;
using ChaoticCupid.Shared;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticCupid.Server.Hubs;

/// <summary>
/// Person-facing PubSub endpoint. Each connected console client is a subscriber;
/// Cupid (the publisher) pushes letters back through <see cref="ILetterClient"/>.
/// Business/matchmaking logic deliberately lives in the Cupid service, not here.
/// </summary>
public sealed class CupidHub : Hub<ILetterClient>, IPersonService
{
    private readonly PersonRegistry _registry;
    private readonly ILogger<CupidHub> _logger;

    public CupidHub(PersonRegistry registry, ILogger<CupidHub> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public Task InitSinglePerson(PersonInfo person)
    {
        if (person is null || string.IsNullOrWhiteSpace(person.Username))
        {
            throw new HubException("A username is required to register.");
        }

        if (!_registry.TryRegister(Context.ConnectionId, person, out _))
        {
            throw new HubException($"The username '{person.Username}' is already taken.");
        }

        _logger.LogInformation("Registered {Username} ({ConnectionId}).", person.Username, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public Task AcknowledgeLetter()
    {
        if (_registry.TryGetByConnection(Context.ConnectionId, out var person) && person is not null)
        {
            person.AcknowledgeLetter();
        }

        return Task.CompletedTask;
    }

    public Task BlockUser(string username)
    {
        if (_registry.TryGetByConnection(Context.ConnectionId, out var person) && person is not null)
        {
            person.Block(username);
            _logger.LogInformation("{User} blocked {Blocked}.", person.Username, username);
        }

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.RemoveByConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
