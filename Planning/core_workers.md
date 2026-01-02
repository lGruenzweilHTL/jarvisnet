# Python Workers Communication Protocol & Pipeline Reference

This document is a **comprehensive reference** for the Python worker layer in the AI voice assistant system. It covers **STT, Intent/Router, LLM, and TTS workers**, the HTTP protocol, and how they interact with the **C# core orchestrator**.

It is designed for future reference and implementation guidance.

---

## 1.  Design Principles for Python Workers

* **Stateless:** no session memory
* **Single-purpose:** one task per worker (STT, TTS, LLM, Router)
* **Deterministic:** same input always yields same output
* **HTTP only:** no WebSockets
* **JSON for metadata, base64 for binary audio**
* **No orchestration knowledge:** core handles sessions, tools, and satellites

### What workers do not know

* Satellites or WebSockets
* C# core logic beyond input/output
* Tool execution (LLMs request tools, C# executes)
* Dashboard or event logging

---

## 2. Common Worker HTTP Protocol

**Endpoint:**

```
POST /infer
```

### Common Request Envelope

```json
{
  "request_id": "uuid",
  "input": { ... },
  "config": { ... },
  "context": { ... }
}
```

### Common Response Envelope

```json
{
  "request_id": "uuid",
  "output": { ... },
  "usage": {
    "latency_ms": 123,
    "model": "model_name"
  },
  "error": null
}
```

**Purpose:** uniform API across all worker types for logging, retries, and easy orchestration.

---

## 3. STT Worker (Speech → Text)

### Request Example

```json
{
  "request_id": "uuid",
  "input": {
    "audio": {
      "encoding": "pcm_s16le",
      "sample_rate": 16000,
      "channels": 1,
      "data_base64": "..."
    }
  },
  "config": {
    "language": "en-US",
    "vad": false,
    "return_segments": false
  },
  "context": {
    "mic_id": "kitchen_satellite",
    "area": "kitchen"
  }
}
```

### Response Example

```json
{
  "request_id": "uuid",
  "output": {
    "text": "turn on the kitchen lights",
    "confidence": 0.92
  },
  "usage": {
    "latency_ms": 420,
    "model": "whisper-large-v3"
  },
  "error": null
}
```

**Notes:** Audio is base64-encoded PCM; core buffers and forwards audio.

---

## 4. Intent / Specialty Router Worker (Text → Specialty)

### Request Example

```json
{
  "request_id": "uuid",
  "input": {
    "text": "turn on the kitchen lights"
  },
  "config": {
    "allowed_specialties": ["home_control","general_chat","coding","deep_research"]
  },
  "context": {
    "area": "kitchen",
    "time": "2026-01-02T19:30:00Z"
  }
}
```

### Response Example

```json
{
  "request_id": "uuid",
  "output": {
    "specialty": "home_control",
    "confidence": 0.95,
    "notes": "Direct home automation command"
  },
  "usage": {
    "latency_ms": 35,
    "model": "llama-3.2-3b"
  },
  "error": null
}
```

**Rules:**

* Router does not generate user-facing text
* Router does not call tools
* Determines which specialized LLM worker to invoke

---

## 5. LLM Worker (Specialized Reasoning)

### Request Example

```json
{
  "request_id": "uuid",
  "input": {
    "messages": [
      {"role": "system", "content": "You are a home assistant controller."},
      {"role": "user", "content": "turn on the kitchen lights"}
    ],
    "tools": [
      {"name": "homeassistant.call_service", "description": "...", "parameters": { ... }}
    ]
  },
  "config": {
    "max_tokens": 256,
    "temperature": 0.2
  },
  "context": {
    "area": "kitchen"
  }
}
```

### Response Example (Tool Call)

```json
{
  "request_id": "uuid",
  "output": {
    "type": "tool_call",
    "tool": "homeassistant.call_service",
    "arguments": {
      "domain": "light",
      "service": "turn_on",
      "entity_id": "light.kitchen"
    }
  },
  "usage": {
    "latency_ms": 210,
    "model": "llama-3.1-8b"
  },
  "error": null
}
```

Or final text response:

```json
{
  "output": {
    "type": "final",
    "text": "Okay, turning on the kitchen lights."
  }
}
```

**Rules:** LLM only requests tool calls; core executes them.

---

## 6. TTS Worker (Text → Audio)

### Request Example

```json
{
  "request_id": "uuid",
  "input": {
    "text": "Okay, turning on the kitchen lights."
  },
  "config": {
    "voice": "en_US_female",
    "speed": 1.0,
    "streaming": false
  },
  "context": {
    "language": "en-US"
  }
}
```

### Response Example

```json
{
  "request_id": "uuid",
  "output": {
    "audio": {
      "encoding": "pcm_s16le",
      "sample_rate": 22050,
      "channels": 1,
      "data_base64": "..."
    }
  },
  "usage": {
    "latency_ms": 180,
    "model": "piper"
  },
  "error": null
}
```

**Notes:** Can later support `streaming=true` without breaking API.

---

## 7. End-to-End Worker Pipeline

1. **C# Core receives audio** → buffers → sends `/infer` to STT worker
2. STT returns text → core sends `/infer` to Router worker
3. Router returns specialty → core invokes appropriate LLM worker `/infer`
4. LLM requests tool calls → core executes tools → optionally sends final text to LLM
5. Core sends text `/infer` to TTS worker → returns audio → core streams to satellite

**This completes one interaction end-to-end.**

---

## 8. Scalability & Extensibility

* Workers are independent processes → can be on separate machines
* Workers can be restarted without breaking sessions
* Can swap models per specialty easily
* Streaming or partial results can be added later without changing API
* Uniform request/response envelopes simplify logging, monitoring, and retries

---

## 9. Summary

* `/infer` uniform interface for all workers
* JSON metadata, base64 audio (STT/TTS)
* Stateless, single-purpose workers
* Core orchestrates sessions, routing, and tool execution
* End-to-end pipeline: Audio → STT → Router → LLM → Tool → Final text → TTS → Audio
* Scalable, testable, and future-proof design
