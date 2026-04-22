using System;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheNewBeginning.UI;

internal sealed class TitleSceneEffectsRenderer : IDisposable
{
    private readonly Texture2D _pixel;
    private readonly Rectangle _backgroundDestination;

    private float _elapsedSeconds;
    private bool _isDisposed;

    public TitleSceneEffectsRenderer()
    {
        _backgroundDestination = HQ.GraphicsDevice.PresentationParameters.Bounds;

        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Update(GameTime gameTime)
    {
        _elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw()
    {
        HQ.GraphicsDevice.Clear(new Color(14, 20, 23, 255));

        DrawMenuBackdrop();
    }

    private void DrawMenuBackdrop()
    {
        HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawRect(_backgroundDestination, new Color(17, 26, 28));
        DrawRect(new Rectangle(0, 0, _backgroundDestination.Width, 244), new Color(30, 55, 48) * 0.82f);
        DrawRect(new Rectangle(0, 244, _backgroundDestination.Width, 170), new Color(51, 71, 49) * 0.45f);
        DrawRect(new Rectangle(0, 560, _backgroundDestination.Width, 160), new Color(8, 13, 18) * 0.65f);

        int drift = (int)(_elapsedSeconds * 18f) % 64;
        for (int x = -64 + drift; x < _backgroundDestination.Width + 64; x += 64)
        {
            DrawRect(new Rectangle(x, 0, 2, _backgroundDestination.Height), new Color(112, 137, 97) * 0.14f);
        }

        for (int y = 0; y < _backgroundDestination.Height; y += 64)
        {
            DrawRect(new Rectangle(0, y, _backgroundDestination.Width, 2), new Color(112, 137, 97) * 0.10f);
        }

        HQ.SpriteBatch.End();
    }

    private void DrawRect(Rectangle rectangle, Color color)
    {
        HQ.SpriteBatch.Draw(_pixel, rectangle, color);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _pixel.Dispose();
        _isDisposed = true;
    }
}
