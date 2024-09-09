using Terraria;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria.ID;

namespace aftermath.Content.Items.Accessories
{
    public class BuilderDelight : ModItem
    {
        public static Vector3 glowColor = new Vector3(255, 255, 255);
        public override string Texture => $"Terraria/Images/Item_{ItemID.SkywareClock}";

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            Lighting.AddLight(player.Center, glowColor);
            player.GetModPlayer<BuilderDelightPlayer>().builderDelight = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DirtBlock, 10);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }

    public class BuilderDelightPlayer : ModPlayer
    {
        public bool builderDelight = false;

        public override void ResetEffects()
        {
            builderDelight = false;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (Player.mount.Active || !builderDelight)
            {
                return;
            }

            Player.runAcceleration *= 1.8f;
            Player.maxRunSpeed *= 1.8f;
            Player.accRunSpeed *= 1.8f;
            Player.runSlowdown *= 1.8f;
            Player.pickSpeed -= 500f;
            Player.tileSpeed -= 50f;
            Player.wallSpeed -= 50f;
            Player.tileRangeX *= 15;
            Player.tileRangeY *= 15;
        }
    }
}