# Google Antigravity for Unity

This package provides a Unity Editor integration for the Google Antigravity IDE.

## Credits

This project is a port of the [Cursor Unity Plugin](https://github.com/boxqkrtm/com.unity.ide.cursor) created by [boxqkrtm](https://github.com/boxqkrtm). We acknowledge and appreciate their original work which made this integration possible.

## Installation

1. Open Unity.
2. Go to **Window** -> **Package Manager**.
3. Click the **+** button in the top left corner.
4. Select **Add package from git URL...**.
5. Enter the git URL of this repository (e.g., `https://github.com/HarineshS/com.unity.ide.antigravity.git`).
6. Click **Add**.

## Supported Editors

- **Antigravity IDE**: Full-featured AI-first IDE built on VS Code with C# bridge and IntelliSense.
- **Antigravity 2.0**: Next-generation agentic desktop development environment for autonomous agent workflows.
- **OpenAI Codex**: Standalone desktop coding environment powered by Codex.

## Features

- **Setup Chooser Window**: Automatically prompts on package import to choose your preferred editor, or open anytime via **Window** -> **Antigravity** -> **Code Editor Chooser** (or **Tools** -> **Antigravity** -> **Code Editor Chooser**).
- **Auto-Discovery**: Automatically scans standard install paths across Windows, macOS, and Linux for Antigravity IDE, Antigravity 2.0, and Codex.
- **Custom Executables**: Easily browse and configure custom executable locations.
- **Project & Solution Generation**: Automatically generates `.csproj` and `.sln` files tailored for Unity C# scripting and debugging.
- **One-Click Activation**: Instantly applies Unity's external script editor preference and regenerates project files.
