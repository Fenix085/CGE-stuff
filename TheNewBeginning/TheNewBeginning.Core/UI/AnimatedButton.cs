using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Graphics.Animation;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using MainEngine.Graphics;

namespace TheNewBeginning.UI;

internal class AnimatedButton : Button
{
    private readonly NineSliceRuntime _background;
    private readonly TextRuntime _textInstance;
    private readonly ButtonVisual _buttonVisual;
    private Color _backgroundColor = Color.White;
    private Color _textColor = new Color(70, 86, 130);

    public AnimatedButton(TextureAtlas atlas)
    {
 
        _buttonVisual = (ButtonVisual)Visual;

        _buttonVisual.Height = 17f;
        _buttonVisual.HeightUnits = DimensionUnitType.Absolute;
        _buttonVisual.Width = 25f;
        _buttonVisual.WidthUnits = DimensionUnitType.RelativeToChildren;

        TextureRegion unfocusedTextureRegion = atlas.GetRegion("unfocused-button");
        
        _background = _buttonVisual.Background;
        _background.Texture = unfocusedTextureRegion.Texture;
        _background.TextureAddress = TextureAddress.Custom;
        _background.Color = _backgroundColor;

        _textInstance = _buttonVisual.TextInstance;
        _textInstance.Text = "START";
        _textInstance.Color = _textColor;
        _textInstance.UseCustomFont = true;
        _textInstance.CustomFontFile = "fonts/04b_30.fnt";
        _textInstance.FontScale = 0.32f;
        _textInstance.Anchor(Gum.Wireframe.Anchor.Center);
        _textInstance.Width = 0;
        _textInstance.WidthUnits = DimensionUnitType.RelativeToChildren;

        AnimationChain unfocusedAnimation = new AnimationChain();
        unfocusedAnimation.Name = nameof(unfocusedAnimation);
        AnimationFrame unfocusedFrame = new AnimationFrame
        {
            TopCoordinate = unfocusedTextureRegion.TopTextureCoordinate,
            BottomCoordinate = unfocusedTextureRegion.BottomTextureCoordinate,
            LeftCoordinate = unfocusedTextureRegion.LeftTextureCoordinate,
            RightCoordinate = unfocusedTextureRegion.RightTextureCoordinate,
            FrameLength = 0.3f,
            Texture = unfocusedTextureRegion.Texture
        };
        unfocusedAnimation.Add(unfocusedFrame);

        Animation focusedAtlasAnimation = atlas.GetAnimation("focused-button-animation");

        AnimationChain focusedAnimation = new AnimationChain();
        focusedAnimation.Name = nameof(focusedAnimation);
        foreach (TextureRegion region in focusedAtlasAnimation.Frames)
        {
            AnimationFrame frame = new AnimationFrame
            {
                TopCoordinate = region.TopTextureCoordinate,
                BottomCoordinate = region.BottomTextureCoordinate,
                LeftCoordinate = region.LeftTextureCoordinate,
                RightCoordinate = region.RightTextureCoordinate,
                FrameLength = (float)focusedAtlasAnimation.Delay.TotalSeconds,
                Texture = region.Texture
            };

            focusedAnimation.Add(frame);
        }

        _background.AnimationChains = new AnimationChainList
        {
            unfocusedAnimation,
            focusedAnimation
        };

        _buttonVisual.ButtonCategory.ResetAllStates();

        StateSave enabledState = _buttonVisual.States.Enabled;
        enabledState.Apply = () =>
        {
            _background.CurrentChainName = unfocusedAnimation.Name;
            ApplyPalette();
        };

        StateSave focusedState = _buttonVisual.States.Focused;
        focusedState.Apply = () =>
        {
            _background.CurrentChainName = focusedAnimation.Name;
            _background.Animate = true;
            ApplyPalette();
        };

        StateSave highlightedFocused = _buttonVisual.States.HighlightedFocused;
        highlightedFocused.Apply = focusedState.Apply;

        StateSave highlighted = _buttonVisual.States.Highlighted;
        highlighted.Apply = enabledState.Apply;

        KeyDown += HandleKeyDown;

        _buttonVisual.RollOn += HandleRollOn;
    }

    public void SetPalette(Color backgroundColor, Color textColor)
    {
        _backgroundColor = backgroundColor;
        _textColor = textColor;

        ApplyPalette();
    }

    public void SetVisualSize(float height, float textScale)
    {
        _buttonVisual.Height = height;
        _textInstance.FontScale = textScale;
    }

    private void ApplyPalette()
    {
        _background.Color = _backgroundColor;
        _textInstance.Color = _textColor;
    }

    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Left)
        {
            HandleTab(TabDirection.Up, loop: true);
        }
        if (e.Key == Keys.Right)
        {
            HandleTab(TabDirection.Down, loop: true);
        }
    }
    private void HandleRollOn(object sender, EventArgs e)
    {
        IsFocused = true;
    }
}
