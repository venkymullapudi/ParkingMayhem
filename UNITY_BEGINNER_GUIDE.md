# Unity Development Guide for Beginners

## Table of Contents
1. [Getting Started with Unity](#getting-started-with-unity)
2. [Understanding Unity Basics](#understanding-unity-basics)
3. [Project Structure and Organization](#project-structure-and-organization)
4. [Scripting in Unity](#scripting-in-unity)
5. [Building Your First Game](#building-your-first-game)
6. [Mobile Development Setup](#mobile-development-setup)
7. [Publishing Your Game](#publishing-your-game)
8. [Troubleshooting Common Issues](#troubleshooting-common-issues)

## Getting Started with Unity

### 1. Installation Requirements
- **Minimum System Requirements**:
  - Windows 10/11 or macOS 10.15+
  - 8GB RAM (16GB recommended)
  - 2GB VRAM (4GB recommended)
  - 20GB free disk space

### 2. Installing Unity
1. **Download Unity Hub**
   - Visit [unity.com/download](https://unity.com/download)
   - Download Unity Hub for your operating system
   - Install Unity Hub

2. **Install Unity Editor**
   - Open Unity Hub
   - Go to "Installs" tab
   - Click "Install Editor"
   - Select version 2022.3.0f1 (LTS)
   - Select these modules:
     - Microsoft Visual Studio Community
     - Android Build Support
     - iOS Build Support
     - Documentation

3. **Install Development Tools**
   - **For Android**:
     - Download and install [Android Studio](https://developer.android.com/studio)
     - Install Android SDK through Android Studio
     - Set up environment variables for Android SDK
   - **For iOS**:
     - Install Xcode from Mac App Store
     - Install Command Line Tools
     - Accept Xcode license

### 3. Creating Your First Project
1. Open Unity Hub
2. Click "New Project"
3. Select "2D" template
4. Name your project (e.g., "ParkingRush")
5. Choose project location
6. Click "Create Project"

## Understanding Unity Basics

### 1. Unity Interface
- **Scene View**: Where you build and edit your game
- **Game View**: Shows how your game looks when running
- **Hierarchy**: Lists all objects in your current scene
- **Project Window**: Shows all files in your project
- **Inspector**: Shows properties of selected objects
- **Console**: Shows debug messages and errors

### 2. Basic Concepts
- **GameObjects**: The basic building blocks of your game
- **Components**: Behaviors and properties attached to GameObjects
- **Prefabs**: Reusable GameObject templates
- **Scenes**: Different screens or levels of your game
- **Tags & Layers**: For organizing and identifying objects

### 3. Common Components
- **Transform**: Position, rotation, and scale
- **Sprite Renderer**: For 2D graphics
- **Rigidbody2D**: For physics
- **Collider2D**: For collision detection
- **Script**: Custom behavior

## Project Structure and Organization

### 1. Recommended Folder Structure
```
Assets/
├── Animations/     # Animation clips and controllers
├── Audio/          # Sound effects and music
├── Editor/         # Custom editor scripts
├── Materials/      # Materials and shaders
├── Prefabs/        # Reusable game objects
├── Resources/      # Loadable assets
├── Scenes/         # Game scenes
├── Scripts/        # C# scripts
├── Sprites/        # 2D graphics
└── UI/             # User interface elements
```

### 2. Best Practices
- Use meaningful names for all assets
- Organize assets in appropriate folders
- Use prefabs for reusable objects
- Keep scenes focused and manageable
- Use version control (Git)

## Scripting in Unity

### 1. Basic C# Concepts
```csharp
// Basic MonoBehaviour script
using UnityEngine;

public class ExampleScript : MonoBehaviour
{
    // Variables
    public float speed = 5f;
    private Vector3 direction;

    // Called when the script starts
    void Start()
    {
        direction = Vector3.right;
    }

    // Called every frame
    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
```

### 2. Common Unity Methods
- `Start()`: Called when the script starts
- `Update()`: Called every frame
- `FixedUpdate()`: Called at fixed time intervals (for physics)
- `OnCollisionEnter2D()`: Called when collision occurs
- `OnTriggerEnter2D()`: Called when trigger collision occurs

### 3. Debugging
```csharp
// Debug messages
Debug.Log("Message");           // Regular message
Debug.LogWarning("Warning");    // Warning message
Debug.LogError("Error");        // Error message

// Breakpoints
// 1. Set breakpoint in Visual Studio
// 2. Attach debugger in Unity
// 3. Run game in debug mode
```

## Building Your First Game

### 1. Setting Up a Scene
1. Create a new scene (File > New Scene)
2. Add a background (Create > 2D Object > Sprite)
3. Add player object (Create > 2D Object > Sprite)
4. Add scripts to objects
5. Set up camera and lighting

### 2. Creating Prefabs
1. Create a GameObject in the scene
2. Configure its components
3. Drag it to the Prefabs folder
4. Use it in other scenes

### 3. Building the Game
1. File > Build Settings
2. Add scenes to build
3. Select target platform
4. Configure player settings
5. Click "Build"

## Mobile Development Setup

### 1. Android Setup
1. **Configure Unity for Android**
   - Edit > Preferences > External Tools
   - Set Android SDK and JDK paths
   - Set NDK path (optional)

2. **Configure Build Settings**
   - File > Build Settings
   - Switch platform to Android
   - Set bundle identifier
   - Configure player settings

3. **Test on Device**
   - Enable USB debugging on Android device
   - Connect device to computer
   - Build and Run

### 2. iOS Setup
1. **Configure Unity for iOS**
   - Edit > Preferences > External Tools
   - Set Xcode path
   - Configure signing settings

2. **Configure Build Settings**
   - File > Build Settings
   - Switch platform to iOS
   - Set bundle identifier
   - Configure player settings

3. **Test on Device**
   - Connect iOS device
   - Build and Run
   - Trust developer certificate on device

## Publishing Your Game

### 1. Android Publishing
1. **Prepare for Release**
   - Create keystore
   - Configure signing settings
   - Build release APK/AAB

2. **Google Play Console**
   - Create developer account
   - Create app listing
   - Upload APK/AAB
   - Complete store listing
   - Submit for review

### 2. iOS Publishing
1. **Prepare for Release**
   - Create distribution certificate
   - Create provisioning profile
   - Archive build in Xcode

2. **App Store Connect**
   - Create app record
   - Upload build
   - Complete store listing
   - Submit for review

## Troubleshooting Common Issues

### 1. Build Issues
- **Missing SDK**: Install required SDK components
- **Signing Errors**: Check certificate and provisioning profiles
- **Build Failures**: Check console for specific errors

### 2. Runtime Issues
- **Null Reference**: Check if objects are properly assigned
- **Physics Issues**: Check collider settings
- **Performance Issues**: Use Profiler to identify bottlenecks

### 3. Mobile-Specific Issues
- **Android**: Check manifest permissions
- **iOS**: Check capabilities and entitlements
- **Device Compatibility**: Test on multiple devices

### 4. Getting Help
- Unity Documentation: [docs.unity3d.com](https://docs.unity3d.com)
- Unity Forums: [forum.unity.com](https://forum.unity.com)
- Stack Overflow: [stackoverflow.com](https://stackoverflow.com)
- Unity Answers: [answers.unity.com](https://answers.unity.com)

## Additional Resources
- [Unity Learn](https://learn.unity.com)
- [Unity Asset Store](https://assetstore.unity.com)
- [Unity Manual](https://docs.unity3d.com/Manual/index.html)
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/index.html) 