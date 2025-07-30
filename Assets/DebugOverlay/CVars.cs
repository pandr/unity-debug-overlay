using System;
using UnityEngine;

/// <summary>
/// Example config variables for common game settings.
/// Add your own cvars here - they'll automatically be available in the console!
/// 
/// Usage examples:
///   In console: "fov = 110" or "set fov 110"
///   In code: float currentFov = CVars.fov; // Ultra-fast access
/// </summary>
public static class CVars
{
    // === Graphics Settings ===
    public static readonly CVar<int> fov = new CVar<int>(
        "fov", 90, "Field of view in degrees",
        validator: x => x >= 60 && x <= 150,
        onChanged: x => Debug.Log($"FOV changed to {x}")
    );

    public static readonly CVar<int> maxFps = new CVar<int>(
        "maxfps", 60, "Maximum framerate (0 = unlimited)",
        validator: x => x >= 0 && x <= 300,
        onChanged: x => Application.targetFrameRate = x == 0 ? -1 : x
    );

    public static readonly CVar<bool> vsync = new CVar<bool>(
        "vsync", true, "Enable vertical sync",
        onChanged: x => QualitySettings.vSyncCount = x ? 1 : 0
    );

    public static readonly CVar<float> mouseSensitivity = new CVar<float>(
        "sensitivity", 1.0f, "Mouse sensitivity multiplier",
        validator: x => x > 0.0f && x <= 10.0f
    );

    // === Audio Settings ===
    public static readonly CVar<float> masterVolume = new CVar<float>(
        "volume", 1.0f, "Master volume (0-1)",
        validator: x => x >= 0.0f && x <= 1.0f,
        onChanged: x => AudioListener.volume = x
    );

    public static readonly CVar<bool> muteAudio = new CVar<bool>(
        "mute", false, "Mute all audio",
        onChanged: x => AudioListener.pause = x
    );

    // === Debug Settings ===
    public static readonly CVar<bool> showFps = new CVar<bool>(
        "showfps", true, "Show FPS counter"
    );

    public static readonly CVar<bool> showStats = new CVar<bool>(
        "showstats", false, "Show detailed performance stats"
    );

    public static readonly CVar<bool> drawWireframe = new CVar<bool>(
        "wireframe", false, "Render in wireframe mode",
        onChanged: x => Camera.main.GetComponent<Camera>().renderingPath = x ? RenderingPath.Forward : RenderingPath.DeferredShading
    );

    // === Game Settings ===
    public static readonly CVar<string> playerName = new CVar<string>(
        "playername", "Player", "Player display name"
    );

    public static readonly CVar<float> walkSpeed = new CVar<float>(
        "walkspeed", 5.0f, "Player walking speed",
        validator: x => x > 0.0f && x <= 50.0f
    );

    public static readonly CVar<float> jumpHeight = new CVar<float>(
        "jumpheight", 2.0f, "Player jump height",
        validator: x => x >= 0.0f && x <= 10.0f
    );

    // === Console Settings ===
    public static readonly CVar<float> consoleSpeed = new CVar<float>(
        "consolespeed", 5.0f, "Console open/close animation speed",
        validator: x => x > 0.0f && x <= 20.0f
    );

    public static readonly CVar<int> consoleHeight = new CVar<int>(
        "consoleheight", 25, "Console height in characters",
        validator: x => x >= 10 && x <= 50
    );

    // === Network Settings (if applicable) ===
    public static readonly CVar<string> serverAddress = new CVar<string>(
        "serveraddress", "localhost", "Server IP address"
    );

    public static readonly CVar<int> serverPort = new CVar<int>(
        "serverport", 7777, "Server port",
        validator: x => x > 0 && x <= 65535
    );

    public static readonly CVar<int> networkRate = new CVar<int>(
        "rate", 20, "Network update rate (Hz)",
        validator: x => x >= 1 && x <= 128
    );

    /// <summary>
    /// Example of how to create a cvar with complex validation
    /// </summary>
    public static readonly CVar<string> gameMode = new CVar<string>(
        "gamemode", "normal", "Current game mode",
        validator: ValidateGameMode
    );

    private static bool ValidateGameMode(string mode)
    {
        return mode == "normal" || mode == "hardcore" || mode == "creative" || mode == "debug";
    }
}