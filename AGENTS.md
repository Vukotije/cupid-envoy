# AGENTS.md — Cursor AI Instructions for Chaotic Cupid

This file provides binding instructions for Cursor (and any AI coding agent) working in this repository.
Read `SPECIFICATION.md` first for the full feature requirements before making any changes.

---

## Project Context

This is a **.NET PubSub application** (WCF or ASP.NET Core) implementing a matchmaking simulation.
It has two distinct interfaces (`IPersonService`, `ICupidService`) and a console client for user interaction.

---

## Architecture Rules

- **Two service interfaces** must remain separate: `IPersonService` and `ICupidService`.
- Use a **PubSub pattern**: Cupid publishes letters; registered persons are subscribers.
- All **shared state** (registered persons, block lists, pending acknowledgment flags) must be managed in a **thread-safe** manner (use `ConcurrentDictionary`, locks, or similar).
- The scoring and letter-dispatch logic belongs exclusively in the **Cupid service**, not in person-side code.

---

## Naming Conventions

| Concept | Name |
|---------|------|
| Person registration method | `InitSinglePerson` |
| Person data model | `PersonInfo` (or `Person`) |
| Cupid scheduling logic | `CupidService` |
| Letter dispatch method | `SendLetter` (or `DispatchLetters`) |
| Block list per person | `BlockedUsers` (e.g., `HashSet<string>` per username) |

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

- Use a `System.Threading.Timer` or `Task.Delay` loop that fires **every 60 seconds**.
- On each tick, iterate all registered persons and dispatch one letter per person.
- Skip self-to-self letters (sender == recipient).
- Skip letters to recipients who have a **pending unacknowledged letter**.
- Skip letters from senders on the recipient's **block list**.

### Scoring Algorithm

Compute a score for each candidate sender against a recipient:

```csharp
int score = 0;
if (candidate.City == recipient.City) score += 30;
if (Math.Abs(candidate.Age - recipient.Age) <= 2) score += 20;
score += GetCryptoRandom(0, 101); // RNGCryptoServiceProvider, range [0, 100]
```

Select the candidate with the **highest score**. In case of a tie, pick any.

### Cryptographic Random (`RNGCryptoServiceProvider`)

- **Always** use `System.Security.Cryptography.RNGCryptoServiceProvider` for the random factor.
- Do **not** use `System.Random` anywhere in the scoring logic.
- Encapsulate this in a helper method, e.g.:

```csharp
private static int GetCryptoRandom(int minInclusive, int maxExclusive)
{
    using var rng = new RNGCryptoServiceProvider();
    byte[] bytes = new byte[4];
    rng.GetBytes(bytes);
    int value = Math.Abs(BitConverter.ToInt32(bytes, 0));
    return minInclusive + (value % (maxExclusive - minInclusive));
}
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

- Do **not** use `System.Random` for the scoring random factor — it must be `RNGCryptoServiceProvider`.
- Do **not** allow a second letter to arrive before the first is acknowledged.
- Do **not** expose phone numbers when the "not interested" message is selected.
- Do **not** let a person receive a letter from themselves.
- Do **not** mix Cupid business logic into the `PersonInfo` model — keep models as plain data objects.
- Do **not** hardcode the timer interval as a magic number; define it as a constant (e.g., `private const int LetterIntervalSeconds = 60`).

---

## File / Project Structure (Suggested)

```
/src
  /Contracts
    IPersonService.cs       # Person-facing interface
    ICupidService.cs        # Cupid-facing interface
    PersonInfo.cs           # Data model
    LoveLetter.cs           # Letter payload model
  /Services
    PersonService.cs        # IPersonService implementation
    CupidService.cs         # Scoring + dispatch logic
  /Helpers
    CryptoRandom.cs         # RNGCryptoServiceProvider wrapper
  /Client
    Program.cs              # Console entry point, registration, command loop
```

Adjust structure if the framework imposes a different layout, but keep concerns separated.

---

## Testing Checklist (verify before marking a feature done)

- [ ] Registration rejects empty input, non-numeric characters, and negative numbers.
- [ ] Cupid timer fires every 60 seconds.
- [ ] No person receives a letter from themselves.
- [ ] Scoring uses `RNGCryptoServiceProvider` (not `System.Random`).
- [ ] Same-city bonus (+30) and similar-age bonus (+20) are applied correctly.
- [ ] Highest-scoring candidate is selected for letter dispatch.
- [ ] Phone number is hidden when "not interested" message is selected.
- [ ] Second letter does not appear until the user acknowledges the first.
- [ ] `/block <username>` command removes that sender from future deliveries.
