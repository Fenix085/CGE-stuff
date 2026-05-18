using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace LILITH.Audio;

public static class AudioAssets
{
    // ── UI ─────────────────────────────────────

    public static SoundEffect PauseOpen  = null!;
    public static SoundEffect PauseClose = null!;
    public static SoundEffect ButtonClick = null!;

    // ── Player ─────────────────────────────────

    public static SoundEffect Footsteps = null!;
    public static SoundEffect Shoot     = null!;
    public static SoundEffect PlayerHit = null!;
    public static SoundEffect PlayerDeath = null!;

    // ── Enemy ──────────────────────────────────

    public static SoundEffect EnemyHit   = null!;
    public static SoundEffect EnemyDeath = null!;

    // ── Music ──────────────────────────────────

    public static Song MainMenuMusic = null!;
    public static Song GameMusic     = null!;
}