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
    public class LongswordSwing : ModProjectile
    {
        // const values will go below here based on moveset, will require tweaking, may need more based on future moves added
        private const float OVERHEAD_SWING_RANGE = 1.33f * (float)Math.PI; // angle overhead swing covers
        private const float OVERHEAD_BEFORE_ATK = 0.15f; // how much of overhead swing happens before it can do damage
        private const float OVERHEAD_DELAY = 0.7f; // how long the sword is held up until it is swung (on overhead slash I)
        private const float UPWARD_SWING_RANGE = 2.5f; // angle upward swing covers, should be a little less than overhead
        private const float UPWARD_BEFORE_ATK = 0.1f; // how much of upward swing happens before it can do damage
        private const float UNWIND = 0.4f; // how long until the attack is over

        private enum AttackType
        {
            OverheadSlash,

            OverheadSlashII, // overhead slash II is identical to I except it has no delay

            Stab,

            UpwardSlash,
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

        public override string Texture => $"Terraria/Images/Item_{ItemID.Katana}"; // uses katana texture for now
        private Player Owner => Main.player[Projectile.owner];

        private float prepTime => 10f / Owner.GetTotalAttackSpeed(Projectile.DamageType); // amount of time in prep (pre-swing)
        private float execTime => 12f / Owner.GetTotalAttackSpeed(Projectile.DamageType); // amount of time in actual swing
        private float stabTimer = 1f; // stab timer

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true; // i have no idea what this does, example mod does it
        }

        public override void SetDefaults()
        {
            Projectile.width = 50; // hitbox width and height - may need adjusting
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.timeLeft = 10000; // time for projectile to expire, may need adjustment (but probably not)
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // proj does not collide with tiles, may want to adjust this for accuracy to MH
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ownerHitCheck = true; // cannot hit things through tiles
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1; // this determines which way the sword faces based on x position of mouse

            if (CurrentAttack == AttackType.OverheadSlash || CurrentAttack == AttackType.OverheadSlashII)
            {
                //Main.NewText("Overhead", 255, 255, 255); // debug text prints to gamechat to tell me what attack should be happening
                InitialAngle = Projectile.spriteDirection == 1 ? 4.19f : 5.24f;
            }
            else if (CurrentAttack == AttackType.UpwardSlash)
            {
                //Main.NewText("Upward", 255, 255, 255);
                InitialAngle = Projectile.spriteDirection == 1 ? 0.3f : 2.9f; // to adjust angle: first number (facing right) lower = weapon higher, second number lower = weapon lower
                Projectile.spriteDirection *= -1;
            }
            else if (CurrentAttack == AttackType.Stab)
            {
                //Main.NewText("Stab", 255, 255, 255);
                InitialAngle = Projectile.spriteDirection == 1 ? 6.28f : 3.14f;
                stabTimer = 1f;
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
            Owner.itemAnimation = CurrentAttack == AttackType.UpwardSlash ? 50 : 15; // changing these will extend the length of time it takes to swing again
            Owner.itemTime = CurrentAttack == AttackType.UpwardSlash ? 50 : 15;      // always keep them the same
                                                                                     // also checks if upward slash to make doing a full combo have a cooldown
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

            if (CurrentAttack != AttackType.Stab)
            {
                armPosition.Y += Owner.gfxOffY;
                Projectile.Center = armPosition; // Set projectile to arm position
            } else
            {
                Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
                if (Projectile.spriteDirection > 0)
                {
                    playerCenter.X += stabTimer;
                }
                else
                {
                    playerCenter.X -= stabTimer;
                }
                stabTimer *= 1.13f; // how much stabtimer increases, resulting in how far (and fast) sword travels on stab. making this any higher than 1.2 results in silly behavior
                Projectile.Center = playerCenter;
            }
            Projectile.scale = Owner.GetAdjustedItemScale(Owner.HeldItem) + 2; // take into account melee size modifiers, adjust size

            Owner.heldProj = Projectile.whoAmI; // set held projectile to this projectile
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        private void PrepareSwing()
        {
            if (CurrentAttack == AttackType.OverheadSlash)
            {
                if (Timer >= prepTime + OVERHEAD_DELAY)
                {
                    SoundEngine.PlaySound(SoundID.Item71);
                    CurrentStage = AttackStage.Swing;
                }
            } else
            {
                if (Timer >= prepTime)
                {
                    SoundEngine.PlaySound(SoundID.Item71);
                    CurrentStage = AttackStage.Swing;
                }
            }
        }

        private void ExecuteSwing()
        {
            Player player = Main.player[Projectile.owner];
            if (CurrentAttack == AttackType.OverheadSlash || CurrentAttack == AttackType.OverheadSlashII)
            {
                Progress = MathHelper.SmoothStep(0, OVERHEAD_SWING_RANGE, (1f - UNWIND) * Timer / execTime);
            }
            else if (CurrentAttack == AttackType.UpwardSlash)
            {
                Progress = MathHelper.SmoothStep(0, UPWARD_SWING_RANGE, (1f - UNWIND) * Timer / execTime);
            }

            if (Timer >= execTime)
            {
                Projectile.Kill();
            }
        }
    }
}