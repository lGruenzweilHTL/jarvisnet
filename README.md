# Jarvisnet

Jarvisnet is a self-cloning network of preconfigured AI agents designed to assist with various tasks. Each agent is capable of performing specific functions and can replicate itself to create new agents as needed, which greatly enhances asynchronous task management and execution.

## Preset system

Jarvisnet comes with a set of predefined AI agents, each tailored for specific roles. These presets can be used as-is or modified to suit particular requirements.

### Available Presets

- **Master**: The central coordinator that manages other agents and oversees task distribution. It is integrated into a voice pipeline with wake word detection for hands-free operation.
- **Worker**: A general-purpose agent that can handle a variety of tasks assigned by the Master agent.
- **Researcher**: An agent specialized in gathering information, conducting research, and compiling data.
- **Writer**: An agent focused on content creation, including writing articles, reports and other textual materials. It is equipped with proofreading and editing capabilities as well as grammar checking.
- **Coder**: An agent dedicated to programming tasks, capable of writing, debugging, and optimizing code in various programming languages.

## Getting Started

To get started with Jarvisnet, follow these steps:
1. Clone the repository:
   ```bash
   git clone https://github.com/lGruenzweilHTL/jarvisnet.git
   ```
2. Navigate to the project directory:
3. Install the required dependencies:
   ```bash
   pip install -r requirements.txt
   ```
4. Download a model for openwakeword and piper-tts and place it in the `onnx/` directory.
5. Edit these lines in `main.py` to point to your wake word model:
   ```python
   WAKE_MODEL = "onnx/hey_jarvis_v0.1.onnx"
   VOICE = "onnx/en_US-lessac-low.onnx"
   ```
6. Run the main application:
   ```bash
    python main.py
    ```
   First execution may take some time as it sets up the agents and downloads necessary models.

## Usage

Once the application is running, the Master agent will listen for the wake word. Upon activation, it can delegate tasks to the appropriate agents based on the user's requests.

