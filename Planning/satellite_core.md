# Satellite ↔ Core WebSocket Protocol & State Machines

This document is a **concise but comprehensive reference** for the WebSocket communication between **satellite devices (microphones)** and the **C# core**. It defines message flow, responsibilities, and the **state machines on both sides**.

It assumes familiarity with the overall system architecture.

---

## 1. Design Goals

* Explicit, debuggable interaction lifecycle
* One interaction = one **voice session**
* Simple satellite implementation
* No hidden or implicit state
* WebSocket-native (JSON + binary frames)

---

## 2. Core Concepts

### Voice Session

A **voice session** represents a single user interaction:

> Wake word / button → speech → assistant response → playback

A session:

* Has a unique `session_id`
* Owns all audio, STT, reasoning, and TTS
* Is the unit of logging, metrics, and debugging

Only **one active session per satellite connection** is allowed (v1).

---

## 3. Message Framing Rules

### WebSocket Frame Types

| Frame type | Purpose                     |
|------------|-----------------------------|
| Text       | UTF-8 JSON control & events |
| Binary     | Raw PCM audio frames        |

### Key Rules

* JSON frames = control/events only
* Binary frames = audio only
* No audio headers in binary frames
* Audio frames belong to the currently active session

---

## 4. Connection Handshake

### `hello` (Satellite → Core)

Sent immediately after WebSocket connection opens.

```json
{
  "type": "hello",
  "protocol_version": 1,
  "mic_id": "kitchen_satellite",
  "area": "kitchen",
  "language": "en-US",
  "capabilities": {
    "speaker": true,
    "display": false,
    "supports_barge_in": true,
    "supports_streaming_tts": true
  },
  "audio_format": {
    "encoding": "pcm_s16le",
    "sample_rate": 16000,
    "channels": 1,
    "frame_ms": 20
  }
}
```

### `hello.ack` (Core → Satellite)

```json
{
  "type": "hello.ack",
  "protocol_version": 1,
  "accepted": true
}
```

After this, the connection is considered **ready**.

---

## 5. Session Control Messages

### `session.start` (Satellite → Core)

```json
{
  "type": "session.start",
  "session_id": "uuid-v4",
  "timestamp": 123456789
}
```

### `session.ack` (Core → Satellite)

```json
{
  "type": "session.ack",
  "session_id": "uuid-v4"
}
```

Once acknowledged, audio streaming may begin.

---

## 6. Audio Streaming

### Audio Frames (Satellite → Core)

* Sent as **binary WebSocket frames**
* Raw PCM16, mono, 16 kHz (v1 canonical)
* One frame per message

Frames are sent **only while a session is active**.

---

### `audio.end` (Satellite → Core)

Signals end-of-speech.

```json
{
  "type": "audio.end",
  "session_id": "uuid-v4",
  "reason": "silence"
}
```

Reason enum:

* `silence`
* `button_release`
* `timeout`
* `cancel`

---

### `session.abort` (Satellite → Core)

Immediate termination.

```json
{
  "type": "session.abort",
  "session_id": "uuid-v4",
  "reason": "user_cancel"
}
```

Core immediately discards the session.

---

## 7. Core → Satellite Output Messages

### `tts.start`

```json
{
  "type": "tts.start",
  "session_id": "uuid-v4",
  "audio_format": {
    "encoding": "pcm_s16le",
    "sample_rate": 22050,
    "channels": 1
  },
  "streaming": true
}
```

Satellite prepares playback.

---

### TTS Audio Frames (Core → Satellite)

* Binary frames
* Raw PCM
* Streamed or buffered

---

### `tts.end`

```json
{
  "type": "tts.end",
  "session_id": "uuid-v4"
}
```

Satellite returns to idle.

---

### `error`

```json
{
  "type": "error",
  "session_id": "uuid-v4",
  "code": "stt_failed",
  "message": "STT service unavailable"
}
```

---

## 8. Barge-In (Interrupt)

If supported:

```json
{
  "type": "barge_in",
  "session_id": "uuid-v4"
}
```

Core stops TTS immediately.

---

## 9. Satellite State Machine

### States

```
DISCONNECTED
  ↓ connect
CONNECTED
  ↓ hello / hello.ack
READY
  ↓ wake word / button
SESSION_STARTING
  ↓ session.ack
STREAMING_AUDIO
  ↓ audio.end
WAITING_FOR_RESPONSE
  ↓ tts.start
PLAYING_TTS
  ↓ tts.end
READY
```

### Abort Paths

* Any state → `session.abort` → READY
* Error → READY

### Invariants

* Only one active session
* Audio frames only in STREAMING_AUDIO

---

## 10. Core State Machine (Per Satellite)

### States

```
DISCONNECTED
  ↓ hello
CONNECTED
  ↓ session.start
SESSION_ACTIVE
  ↓ audio.end
PROCESSING
  ↓ tts.start
PLAYBACK
  ↓ tts.end
CONNECTED
```

### Internal Sub-States (SESSION_ACTIVE)

* Buffering audio
* Streaming to STT (optional)

### Abort Paths

* `session.abort` → CONNECTED
* Internal error → error → CONNECTED

---

## 11. State Machine Guarantees

* Illegal transitions are rejected
* Sessions are fully isolated
* Cleanup is deterministic
* Logging aligns perfectly with sessions

---

## 12. Why This Design Works

* Minimal satellite logic
* Centralized intelligence
* Easy debugging & replay
* Extensible without breaking v1
* Matches real-time voice UX expectations

---

## 13. Summary (TL;DR)

* WebSocket with JSON + binary frames
* Explicit voice sessions
* One session per connection
* Canonical audio format (v1)
* Clear state machines on both sides
* Designed for reliability, not cleverness

---

This protocol is intended as a **stable foundation** for all future voice features (partial STT, displays, auth, multi-room, etc.).
