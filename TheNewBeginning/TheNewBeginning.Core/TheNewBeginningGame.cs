using System;
using TheNewBeginning.Core.Localization;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace TheNewBeginning.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    
    
    public class TheNewBeginningGame : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager _graphics;
        private GameManager _gameManager;
        // Texture2D playerTexture;
        // private GraphicsDeviceManager graphicsDeviceManager;
        // private SpriteBatch _spriteBatch;
        
        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the game. Configures platform-specific settings, 
        /// initializes services like settings and leaderboard managers, and sets up the 
        /// screen manager for screen transitions.
        /// </summary>
        public TheNewBeginningGame()
        {
            _graphics = new GraphicsDeviceManager(this);
        
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        /// <summary>
        /// Initializes the game, including setting up localization and adding the 
        /// initial screens to the ScreenManager.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Load supported languages and set the default language.
            List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
            var languages = new List<CultureInfo>();
            for (int i = 0; i < cultures.Count; i++)
            {
                languages.Add(cultures[i]);
            }

            // TODO You should load this from a settings file or similar,
            // based on what the user or operating system selected.
            var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
            LocalizationManager.SetCulture(selectedLanguage);
        }

        /// <summary>
        /// Loads game content, such as textures and particle systems.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            Globals.SpriteBatch = new SpriteBatch(GraphicsDevice);
            // Make the content manager globally accessible for loading assets in other parts of the game.
            Globals.Content = Content;
            // Load any game-specific content here, such as textures, sounds, etc.
            _gameManager = new GameManager();
            
            base.LoadContent();
        }

        
        protected override void Update(GameTime gameTime)
        {
            // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            // Update the global time variable for use in other parts of the game.
            _gameManager.Update(gameTime);
            Globals.TotalSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        
        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the MonoGame gray color before drawing.
            GraphicsDevice.Clear(Color.Gray);

            // Begin the sprite batch, draw the game elements, and end the sprite batch.
            Globals.SpriteBatch.Begin();
            _gameManager.Draw(gameTime);
            Globals.SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}