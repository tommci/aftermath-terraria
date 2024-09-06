using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace aftermath.Content.Items.Tools
{
    internal class Lavaporter : ModItem
    {

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.IceMirror);
            Item.color = Color.White;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.itemTime == 0)
            {
                player.ApplyItemTime(Item);
            }
            else if (player.itemTime == player.itemTimeMax / 2)
            {
                player.RemoveAllGrapplingHooks();

                Vector2 location = new Vector2(0, 0);
                Point tileLocation = Main.LocalPlayer.Center.ToTileCoordinates();
                Tile currentTile = Main.tile[tileLocation];

                int i = 1, failCheck = 0;
                while (true)
                {
                    // check below
                    if ((tileLocation.Y + i) < Main.maxTilesY)
                    {
                        currentTile = Main.tile[tileLocation.X, tileLocation.Y + i];
                        if (currentTile.LiquidAmount > 0 && currentTile.LiquidType == 1)
                        {
                            location.X = tileLocation.X * 16;
                            location.Y = (tileLocation.Y + i) * 16;
                            break;
                        }
                    } else { failCheck++; }

                    // check above
                    if ((tileLocation.Y - i) > 0)
                    {
                        currentTile = Main.tile[tileLocation.X, tileLocation.Y - i];
                        if (currentTile.LiquidAmount > 0 && currentTile.LiquidType == 1)
                        {
                            location.X = tileLocation.X * 16;
                            location.Y = (tileLocation.Y - i) * 16;
                            break;
                        }
                    } else { failCheck++; }

                    // check right
                    if ((tileLocation.X + i) < Main.maxTilesX)
                    {
                        currentTile = Main.tile[tileLocation.X + i, tileLocation.Y];
                        if (currentTile.LiquidAmount > 0 && currentTile.LiquidType == 1)
                        {
                            location.X = (tileLocation.X + i) * 16;
                            location.Y = tileLocation.Y * 16;
                            break;
                        }
                    } else { failCheck++; }

                    // check left
                    if ((tileLocation.X - i) > 0)
                    {
                        currentTile = Main.tile[tileLocation.X - i, tileLocation.Y];
                        if (currentTile.LiquidAmount > 0 && currentTile.LiquidType == 1)
                        {
                            location.X = (tileLocation.X - i) * 16;
                            location.Y = tileLocation.Y * 16;
                            break;
                        }
                    } else { failCheck++; }

                    // if we're stuck checking out of bounds no matter what, break the loop to avoid softlock
                    if (failCheck >= 4) { break; }

                    i++;
                    failCheck = 0;
                }

                player.Teleport(location, 0, 0);
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DirtBlock, 10);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}