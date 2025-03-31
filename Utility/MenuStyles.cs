using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace WarpMod.Utility
{
    /// <summary>
    /// Handles UI styling for the warp menu
    /// </summary>
    public class MenuStyles
    {
        // Font and sizing constants
        public const float BUTTON_TEXT_SCALE = 0.9f;
        public const int MIN_BUTTON_HEIGHT = 60;
        
        // UI dimensions
        private Rectangle menuBounds;
        
        public MenuStyles(Rectangle menuBounds)
        {
            this.menuBounds = menuBounds;
        }
        
        /// <summary>
        /// Draw a standard button with appropriate styling
        /// </summary>
        public void DrawButton(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hover)
        {
            // Draw button background
            Color buttonColor = selected ? new Color(255, 255, 235) : new Color(225, 221, 210); 
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                buttonColor, 1f);

            // Center the text
            float scale = BUTTON_TEXT_SCALE;
            Vector2 textSize = Game1.dialogueFont.MeasureString(text) * scale;
            
            // Adjust scale if text doesn't fit
            if (textSize.X > bounds.Width - 24)
            {
                scale *= (bounds.Width - 24) / textSize.X;
            }
            
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X * scale) / 2,
                bounds.Y + (bounds.Height - textSize.Y * scale) / 2
            );

            // Draw text
            Color textColor = selected ? new Color(60, 40, 10) : new Color(80, 60, 30);
            b.DrawString(Game1.dialogueFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Add hover effect
            if (hover)
            {
                b.Draw(Game1.staminaRect, bounds, null, Color.White * 0.15f);
            }
            
            // Add selection indicator
            if (selected)
            {
                // Draw selection border
                int borderWidth = 2;
                Color selectionColor = new Color(150, 180, 255);
                
                // Draw border
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, bounds.Width + borderWidth * 2, borderWidth), null, selectionColor);
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y + bounds.Height, bounds.Width + borderWidth * 2, borderWidth), null, selectionColor);
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2), null, selectionColor);
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X + bounds.Width, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2), null, selectionColor);
            }
        }
        
        /// <summary>
        /// Draw a vertical tab with appropriate styling
        /// </summary>
        public void DrawVerticalTab(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered = false)
        {
            // Draw tab
            Color tabColor = selected ? new Color(255, 255, 240) : (hovered ? new Color(210, 210, 210) : new Color(180, 180, 180));
            
            // Draw tab background
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                tabColor, 1f);

            // Draw text
            float scale = 1.0f;
            Vector2 textSize = Game1.smallFont.MeasureString(text) * scale;
            
            // Adjust scale if needed
            if (textSize.X > bounds.Width - 16)
            {
                scale = (bounds.Width - 16) / textSize.X;
            }

            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X * scale) / 2,
                bounds.Y + (bounds.Height - textSize.Y * scale) / 2
            );

            // Draw text
            Color textColor = selected ? new Color(40, 20, 0) : new Color(50, 40, 30);
            b.DrawString(Game1.smallFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}