# AI Voice Assistant – System Architecture Reference

This document is a **concise but complete reference** for the custom AI voice assistant architecture we discussed. It is meant to be skimmable, yet detailed enough to understand the system from scratch.

---

## 1. High-Level Goal

Build a **local-first, extensible AI voice assistant platform** that:

* Supports multiple microphones (“satellites”)
* Integrates deeply with Home Assistant
* Supports non-HA tools (coding, research, APIs, etc.)
* Uses agent-style *specialization* without GPU waste
* Is observable, debuggable, and maintainable long-term

---

## 2. Core Design Principles

* **Central orchestration** (one brain)
* **Stateless, specialized workers**
* **Hard process boundaries** between subsystems
* **Tool-based interaction**, not free-form control
* **Constrained contexts** for reliability and performance

---

## 3. System Overview

```
[SATELLITES / MICS]
        ↓ (WebSocket)
[C# CORE / ORCHESTRATOR]
        ↓ (HTTP)
[PYTHON WORKERS]
        ↓
[MODELS / ML]

[C# CORE] → [EVENT STORE] → [DASHBOARD]
```

---

## 4. Satellites (Microphone Devices)

**Role:** Voice terminals only. No intelligence.

### Responsibilities

* Wake word detection (local)
* Audio capture (PCM)
* Audio streaming
* Audio playback (TTS)
* Identify themselves (mic_id, area)

### Key Properties Sent with Each Request

* `mic_id`
* `area`
* `language`
* `capabilities` (speaker, display, etc.)

### Communication

* **WebSocket → C# Core**
* Persistent connection
* Event + audio frame streaming

---

## 5. C# Core (Central Coordinator)

**Language:** C# (.NET / ASP.NET Core)

**This is the brain of the system.**

### Responsibilities

* Manage satellite connections
* Maintain voice sessions
* Buffer / stream audio
* Invoke STT / TTS / LLM workers
* Route requests to specialized assistants
* Register and execute tools (via reflection)
* Integrate with Home Assistant
* Emit structured events
* Expose APIs for dashboard

### What It Does *Not* Do

* No ML inference
* No heavy audio DSP
* No UI logic

### Internal Structure (Conceptual)

* Voice Gateway
* Session Manager
* Intent / Specialty Router
* Tool Registry & Executor
* Python Worker Clients
* Event Bus & Event Store

---

## 6. Python Workers (AI & Audio)

**Language:** Python
**Framework:** FastAPI (recommended)

Workers are:

* Stateless
* Single-purpose
* Replaceable

### Example Workers

* STT Service (Whisper)
* TTS Service
* Router Model (small LLM)
* Home Control LLM
* Research / Coding LLM

### API Shape

```
POST /infer
```

* JSON in, JSON out
* No memory
* No orchestration logic

### Why FastAPI

* Async-first
* Strong schemas (Pydantic)
* Auto OpenAPI
* Cleaner than Flask for services

---

## 7. Agentic / Specialized Assistant Model

### Key Idea

Not multiple chatting agents — **routing + constrained contexts**.

### Flow

1. User speech → STT
2. **Router model (small)** classifies intent
3. Select specialty profile
4. Run **one specialized assistant** with:

   * Limited tools
   * Tight system prompt
   * Appropriate model size

### Example Specialties

* `home_control`
* `general_chat`
* `coding`
* `deep_research`

### Benefits

* Lower GPU usage
* Fewer hallucinations
* Simpler prompts
* Easier debugging

---

## 8. Tool System (C# Reflection-Based)

### Tools Are

* Strongly typed C# methods
* Discovered via reflection
* Annotated with metadata

### Example

```csharp
[Tool("homeassistant.call_service")]
Task CallService(string domain, string service, string entityId, object data)
```

### Tool System Handles

* Schema generation
* Permissioning per specialty
* Validation
* Execution

LLMs never call raw APIs — only tools.

---

## 9. Home Assistant Integration

* Implemented as a **tool provider** in C#
* Uses HA REST / WebSocket APIs
* Exposes:

  * Entity state
  * Service calls
  * Areas

HA is *not* the brain — just another capability.

---

## 10. Event & Logging Architecture

### Structured Events (not raw logs)

Examples:

* Voice request received
* STT completed
* Router decision
* Tool invoked
* Assistant response
* Errors

### Storage

* SQLite initially
* Postgres later if needed

---

## 11. Dashboard

**Recommended:** Blazor Server

### Why Blazor

* Same language as backend
* Shared DTOs
* SignalR for live updates
* Great for observability UIs

### Dashboard Is

* Read-only
* API-driven
* Displays events, sessions, metrics

Dashboard never touches core logic directly.

---

## 12. Communication Summary

| From      | To             | Protocol       |
| --------- | -------------- | -------------- |
| Satellite | C# Core        | WebSocket      |
| C# Core   | Python Workers | HTTP (FastAPI) |
| C# Core   | Dashboard      | REST + SignalR |

---

## 13. Migration Strategy

1. Keep existing Python pipeline
2. Build C# core alongside it
3. Wrap Python logic behind FastAPI
4. Move orchestration & tools to C#
5. Replace Flask incrementally
6. Add dashboard last

---

## 14. Mental Model (TL;DR)

* **C# Core** = Conductor
* **Python Workers** = Specialists
* **Satellites** = Dumb terminals
* **Tools** = Only way to act
* **Router** = Keeps system efficient
* **Dashboard** = Observability, not control

---

This architecture is designed to scale in **features, reliability, and debuggability** without collapsing into tightly coupled spaghetti.
