using System;
using Microsoft.Xna.Framework;
using MainEngine.Global;
namespace MainEngine.Graphics;

public class AnimatedSprite : Sprite 
{
private int _currentFrame;
private Animation _animation;

/// <summary>
/// Gets or Sets the animation for this animated sprite.
/// </summary>
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

/// <summary>
/// Updates this animated sprite.
/// </summary>
/// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
public override void Update(GameTime gameTime)
{
    Globals.FixedUpdateRate += gameTime.ElapsedGameTime;

    if (Globals.FixedUpdateRate >= _animation.Delay)
    {
        Globals.FixedUpdateRate -= _animation.Delay;
        _currentFrame++;

        if (_currentFrame >= _animation.Frames.Count)
        {
            _currentFrame = 0;
        }

        Region = _animation.Frames[_currentFrame];
    }
}

}
