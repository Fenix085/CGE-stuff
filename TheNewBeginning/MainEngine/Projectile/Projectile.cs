using MainEngine.FlockEnemy;
using MainEngine.Global;
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MainEngine.Projectile;

public class Projectile
{
    public Vector2 Position;
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

    public void Update(float dt)
    {
        Position += Direction * Speed * dt;
        _age += dt;
    }

    public void Draw(SpriteBatch spriteBatch, Sprite sprite)
    {
        sprite.Rotation = MathF.Atan2(Direction.Y,Direction.X);
        sprite.Draw(spriteBatch, Position);
    }
}