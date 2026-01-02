# Dashboard API & Observability Reference

This document defines the **dashboard-facing HTTP API** exposed by the **C# core**. It is intended as a **future reference** describing how logs, sessions, satellites, workers, and system health are surfaced for visualization and debugging.

The dashboard is **read-only by default**, with optional control endpoints for development and administration.

---

## 1. Dashboard Design Goals

* Provide **visibility**, not orchestration
* Reflect the internal state of the system without coupling
* Enable debugging of voice sessions end-to-end
* Allow historical inspection (logs, metrics)
* Be simple enough to back with in-memory storage initially

The dashboard does **not**:

* Control satellites directly
* Call Python workers
* Execute tools
* Participate in real-time voice flows

---

## 2. High-Level Architecture

```
[ Satellites ]
      ↓ (WebSocket)
[ C# Core ] ── exposes HTTP API ──> [ Dashboard (Blazor) ]
      ↓
[ Python Workers (HTTP) ]
```

* Dashboard communicates **only with the C# core**
* Core acts as the **single source of truth**

---

## 3. Core Concepts Exposed to Dashboard

### Entities

* **Satellite** – a physical or logical mic/speaker device
* **Voice Session** – one user interaction lifecycle
* **Worker Call** – one `/infer` request to a Python worker
* **Tool Call** – one tool execution requested by an LLM
* **Event** – timestamped system occurrence

---

## 4. Satellite Endpoints

### `GET /api/satellites`

Returns all known satellites.

```json
[
  {
    "satellite_id": "kitchen_satellite",
    "status": "connected",
    "capabilities": {
      "mic": true,
      "speaker": true,
      "wakeword": true
    },
    "last_seen": "2026-01-02T19:30:10Z"
  }
]
```

---

### `GET /api/satellites/{id}`

Detailed satellite information.

---

## 5. Voice Session Endpoints

### `GET /api/sessions`

List recent voice sessions.

```json
[
  {
    "session_id": "uuid",
    "satellite_id": "kitchen_satellite",
    "state": "completed",
    "started_at": "2026-01-02T19:29:58Z",
    "ended_at": "2026-01-02T19:30:02Z",
    "final_intent": "home_control"
  }
]
```

---

### `GET /api/sessions/{id}`

Full session timeline.

```json
{
  "session_id": "uuid",
  "states": [
    {"state": "listening", "timestamp": "..."},
    {"state": "processing", "timestamp": "..."},
    {"state": "responding", "timestamp": "..."}
  ],
  "transcript": "turn on the kitchen lights",
  "final_response": "Okay, turning on the kitchen lights."
}
```

---

## 6. Worker Call Endpoints

### `GET /api/workers/calls`

List recent worker invocations.

```json
[
  {
    "request_id": "uuid",
    "worker_type": "stt",
    "model": "whisper-large-v3",
    "latency_ms": 420,
    "session_id": "uuid",
    "timestamp": "..."
  }
]
```

---

### `GET /api/workers/calls/{request_id}`

Full request/response payload (sanitized).

---

## 7. Tool Execution Endpoints

### `GET /api/tools/calls`

```json
[
  {
    "tool": "homeassistant.call_service",
    "status": "success",
    "session_id": "uuid",
    "latency_ms": 80
  }
]
```

---

## 8. Event & Log Endpoints

### `GET /api/events`

Chronological system events.

```json
[
  {
    "type": "session_started",
    "session_id": "uuid",
    "timestamp": "..."
  },
  {
    "type": "worker_error",
    "worker": "stt",
    "message": "timeout"
  }
]
```

---

### `GET /api/logs`

Filtered logs.

**Query parameters:**

* `level=info|warn|error`
* `session_id=uuid`
* `since=timestamp`

---

## 9. System Health Endpoints

### `GET /api/health`

Overall system health.

```json
{
  "core": "ok",
  "satellites_connected": 3,
  "workers": {
    "stt": "ok",
    "router": "ok",
    "llm": "degraded",
    "tts": "ok"
  }
}
```

---

### `GET /api/metrics`

High-level performance metrics.

```json
{
  "avg_session_latency_ms": 980,
  "avg_stt_latency_ms": 410,
  "avg_llm_latency_ms": 220
}
```

---

## 10. Optional Control Endpoints (Dev/Admin)

> These are **optional** and should be disabled or protected in production.

### `POST /api/admin/sessions/{id}/abort`

Abort an active session.

### `POST /api/admin/workers/{type}/reload`

Reload worker configuration.

---

## 11. Dashboard UI Mapping (Blazor)

Suggested views:

* **Overview:** health, connected satellites
* **Live sessions:** active voice interactions
* **Session detail:** timeline, transcript, tool calls
* **Workers:** latency charts, error rates
* **Logs:** filterable event stream

Blazor is a good fit because:

* Strong typing
* Shared DTOs with core
* Real-time updates via SignalR (optional)

---

## 12. Summary

* Dashboard talks only to C# core
* REST-style, read-only by default
* Exposes satellites, sessions, workers, tools, logs, and health
* Complements (but does not interfere with) the voice pipeline
* Designed for observability, debugging, and future metrics

This completes the **full system reference**: satellites, core, workers, and dashboard.
