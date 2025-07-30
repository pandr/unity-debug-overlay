/// <summary>
/// Example CVars used by the game. Feel free to add more.
/// </summary>
public static class GameCVars
{
    // Visuals
    public static readonly CVarFloat Fov = new CVarFloat("fov", 60f, "Vertical field of view (degrees)");

    // Gameplay toggles
    public static readonly CVarBool GodMode = new CVarBool("god", false, "Player invulnerability toggle");
}
