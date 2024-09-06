using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace aftermath.Content.Items.Tools
{
    internal class StaffofDiscord : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.Vilethorn}";

        public override void SetDefaults()
        {
            Item.UseSound = SoundID.Item3;
            Item.useStyle = 4;
            Item.rare = -13;
            Item.width = 40;
            Item.height = 40;
            Item.consumable = false;
            Item.useTime = 1;
            Item.useTurn = true;
            Item.useAnimation = 1;
        }

        public override bool? UseItem(Player player)
        {

            player.RemoveAllGrapplingHooks();
            Vector2 location = Main.MouseWorld;

            player.Teleport(location, 0, 0);
            return base.UseItem(player);
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