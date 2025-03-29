using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace WarpMod.Utility
{
    /// <summary>
    /// Handles UI styling, animations and visual effects for the warp menu
    /// </summary>
    public class MenuStyles
    {
        // Animation properties
        private float starPulsateTimer = 0f;
        private readonly Random random = new Random();
        private readonly List<Vector2> starPositions = new List<Vector2>();
        private readonly Color[] starColors;
        
        // Font and sizing constants
        public const float TITLE_SCALE = 1.0f;
        public const float TAB_TEXT_SCALE = 0.9f;
        public const float BUTTON_TEXT_SCALE = 0.75f;
        
        // UI element sizing
        public const int TITLE_HEIGHT = 64;
        public const int TAB_HEIGHT = 42;
        public const int TAB_PADDING = 6;
        public const int BUTTON_SECTION_TOP_PADDING = 16;
        public const int TITLE_VERTICAL_OFFSET = 8;
        public const int TAB_TEXT_VERTICAL_OFFSET = -4;
        public const int MAP_TOP_MARGIN = 20;
        public const int BUTTON_PADDING = 12;
        
        // Button sizing
        public const int MIN_BUTTON_WIDTH = 160;
        public const int MIN_BUTTON_HEIGHT = 50;
        public const int MAX_BUTTON_WIDTH = 240;
        
        public MenuStyles(Rectangle menuBounds)
        {
            // Initialize star colors
            starColors = new Color[]
            {
                new Color(180, 180, 255), // Blue-white
                new Color(255, 255, 180), // Yellow-white
                new Color(180, 255, 180)  // Green-white
            };
            
            // Generate random stars
            GenerateStarBackground(menuBounds.Width, menuBounds.Height);
        }
        
        /// <summary>
        /// Generate random star positions for the background
        /// </summary>
        public void GenerateStarBackground(int width, int height)
        {
            starPositions.Clear();
            int starCount = random.Next(120, 180); // 120-180 stars

            for (int i = 0; i < starCount; i++)
            {
                starPositions.Add(new Vector2(
                    random.Next(width),
                    random.Next(height)
                ));
            }
        }
        
        /// <summary>
        /// Update animations based on game time
        /// </summary>
        public void Update(GameTime time)
        {
            // Update star pulsation
            starPulsateTimer += (float)time.ElapsedGameTime.TotalSeconds;
            if (starPulsateTimer > 100f) // Reset periodically to avoid float precision issues
                starPulsateTimer = 0f;
        }
        
        /// <summary>
        /// Draw the starry background effect
        /// </summary>
        public void DrawStarryBackground(SpriteBatch b, int xPosition, int yPosition)
        {
            // Draw stars with various sizes and pulsating effects
            for (int i = 0; i < starPositions.Count; i++)
            {
                Vector2 pos = new Vector2(
                    xPosition + starPositions[i].X,
                    yPosition + starPositions[i].Y
                );

                // Determine star properties
                float pulse = (float)Math.Sin(starPulsateTimer * 2f + i * 0.1f) * 0.5f + 0.5f;
                float size = (i % 3 == 0) ? 1.5f : (i % 3 == 1 ? 1f : 0.7f);
                Color starColor = starColors[i % starColors.Length] * pulse;

                // Use actual star sprite from Stardew Valley
                Rectangle starSource = new Rectangle(346, 392, 8, 8); // Star sprite from mouseCursors
                b.Draw(Game1.mouseCursors, pos, starSource, starColor, 0f, Vector2.Zero, size, SpriteEffects.None, 0.99f);

                // For some stars, add a glow effect
                if (i % 5 == 0)
                {
                    b.Draw(Game1.mouseCursors, pos, starSource, starColor * 0.3f, 0f, 
                        new Vector2(4f, 4f), size * 2f, SpriteEffects.None, 0.98f);
                }
            }
        }
        
        /// <summary>
        /// Draw decorative sparkle effects
        /// </summary>
        public void DrawSparkle(SpriteBatch b, float x, float y, float timeOffset)
        {
            // Make sparkles more visible with better sprites and larger size
            float sparkleScale = (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 3 + timeOffset) * 0.2f + 0.8f);

            // Use star sparkle sprite from Stardew's sprite sheet (better-looking sparkle)
            Rectangle sparkleSource = new Rectangle(141, 465, 10, 10); // Brighter sparkle sprite

            // Draw multiple sparkles with slight offsets for a more magical effect
            Color sparkleColor = new Color(255, 255, 150) * sparkleScale; // Yellow-white glow

            // Draw main sparkle
            b.Draw(Game1.mouseCursors, new Vector2(x, y), sparkleSource, sparkleColor, 0f, Vector2.Zero, 2f * sparkleScale, SpriteEffects.None, 0f);

            // Draw smaller accent sparkles
            float smallScale = sparkleScale * 0.7f;
            b.Draw(Game1.mouseCursors, new Vector2(x + 12, y - 5), sparkleSource, sparkleColor * 0.8f, 0f, Vector2.Zero, smallScale, SpriteEffects.None, 0f);
            b.Draw(Game1.mouseCursors, new Vector2(x - 8, y + 8), sparkleSource, sparkleColor * 0.7f, 0f, Vector2.Zero, smallScale, SpriteEffects.None, 0f);
        }
        
        /// <summary>
        /// Draw a menu button with appropriate styling
        /// </summary>
        public void DrawLocationButton(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hover)
        {
            // Draw button background
            Color buttonColor = selected ? Color.White : new Color(215, 211, 200);
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                buttonColor, 1f);

            // Center the text
            float scale = BUTTON_TEXT_SCALE;
            Vector2 textSize = Game1.dialogueFont.MeasureString(text) * scale;
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y * scale) / 2
            );

            // Draw text with bold effect and shadow
            Color textColor = selected ? new Color(68, 58, 46) : new Color(88, 78, 66);
            // Draw shadow
            b.DrawString(Game1.dialogueFont, text, textPos + new Vector2(2, 2), Color.Gray * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            // Draw bold effect by drawing text multiple times with slight offsets
            b.DrawString(Game1.dialogueFont, text, textPos + new Vector2(1, 0), textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, text, textPos + new Vector2(0, 1), textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Add hover effect
            if (hover)
            {
                b.Draw(Game1.staminaRect, bounds, null, Color.White * 0.1f);
            }
        }
        
        /// <summary>
        /// Draw a menu tab with appropriate styling
        /// </summary>
        public void DrawTab(SpriteBatch b, Rectangle bounds, string text, bool selected)
        {
            // Draw tab background
            Rectangle tabBackground = bounds;
            if (!selected)
                tabBackground.Height -= 4;

            // Draw tab shadow
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                tabBackground.X, tabBackground.Y + 2, tabBackground.Width, tabBackground.Height,
                Color.Black * 0.2f, 1f, false);

            // Improved unselected color - blend with background
            Color tabColor = selected ? Color.White : new Color(215, 211, 200);
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                tabBackground.X, tabBackground.Y, tabBackground.Width, tabBackground.Height,
                tabColor, 1f, false);

            // Draw tab text with proper font and styling
            float scale = TAB_TEXT_SCALE;
            Vector2 textSize = Game1.dialogueFont.MeasureString(text) * scale;

            // Adjust scale if text is too wide
            if (textSize.X > bounds.Width - 20)
            {
                scale *= (bounds.Width - 20) / textSize.X;
            }

            Vector2 textPos = new Vector2(
                bounds.X + bounds.Width / 2 - (textSize.X / 2),
                bounds.Y + (bounds.Height - textSize.Y * scale) / 2 + TAB_TEXT_VERTICAL_OFFSET
            );

            // Improved text colors
            Color textColor = selected ? new Color(68, 58, 46) : new Color(88, 78, 66);
            b.DrawString(Game1.dialogueFont, text, textPos + new Vector2(2, 2), Color.Gray * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Draw selected tab indicator
            if (selected)
            {
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y + bounds.Height - 2,
                    bounds.Width, 2), null, new Color(68, 58, 46) * 0.5f);
            }
        }
        
        /// <summary>
        /// Draw a vertical tab with appropriate styling
        /// </summary>
        public void DrawVerticalTab(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered = false)
        {
            // Draw tab background
            Rectangle tabBackground = bounds;
            if (!selected)
                tabBackground.Width -= 4;

            // Draw tab shadow
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                tabBackground.X + 2, tabBackground.Y, tabBackground.Width, tabBackground.Height,
                Color.Black * 0.2f, 1f, false);

            // Improved unselected color - blend with background
            Color tabColor = selected ? Color.White : (hovered ? new Color(225, 221, 210) : new Color(215, 211, 200));
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                tabBackground.X, tabBackground.Y, tabBackground.Width, tabBackground.Height,
                tabColor, 1f, false);

            // Draw tab text with proper font and styling
            float scale = TAB_TEXT_SCALE;
            Vector2 textSize = Game1.dialogueFont.MeasureString(text) * scale;

            // Adjust scale if text is too wide
            if (textSize.X > bounds.Width - 20)
            {
                scale *= (bounds.Width - 20) / textSize.X;
            }

            Vector2 textPos = new Vector2(
                bounds.X + bounds.Width / 2 - (textSize.X / 2),
                bounds.Y + (bounds.Height - textSize.Y * scale) / 2 + TAB_TEXT_VERTICAL_OFFSET
            );

            // Improved text colors
            Color textColor = selected ? new Color(68, 58, 46) : (hovered ? new Color(78, 68, 56) : new Color(88, 78, 66));
            b.DrawString(Game1.dialogueFont, text, textPos + new Vector2(2, 2), Color.Gray * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Draw selected tab indicator
            if (selected)
            {
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X + bounds.Width - 2, bounds.Y,
                    2, bounds.Height), null, new Color(68, 58, 46) * 0.5f);
            }
        }
        
        /// <summary>
        /// Draw the menu title with decorative elements
        /// </summary>
        public void DrawTitle(SpriteBatch b, Rectangle menuBounds, string title)
        {
            // Draw title banner
            Rectangle headerBounds = new Rectangle(menuBounds.X + 32, menuBounds.Y + 16, menuBounds.Width - 64, TITLE_HEIGHT);
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                headerBounds.X, headerBounds.Y, headerBounds.Width, headerBounds.Height,
                Color.White, 1f, false);

            // Calculate title position
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title) * TITLE_SCALE;
            Vector2 titlePos = new Vector2(
                menuBounds.X + (menuBounds.Width - titleSize.X) / 2,
                headerBounds.Y + (headerBounds.Height - titleSize.Y) / 2
            );

            // Draw title with shadow
            b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), Color.DarkBlue * 0.3f, 0f, Vector2.Zero, TITLE_SCALE, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, title, titlePos, Game1.textColor, 0f, Vector2.Zero, TITLE_SCALE, SpriteEffects.None, 0f);

            // Draw decorative icons (using proper Stardew icons)
            int iconSize = 32;
            Rectangle leftIconSource = new Rectangle(120, 428, 10, 10);  // Star icon
            Rectangle rightIconSource = new Rectangle(120, 428, 10, 10); // Same star on other side
            float iconY = titlePos.Y + (titleSize.Y - iconSize) / 2;

            b.Draw(Game1.mouseCursors, new Rectangle((int)titlePos.X - iconSize - 15, (int)iconY, iconSize, iconSize),
                leftIconSource, Color.White * 0.8f);
            b.Draw(Game1.mouseCursors, new Rectangle((int)(titlePos.X + titleSize.X + 15), (int)iconY, iconSize, iconSize),
                rightIconSource, Color.White * 0.8f);
        }
    }
}