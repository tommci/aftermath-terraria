using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using aftermath.Content.Projectiles;

namespace aftermath.Content.Items.Weapons
{
    public class DualBlades : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.FalconBlade}";

        public int attackType = 0;
        public int comboExpireTimer = 0;

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.rare = ItemRarityID.Blue;

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;

            Item.knockBack = 0;
            Item.autoReuse = true;
            Item.damage = 10;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.shoot = ModContent.ProjectileType<DualbladeSwing>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer, attackType);
            attackType = (attackType + 1) % 2; // value after modulo is how many different attacks there are
            comboExpireTimer = 0;
            return false;
        }

        public override void UpdateInventory(Player player)
        {
            if (comboExpireTimer++ >= 30) // combo resets after inactivity
            {
                attackType = 0;
            }
        }

        public override bool MeleePrefix()
        {
            return true;
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