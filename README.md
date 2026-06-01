# Chaotic Cupid

A PubSub matchmaking simulation built with **ASP.NET Core + SignalR** on **.NET 10**.

Cupid (a background publisher on the server) periodically scores every registered
person against the others and pushes a single "love letter" to each person's best
match. People connect through a console client to register, acknowledge letters,
and block unwanted senders.

See [`SPECIFICATION.md`](SPECIFICATION.md) for the full feature requirements and
[`AGENTS.md`](AGENTS.md) for the implementation conventions.

## Architecture

```
ChaoticCupid.slnx
└── src/
    ├── ChaoticCupid.Shared/     # DTOs shared by client + server (PersonInfo, LoveLetter, ILetterClient)
    ├── ChaoticCupid.Server/     # SignalR hub + Cupid background dispatcher
    │   ├── Contracts/           # IPersonService, ICupidService (kept separate)
    │   ├── Hubs/CupidHub.cs      # Person-facing PubSub endpoint
    │   ├── Services/CupidService.cs  # Scoring + dispatch (publisher)
    │   ├── State/                # Thread-safe PersonRegistry + RegisteredPerson
    │   └── Helpers/CryptoRandom.cs   # Cryptographic RNG
    └── ChaoticCupid.Client/     # Console client (subscriber)
```

- **Two interfaces** are kept separate: `IPersonService` (register / acknowledge /
  block) and `ICupidService` (scoring + dispatch).
- All shared state lives in a thread-safe `PersonRegistry` (`ConcurrentDictionary`
  + per-person locks / interlocked flags).
- The matchmaking random factor uses `System.Security.Cryptography.RandomNumberGenerator`
  (the supported replacement for the obsolete `RNGCryptoServiceProvider`); `System.Random`
  is never used in scoring.

## Prerequisites

- .NET SDK 10.x (`dotnet --version`)

## Build

```bash
dotnet build ChaoticCupid.slnx
```

## Run

Start the server (listens on `http://localhost:5188`, hub at `/cupid`):

```bash
cd src/ChaoticCupid.Server
ASPNETCORE_URLS="http://localhost:5188" dotnet run --no-launch-profile
```

In a separate terminal, start a client (run several in parallel to see matchmaking):

```bash
cd src/ChaoticCupid.Client
dotnet run
```

To point the client at a non-default server, pass the hub URL as an argument:

```bash
dotnet run -- http://localhost:5188/cupid
```

## Using the client

1. **Register** by entering `username`, `city`, `age`, and `phone number` when
   prompted. Empty values, non-numeric age/phone, and negative age are rejected.
2. Once two or more people are registered, Cupid dispatches one letter per person
   **every 60 seconds** to their highest-scoring match.
3. When a letter arrives, **press Enter** to acknowledge it. No new letter is
   delivered until you do.
4. Block a sender at any time:

   ```
   /block <username>
   ```

5. Type `/quit` to leave.

## Scoring

For each recipient, every other person is scored and the highest wins:

| Condition                 | Points        |
|---------------------------|---------------|
| Same city                 | +30           |
| Age within +/-2 years     | +20           |
| Cryptographic random      | +0 .. +100    |

Each delivered letter carries one of three random messages. For
"I am not interested in getting acquainted." the sender's phone number is hidden;
for the other two it is shown.
