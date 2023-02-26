﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessRacer.Menu
{
    internal class MenuButton
    {
        private const int DisabledRow = 0;
        private const int EnabledRow = 1;

        private const int SpriteW = 511;
        private const int SpriteH = 147;

        private Texture2D _spriteSheet;
        private Vector2 _position;
        private MainMenuButtonType _type;

        public bool Enabled { get; private set; }

        public MenuButton(Texture2D spriteSheet, Vector2 position, MainMenuButtonType type, bool selected=false)
        {
            _spriteSheet = spriteSheet;
            _position = position;
            _type = type;

            Enabled = selected;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var x = (int)_type * SpriteW;
            var y = Enabled ? EnabledRow * SpriteH : DisabledRow;

            var sourceRectangle = new Rectangle(x, y, SpriteW, SpriteH);

            spriteBatch.Draw(_spriteSheet, _position, sourceRectangle, Color.White);
        }

        public void Enable() => Enabled = true;
        public void Disable() => Enabled = false;
    }
}