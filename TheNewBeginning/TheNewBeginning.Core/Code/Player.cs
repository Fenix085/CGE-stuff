using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheNewBeginning;

public class Player : Sprite
{
    public Player(Texture2D tex, Vector2 pos) : base (tex,pos)
    {
        
    }
    private float _currentShootAngle;
    private void Action()
    {
        // Placeholder for player action, such as firing a weapon or interacting with objects.
    }
    public void Update()
    {
        if(InputManager.Direction != Vector2.Zero)
        {
            var dir = Vector2.Normalize(InputManager.Direction);
            Position += dir * Speed * Globals.TotalSeconds;
        }
        
        var toMouse = InputManager.MousePosition - Position;
        float angle = (float)Math.Atan2(toMouse.Y, toMouse.X);

        Rotation = angle - MathHelper.PiOver2;
        _currentShootAngle = angle;

        if (InputManager.MouseClicked)
        {
            Action();
        }
    }
}