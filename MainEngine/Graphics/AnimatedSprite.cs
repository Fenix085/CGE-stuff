using System;
using Microsoft.Xna.Framework;
namespace MainEngine.Graphics;

public class AnimatedSprite : Sprite 
{
private int _currentFrame;
private Animation _animation;
private TimeSpan _elapsed;

public bool PlayOnce    { get; set; } = false;
public bool IsFinished  { get; private set; } = false;
public Animation Animation
{
    get => _animation;
    set
    {
        _animation = value;
        Region = _animation.Frames[0];
    }
}

/// <summary>
/// Creates a new animated sprite.
/// </summary>
public AnimatedSprite() { }

/// <summary>
/// Creates a new animated sprite with the specified frames and delay.
/// </summary>
/// <param name="animation">The animation for this animated sprite.</param>
public AnimatedSprite(Animation animation)
{
    Animation = animation;
}

public override void ApplyDeath()
{
    // AnimatedSprite death logic here (e.g., play death animation, remove from game, etc.)
}

/// <summary>
/// Updates this animated sprite.
/// </summary>
/// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
public override void Update(GameTime gameTime)
{
    _elapsed += gameTime.ElapsedGameTime;

    if (_elapsed >= _animation.Delay)
    {
        _elapsed -= _animation.Delay;
        _currentFrame++;

        if (_currentFrame >= _animation.Frames.Count)
        {
            if (PlayOnce)
                {
                    
                    _currentFrame = _animation.Frames.Count - 1;
                    IsFinished    = true;
                }
            else
                {
                    _currentFrame = 0;
                }
        }

        Region = _animation.Frames[_currentFrame];
    }
}

}
