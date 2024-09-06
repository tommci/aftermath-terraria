using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace aftermath.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class TomyWings : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 60 ticks in a second
            // syntax is WingStats(flight time (ticks), speed, acceleration mult)
            // for reference: solar wings use 180, 9, 2.5
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(3600, 20f, 5f);
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 20;
            Item.value = 10000;
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
            ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f; // Falling glide speed
            ascentWhenRising = 1f; // Rising speed
            maxCanAscendMultiplier = 2f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;
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