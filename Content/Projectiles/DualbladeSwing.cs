using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace aftermath.Content.Projectiles
{
    public class DualbladeSwing : ModProjectile
    {

        private const float SWING_RANGE = 1.33f * (float)Math.PI; // angle the two swing types swing cover
        private const float BEFORE_ATK = 0.05f; // how much of swing happens before it can do damage
        private const float UNWIND = 0.4f; // how long until the attack is over

        private enum AttackType
        {
            DownwardSwing,

            UpwardSwing,
        }

        private enum AttackStage
        {
            PrepareSwing,
            Swing,
        }

        // these make code more readable and less tedious to write
        private AttackType CurrentAttack
        {
            get => (AttackType)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        private AttackStage CurrentStage
        {
            get => (AttackStage)Projectile.localAI[0];
            set
            {
                Projectile.localAI[0] = (float)value;
                Timer = 0;
            }
        }

        private ref float InitialAngle => ref Projectile.ai[1]; // Angle aimed in (with constraints) (from examplemod)
        private ref float Timer => ref Projectile.ai[2]; // Timer to keep track of progression of each stage (from examplemod)
        private ref float Progress => ref Projectile.localAI[1]; // Position of sword relative to initial angle (from examplemod)
        // note that sword size will probably remain constant, but keeping track of size *would* go here otherwise

        public override string Texture => $"Terraria/Images/Item_{ItemID.FalconBlade}"; // uses falconblade texture (for now)
        private Player Owner => Main.player[Projectile.owner];

        private float prepTime => 1f / Owner.GetTotalAttackSpeed(Projectile.DamageType); // amount of time in prep (pre-swing)
        private float execTime => 6f / Owner.GetTotalAttackSpeed(Projectile.DamageType); // amount of time in actual swing

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true; // i have no idea what this does, example mod does it
        }

        public override void SetDefaults()
        {
            Projectile.width = 50; // hitbox width and height - may need adjusting
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.timeLeft = 10000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ownerHitCheck = true; // cannot hit things through tiles
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1; // this determines which way the sword faces based on x position of mouse
            float targetAngle = (Main.MouseWorld - Owner.MountedCenter).ToRotation(); // the dbs will point to the mouse cursor when swinging
            //targetAngle = MathHelper.Clamp(targetAngle, (float)-Math.PI * 1 / 3, (float)Math.PI * 1 / 6);

            if (CurrentAttack == AttackType.DownwardSwing)
            {
                if (Projectile.spriteDirection > 0)
                {
                    InitialAngle = targetAngle + 5f;
                }
                else
                {
                    InitialAngle = targetAngle - 5f;
                }
            } else
            {
                if (Projectile.spriteDirection > 0)
                {
                    InitialAngle = targetAngle - 5f;
                    Projectile.spriteDirection *= -1;
                }
                else
                {
                    InitialAngle = targetAngle + 5f;
                    Projectile.spriteDirection *= -1;
                }
            }       
        }

        // for multiplayer syncing (straight outta examplemod): 
        public override void SendExtraAI(BinaryWriter writer)
        {
            // Projectile.spriteDirection for this projectile is derived from the mouse position of the owner in OnSpawn, as such it needs to be synced. spriteDirection is not one of the fields automatically synced over the network. All Projectile.ai slots are used already, so we will sync it manually. 
            writer.Write((sbyte)Projectile.spriteDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.spriteDirection = reader.ReadSByte();
        }

        public override void AI()
        {
            Owner.itemAnimation = 2; // changing these will extend the length of time it takes to swing again
            Owner.itemTime = 2;      // always keep them the same

            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed) // if the owner is dead or something get rid of the proj
            {
                Projectile.Kill();
                return;
            }

            switch (CurrentStage)
            {
                case AttackStage.PrepareSwing:
                    PrepareSwing();
                    break;
                case AttackStage.Swing:
                    ExecuteSwing();
                    break;
                default:
                    PrepareSwing();
                    break;
            }

            SetSwordPosition();
            Timer++;
        }

        // can't damage during prepare phase (should be a very brief window) (lots of the following functions are from examplemod (for now))
        public override bool? CanDamage()
        {
            if (CurrentStage == AttackStage.PrepareSwing)
                return false;
            return base.CanDamage();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Calculate origin of sword (hilt) based on orientation and offset sword rotation (as sword is angled in its sprite)
            Vector2 origin;
            float rotationOffset;
            SpriteEffects effects;

            if (Projectile.spriteDirection > 0)
            {
                origin = new Vector2(0, Projectile.height);
                rotationOffset = MathHelper.ToRadians(45f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(Projectile.width, Projectile.height);
                rotationOffset = MathHelper.ToRadians(135f);
                effects = SpriteEffects.FlipHorizontally;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, default, lightColor * Projectile.Opacity, Projectile.rotation + rotationOffset, origin, Projectile.scale, effects, 0);

            // Since we are doing a custom draw, prevent it from normally drawing
            return false;
        }

        public override void CutTiles()
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (Projectile.Size.Length() * Projectile.scale);
            Utils.PlotTileLine(start, end, 15 * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * ((Projectile.Size.Length()) * Projectile.scale);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * Projectile.scale, ref collisionPoint);
        }

        public void SetSwordPosition()
        {
            Player player = Main.player[Projectile.owner];

            Projectile.rotation = InitialAngle + Projectile.spriteDirection * Progress; // Set projectile rotation

            // Set composite arm allows you to set the rotation of the arm and stretch of the front and back arms independently
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f)); // set arm position (90 degree offset since arm starts lowered)
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2); // get position of hand

            armPosition.Y += Owner.gfxOffY;
            Projectile.Center = armPosition; // Set projectile to arm position

            Projectile.scale = Owner.GetAdjustedItemScale(Owner.HeldItem) + 1; // take into account melee size modifiers, adjust size

            Owner.heldProj = Projectile.whoAmI; // set held projectile to this projectile
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        private void PrepareSwing()
        {
            if (Timer >= prepTime)
            {
                SoundEngine.PlaySound(SoundID.Item1);
                CurrentStage = AttackStage.Swing;
            }
        }

        private void ExecuteSwing()
        {
            Player player = Main.player[Projectile.owner];
            if (CurrentAttack == AttackType.DownwardSwing)
            {
                Progress = MathHelper.SmoothStep(0, SWING_RANGE, (1f - UNWIND) * Timer / execTime);
            }
            else if (CurrentAttack == AttackType.UpwardSwing)
            {
                Progress = MathHelper.SmoothStep(0, SWING_RANGE, (1f - UNWIND) * Timer / execTime);
            }

            if (Timer >= execTime)
            {
                Projectile.Kill();
            }
        }
    }
}