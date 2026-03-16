using MainEngine.FlockEnemy;
using MainEngine.Global;
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MainEngine.Projectile;

public class Projectile : Sprite
{
    public Vector2 Direction;
    public float Speed = 500f;
    public float LifeTime = 2f;
    private float _age = 0f;

    public bool Hit = false;

    public bool IsDead => _age >= LifeTime || Hit;

    public Circle Bounds => new Circle(
        (int)Position.X,
        (int)Position.Y,
        10
    );

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Move(dt);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Direction != Vector2.Zero)
            Rotation = MathF.Atan2(Direction.Y, Direction.X);

        base.Draw(gameTime, spriteBatch);
    }

    private void Move(float timeStep)
    {
        Position += Direction * Speed * timeStep;
        _age += timeStep;
    }

}