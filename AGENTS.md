# AGENTS.md — Cursor AI Instructions for Chaotic Cupid

This file provides binding instructions for Cursor (and any AI coding agent) working in this repository.
Read `SPECIFICATION.md` first for the full feature requirements before making any changes.

---

## Project Context

This is a **.NET 10 PubSub application** implementing a matchmaking simulation, built with
**ASP.NET Core + SignalR** (chosen over WCF, whose server side is not supported on modern
.NET / macOS).
It has two distinct interfaces (`IPersonService`, `ICupidService`) and a console client for user interaction.

The solution (`ChaoticCupid.slnx`) has three projects:

- `src/ChaoticCupid.Shared` — DTOs shared by client and server.
- `src/ChaoticCupid.Server` — SignalR hub (`CupidHub`) + Cupid background dispatcher (`CupidService`).
- `src/ChaoticCupid.Client` — console client (the subscriber).

---

## Architecture Rules

- **Two service interfaces** must remain separate: `IPersonService` (implemented by `CupidHub`) and `ICupidService` (implemented by `CupidService`).
- Use a **PubSub pattern**: Cupid publishes letters via SignalR (`IHubContext<CupidHub, ILetterClient>`); registered persons are subscribers that receive `ILetterClient.ReceiveLetter`.
- All **shared state** lives in the thread-safe `PersonRegistry` singleton (`ConcurrentDictionary` + per-person locks / interlocked flags in `RegisteredPerson`).
- The scoring and letter-dispatch logic belongs exclusively in the **Cupid service**, not in person-side code.

---

## Naming Conventions

| Concept | Name (as implemented) |
|---------|------|
| Person registration method | `InitSinglePerson` |
| Person data model | `PersonInfo` (plain DTO in `ChaoticCupid.Shared`) |
| Letter payload model | `LoveLetter` (DTO; `SenderPhone` is nullable so it can be omitted) |
| Client callback contract | `ILetterClient.ReceiveLetter(LoveLetter)` |
| Cupid scheduling logic | `CupidService` (a `BackgroundService` implementing `ICupidService`) |
| Letter dispatch method | `DispatchLetters` |
| Person hub | `CupidHub : Hub<ILetterClient>, IPersonService` |
| Shared state store | `PersonRegistry` (singleton) |
| Per-person state | `RegisteredPerson` (holds `PersonInfo`, connection id, pending-ack flag, blocked set) |
| Block list per person | `HashSet<string>` (case-insensitive) inside `RegisteredPerson` |

Follow existing naming if already established in the codebase — do not rename without a clear reason.

---

## Implementation Guidelines

### Registration & Validation (`InitSinglePerson`)

- Prompt for: `username`, `city`, `age`, `phone number` in that order.
- Validate each field before proceeding:
  - Empty or whitespace → print error, re-prompt.
  - Non-numeric input for `age` or `phone` → print error, re-prompt.
  - Negative value for `age` → print error, re-prompt.
- Only register the person after **all fields pass validation**.

### Cupid Scheduling

- Use a timer that fires **every 60 seconds** (implemented with `PeriodicTimer` inside `CupidService.ExecuteAsync`; interval is the constant `LetterIntervalSeconds`).
- On each tick (`DispatchLetters`), iterate all registered persons and dispatch one letter per person.
- Skip self-to-self letters (sender == recipient).
- Skip letters to recipients who have a **pending unacknowledged letter**.
- Skip letters from senders on the recipient's **block list**.

### Scoring Algorithm

Compute a score for each candidate sender against a recipient:

```csharp
int score = 0;
if (candidate.City == recipient.City) score += 30;            // case-insensitive compare
if (Math.Abs(candidate.Age - recipient.Age) <= 2) score += 20;
score += CryptoRandom.GetInt32(0, 101);                       // cryptographic RNG, range [0, 100]
```

Select the candidate with the **highest score**. In case of a tie, pick any.

### Cryptographic Random (`CryptoRandom`)

> **Deviation from the spec:** the original requirement names `RNGCryptoServiceProvider`,
> but that type is obsolete (SYSLIB0023) since .NET 6. We use the supported cryptographic
> RNG `System.Security.Cryptography.RandomNumberGenerator.GetInt32` instead. This still
> satisfies the intent: a cryptographic RNG, **never** `System.Random`.

- **Always** use the `CryptoRandom` helper (backed by `RandomNumberGenerator`) for the random factor.
- Do **not** use `System.Random` anywhere in the scoring logic.
- The helper (`src/ChaoticCupid.Server/Helpers/CryptoRandom.cs`):

```csharp
public static int GetInt32(int minInclusive, int maxExclusive)
    => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
```

### Letter Delivery & Console Output

When a letter arrives, randomly select one of three messages:

```
"I look forward to our meeting!"
"I would like to get to know you."
"I am not interested in getting acquainted."
```

- For messages 1 and 2: display all sender details (username, city, age, **phone number**).
- For message 3: display all sender details **except phone number**.
- After displaying the letter, **block console input for new letters** until the user presses Enter to acknowledge.

### Blocking

- Parse the console input continuously for the `/block <username>` command (case-insensitive username matching is acceptable).
- Add the given username to the recipient's in-memory block list immediately.
- This check must happen **before** a letter is delivered (filter at dispatch time, not display time).

---

## What NOT to Do

- Do **not** use `System.Random` for the scoring random factor — it must be a cryptographic RNG (`RandomNumberGenerator` via the `CryptoRandom` helper).
- Do **not** allow a second letter to arrive before the first is acknowledged.
- Do **not** expose phone numbers when the "not interested" message is selected.
- Do **not** let a person receive a letter from themselves.
- Do **not** mix Cupid business logic into the `PersonInfo` model — keep models as plain data objects.
- Do **not** hardcode the timer interval as a magic number; define it as a constant (e.g., `private const int LetterIntervalSeconds = 60`).

---

## File / Project Structure (as implemented)

```
ChaoticCupid.slnx
/src
  /ChaoticCupid.Shared
    PersonInfo.cs           # Data model (plain DTO)
    LoveLetter.cs           # Letter payload model (nullable SenderPhone)
    ILetterClient.cs        # Client callback contract (ReceiveLetter)
  /ChaoticCupid.Server
    Program.cs              # DI wiring, maps hub at /cupid
    /Contracts
      IPersonService.cs     # Person-facing interface (InitSinglePerson, AcknowledgeLetter, BlockUser)
      ICupidService.cs      # Cupid-facing interface (DispatchLetters)
    /Hubs
      CupidHub.cs           # IPersonService implementation (SignalR hub)
    /Services
      CupidService.cs       # BackgroundService: scoring + dispatch logic
    /State
      PersonRegistry.cs     # Thread-safe store (singleton)
      RegisteredPerson.cs   # Per-person state (connection, pending-ack, block list)
    /Helpers
      CryptoRandom.cs       # RandomNumberGenerator wrapper
  /ChaoticCupid.Client
    Program.cs              # Console entry point, registration, command loop
```

Keep concerns separated if the structure changes.

---

## Testing Checklist (verify before marking a feature done)

- [ ] Registration rejects empty input, non-numeric characters, and negative numbers.
- [ ] Cupid timer fires every 60 seconds (`PeriodicTimer`, `LetterIntervalSeconds`).
- [ ] No person receives a letter from themselves.
- [ ] Scoring uses a cryptographic RNG (`RandomNumberGenerator` via `CryptoRandom`), not `System.Random`.
- [ ] Same-city bonus (+30) and similar-age bonus (+20) are applied correctly.
- [ ] Highest-scoring candidate is selected for letter dispatch.
- [ ] Phone number is hidden when "not interested" message is selected.
- [ ] Second letter does not appear until the user acknowledges the first.
- [ ] `/block <username>` command removes that sender from future deliveries.
