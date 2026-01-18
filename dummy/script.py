#!/usr/bin/env python3
"""
WebSocket Testing Client for JarvisNet Satellite Protocol
Implements the communication protocols defined in main.cpp
"""

import json
import asyncio
import sys
from datetime import datetime
import websockets

# Protocol constants (match main.cpp)
PROTOCOL_VERSION = 1
SAMPLE_RATE = 16000
BUFFER_MS = 20


class SatelliteTestClient:
    def __init__(self, host: str = "localhost", port: int = 5264, path: str = "/ws/satellite"):
        self.host = host
        self.port = port
        self.path = path
        self.ws = None
        self.connected = False
        self.session_id = None
        self.current_state = "DISCONNECTED"

    async def connect(self):
        """Connect to the WebSocket server"""
        uri = f"ws://{self.host}:{self.port}{self.path}"
        try:
            self.ws = await websockets.connect(uri)
            self.connected = True
            self.current_state = "CONNECTED"
            print(f"✓ Connected to {uri}")

            # Start receiving messages
            asyncio.create_task(self._receive_loop())
        except Exception as e:
            print(f"✗ Connection failed: {e}")
            self.connected = False

    async def disconnect(self):
        """Disconnect from the WebSocket server"""
        if self.ws:
            await self.ws.close()
            self.connected = False
            self.current_state = "DISCONNECTED"
            print("✓ Disconnected")

    async def _receive_loop(self):
        """Listen for incoming messages"""
        try:
            async for message in self.ws:
                if isinstance(message, str):
                    await self._handle_text_message(message)
                elif isinstance(message, bytes):
                    await self._handle_binary_message(message)
        except websockets.exceptions.ConnectionClosed:
            self.connected = False
            self.current_state = "DISCONNECTED"
            print("✗ Connection closed")

    async def _handle_text_message(self, message: str):
        """Handle incoming text (JSON) messages"""
        try:
            doc = json.loads(message)
            msg_type = doc.get("type")

            print(f"\n[RECV] {msg_type}")
            print(f"       {json.dumps(doc, indent=2)}")

            if msg_type == "hello.ack":
                print("       → Server acknowledged hello")
                self.current_state = "READY"
            elif msg_type == "session.ack":
                self.session_id = doc.get("session_id")
                print(f"       → Session acknowledged: {self.session_id}")
                self.current_state = "STREAMING_AUDIO"
            elif msg_type == "tts.start":
                print("       → TTS started, ready to receive audio")
                self.current_state = "PLAYING_TTS"
            elif msg_type == "tts.end":
                print("       → TTS ended")
                self.current_state = "READY"
            else:
                print(f"       → Unknown message type: {msg_type}")

        except json.JSONDecodeError as e:
            print(f"✗ Failed to decode JSON: {e}")

    async def _handle_binary_message(self, data: bytes):
        """Handle incoming binary messages (audio data)"""
        print(f"[RECV] Binary audio data: {len(data)} bytes")

    async def send_hello(self, device_id: str = "test-satellite-001",
                         area: str = "office", language: str = "en"):
        """Send hello message"""
        if not self.connected:
            print("✗ Not connected")
            return

        message = {
            "type": "hello",
            "protocol_version": PROTOCOL_VERSION,
            "satellite_id": device_id,
            "area": area,
            "language": language,
            "capabilities": {
                "speaker": True,
                "display": False,
                "supports_barge_in": True,
                "supports_streaming_tts": True
            },
            "audio_format": {
                "encoding": "pcm_s16le",
                "sample_rate": SAMPLE_RATE,
                "channels": 1,
                "frame_ms": BUFFER_MS
            }
        }

        await self._send_text(message)

    async def send_session_start(self):
        """Send session.start message"""
        if not self.connected:
            print("✗ Not connected")
            return

        message = {
            "type": "session.start",
            "timestamp": int(datetime.now().timestamp() * 1000),
            "session_id": f"session-{datetime.now().timestamp()}"
        }

        self.session_id = message["session_id"]
        await self._send_text(message)

    async def send_audio_end(self, reason: str = "button_release"):
        """Send audio.end message"""
        if not self.connected or not self.session_id:
            print("✗ Not connected or no active session")
            return

        message = {
            "type": "audio.end",
            "session_id": self.session_id,
            "reason": reason
        }

        await self._send_text(message)

    async def send_audio_chunk(self, data: bytes):
        """Send raw audio data"""
        if not self.connected:
            print("✗ Not connected")
            return

        await self.ws.send(data)
        print(f"[SEND] Audio data: {len(data)} bytes")

    async def send_audio_file(self, filepath: str):
        """Send audio file as chunks"""
        try:
            with open(filepath, 'rb') as f:
                chunk_size = 4096
                while True:
                    chunk = f.read(chunk_size)
                    if not chunk:
                        break
                    await self.send_audio_chunk(chunk)
                    await asyncio.sleep(0.01)  # Small delay between chunks
        except FileNotFoundError:
            print(f"✗ File not found: {filepath}")

    async def _send_text(self, message: dict):
        """Send text message"""
        try:
            json_str = json.dumps(message)
            await self.ws.send(json_str)
            print(f"[SEND] {message.get('type')}")
            print(f"       {json.dumps(message, indent=2)}")
        except Exception as e:
            print(f"✗ Failed to send message: {e}")

    def print_status(self):
        """Print current connection status"""
        print(f"\nStatus: {self.current_state}")
        print(f"Connected: {'Yes' if self.connected else 'No'}")
        if self.session_id:
            print(f"Session ID: {self.session_id}")
        print()

    async def interactive_menu(self):
        """Interactive testing menu"""
        print("\n" + "=" * 60)
        print("JarvisNet Satellite WebSocket Test Client")
        print("=" * 60)

        while True:
            self.print_status()
            print("Commands:")
            print("  1 - Connect to server")
            print("  2 - Send hello message")
            print("  3 - Start session")
            print("  4 - Send audio file")
            print("  5 - End session")
            print("  6 - Disconnect")
            print("  q - Quit")
            print()

            choice = input("Enter command: ").strip().lower()

            if choice == '1':
                host = input("Host [localhost]: ").strip() or "localhost"
                port = input("Port [5264]: ").strip() or "5264"
                path = input("Path [/ws/satellite]: ").strip() or "/ws/satellite"
                self.host = host
                self.port = int(port)
                self.path = path
                await self.connect()

            elif choice == '2':
                device_id = input("Device ID [test-satellite-001]: ").strip() or "test-satellite-001"
                area = input("Area [office]: ").strip() or "office"
                language = input("Language [en]: ").strip() or "en"
                await self.send_hello(device_id, area, language)

            elif choice == '3':
                await self.send_session_start()

            elif choice == '4':
                filepath = input("Audio file path: ").strip()
                if filepath:
                    await self.send_audio_file(filepath)

            elif choice == '5':
                reason = input("End reason [button_release]: ").strip() or "button_release"
                await self.send_audio_end(reason)

            elif choice == '6':
                await self.disconnect()

            elif choice == 'q':
                if self.connected:
                    await self.disconnect()
                print("Goodbye!")
                break

            else:
                print("✗ Invalid command")

            await asyncio.sleep(0.1)


async def main():
    client = SatelliteTestClient()

    # Check if running in interactive or automated mode
    if len(sys.argv) > 1:
        # Automated test mode
        host = sys.argv[1] if len(sys.argv) > 1 else "localhost"
        port = int(sys.argv[2]) if len(sys.argv) > 2 else 8765

        await client.connect()
        if client.connected:
            await client.send_hello()
            await asyncio.sleep(1)
            await client.send_session_start()
            await asyncio.sleep(2)
            await client.send_audio_end()
            await asyncio.sleep(1)
            await client.disconnect()
    else:
        # Interactive mode
        try:
            await client.interactive_menu()
        except KeyboardInterrupt:
            print("\n\nInterrupted by user")
            if client.connected:
                await client.disconnect()


if __name__ == "__main__":
    asyncio.run(main())
