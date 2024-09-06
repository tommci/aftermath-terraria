using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace aftermath.Content.Items.Consumables
{
    public class DirtLootbox : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.IceMirror}";

        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.width = 22;
            Item.height = 26;
            Item.rare = ItemRarityID.White;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            int[] drops = new int[]
            {
                ModContent.ItemType<Tools.Lavaporter>()
            };
            itemLoot.Add(ItemDropRule.OneFromOptionsNotScalingWithLuck(1, drops));
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