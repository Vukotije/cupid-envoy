# Chaotic Cupid — Project Specification

## Overview

Build a **PubSub application** that simulates a "Chaotic Cupid" matchmaking service.
The system exposes **two interfaces**: one for **persons** (players) and one for **Cupid** (the matchmaking agent).
Implement using either **WCF** or **ASP.NET Core** (your choice).

---

## Interfaces

### Person Interface

Exposes functionality for users to register and receive love letters.

### Cupid Interface

Exposes functionality for the Cupid service to send letters and manage matchmaking logic.

---

## Features

### 1. Player Registration — `InitSinglePerson`

Players register for matchmaking by calling the `InitSinglePerson` method.
Via the console, the user enters:

- **Username**
- **City**
- **Age**
- **Phone number**

**Input validation rules:**
- Display an appropriate error message if the user enters nothing.
- Display an appropriate error message if the user enters non-numeric characters where numbers are expected.
- Display an appropriate error message if the user enters negative numbers.

---

### 2. Cupid Sends Love Letters (Periodic — Every Minute)

- Cupid sends **one love letter to every registered person** once per minute.
- A person **must not receive a letter from themselves**.

---

### 3. Matchmaking Scoring Algorithm

For each registered person, Cupid calculates a score against every other registered person and sends a letter to the **highest-scoring candidate**:

| Condition | Points |
|-----------|--------|
| Same city/location | +30 |
| Similar age (±2 years) | +20 |
| Random factor | +0 to +100 (use `RNGCryptoServiceProvider`) |

> **Note:** The random factor must be generated using `System.Security.Cryptography.RNGCryptoServiceProvider` (not `System.Random`).

---

### 4. Receiving a Love Letter

When a letter arrives, the recipient's console displays the **sender's details** along with a **randomly selected message** from the following three options:

1. `"I look forward to our meeting!"` → Show all details including phone number.
2. `"I would like to get to know you."` → Show all details including phone number.
3. `"I am not interested in getting acquainted."` → **Do NOT display the sender's phone number.**

---

### 5. Acknowledgment Requirement

- A person **cannot receive a new letter** until they **explicitly confirm** (via console input) that they have read the previous letter.
- This prevents message flooding and ensures each letter is acknowledged.

---

### 6. Blocking Users

- A person can block another user using the console command:

```
/block <username>
```

- Once a user is blocked, the recipient will **never receive a letter from that sender** again.
- Blocking is **per-recipient** — each person manages their own block list.

---

## Summary of Console Commands

| Command | Description |
|---------|-------------|
| *(registration prompts)* | Enter username, city, age, phone number |
| *(any key / Enter)* | Acknowledge receipt of a letter |
| `/block <username>` | Block a specific user from sending letters |

---

## Technology Constraints

- Framework: **WCF** or **ASP.NET Core** (PubSub pattern)
- Random number generation: **`RNGCryptoServiceProvider`** (cryptographic RNG, not `System.Random`)
- Communication pattern: **Publish/Subscribe**
