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
        // Define colors as static readonly fields to avoid magic values
        private static readonly Color ButtonColorNormal = new Color(225, 221, 210);
        private static readonly Color ButtonColorSelected = new Color(255, 255, 235);
        private static readonly Color TextColorNormal = new Color(80, 60, 30);
        private static readonly Color TextColorSelected = new Color(60, 40, 10);
        private static readonly Color TabColorNormal = new Color(180, 180, 180);
        private static readonly Color TabColorHovered = new Color(210, 210, 210);
        private static readonly Color TabColorSelected = new Color(255, 255, 240);
        
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
            Color buttonColor = selected ? ButtonColorSelected : ButtonColorNormal;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                buttonColor, 1f);

            // Draw text centered with proper scaling
            DrawCenteredText(b, text, bounds, selected ? TextColorSelected : TextColorNormal);
            
            // Add hover effect
            if (hover)
            {
                b.Draw(Game1.staminaRect, bounds, null, Color.White * 0.15f);
            }
            
            // Add selection indicator if selected
            if (selected)
            {
                DrawSelectionBorder(b, bounds);
            }
        }
        
        /// <summary>
        /// Draw a vertical tab with appropriate styling
        /// </summary>
        public void DrawVerticalTab(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered = false)
        {
            // Determine color based on state
            Color tabColor = selected ? TabColorSelected : (hovered ? TabColorHovered : TabColorNormal);
            
            // Draw tab background
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                tabColor, 1f);

            // Draw text centered
            DrawCenteredText(b, text, bounds, selected ? TextColorSelected : TextColorNormal);
        }
        
        /// <summary>
        /// Helper method to draw centered text
        /// </summary>
        private void DrawCenteredText(SpriteBatch b, string text, Rectangle bounds, Color textColor, float baseScale = 1.0f)
        {
            float scale = baseScale;
            Vector2 textSize = Game1.smallFont.MeasureString(text) * scale;
            
            // Adjust scale if text doesn't fit
            if (textSize.X > bounds.Width - 16)
            {
                scale *= (bounds.Width - 16) / textSize.X;
                textSize = Game1.smallFont.MeasureString(text) * scale;
            }
            
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            
            b.DrawString(Game1.smallFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        /// <summary>
        /// Draw a selection border around a rectangle
        /// </summary>
        private void DrawSelectionBorder(SpriteBatch b, Rectangle bounds)
        {
            int borderWidth = 2;
            Color selectionColor = new Color(150, 180, 255);
            
            // Draw border (top, bottom, left, right)
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, bounds.Width + borderWidth * 2, borderWidth), null, selectionColor);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y + bounds.Height, bounds.Width + borderWidth * 2, borderWidth), null, selectionColor);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2), null, selectionColor);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X + bounds.Width, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2), null, selectionColor);
        }
    }
}