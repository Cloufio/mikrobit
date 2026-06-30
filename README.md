# mikrobit

## Requirements

| Tool / Technology | Version | Notes |
| :--- | :--- | :--- |
| **Unity Editor** | 6000.0.39f1 | Required to compile and run the project |
| **Operating System** | Windows 10 / 11 | |
| **Git** | 2.x+ | Standard Git CLI or GitHub Desktop |

## Getting Started

### 1. Clone the Repository

**Option A: Using Git Command Line**
Open your terminal (Command Prompt, PowerShell, or Git Bash) and execute the following commands:

    git clone <repo-url>
    cd mikrobit

**Option B: Using GitHub Desktop**
1. Open GitHub Desktop.
2. Navigate to File > Clone repository...
3. Select the URL tab and paste the repository URL.
4. Specify your local path and click Clone.

### 2. Open in Unity Hub

The project must be initialized through Unity Hub to generate local library and configuration files.

1. Open Unity Hub.
2. Navigate to the Projects tab and click Add (or Open > Add project from disk).
3. Select the cloned mikrobit directory.
4. Click on the project name in the list to launch the Unity Editor.

*Note: Because this project was initially developed on macOS (Metal API), Unity will automatically recompile shaders for Windows (DirectX) upon the first launch. This initialization process is expected and may take several minutes.*

### 3. Run the Project

Once the Unity Editor has fully loaded:

1. In the Project window, navigate to Assets/Scenes.
2. Double-click the primary scene file to load the environment into the Hierarchy.
3. Press the Play button located at the top-center of the Editor interface to start the application.