using Microsoft.Xna.Framework.Graphics;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities;
using Threadlock.Helpers;

namespace Threadlock.StaticData
{
    public class Projectiles
    {
        public static ProjectileConfig PlayerGunProjectile
        {
            get
            {
                var path = Nez.Content.Textures.Characters.Player.Player_gun_projectile;
                var texture = Game1.Content.LoadTexture(path);
                var sprites = Sprite.SpritesFromAtlas(texture, 16, 16);
                return new ProjectileConfig()
                {
                    Damage = 2,
                    Speed = 350,
                    Radius = 4,
                    SpritePath = path,
                    TravelSprites = AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 2, 4),
                    BurstSprites = AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 4, 4),
                    PhysicsLayer = PhysicsLayers.PlayerHitbox,
                    HitLayers = new List<int> { PhysicsLayers.EnemyHurtbox },
                    DestroyOnWall = true
                };
            }
        }

        public static ProjectileConfig PlayerShotgunProjectile
        {
            get
            {
                var path = Nez.Content.Textures.Characters.Player.Player_gun_projectile;
                var texture = Game1.Content.LoadTexture(path);
                var sprites = Sprite.SpritesFromAtlas(texture, 16, 16);
                return new ProjectileConfig()
                {
                    Damage = 1,
                    Speed = 475,
                    Radius = 4,
                    SpritePath = path,
                    TravelSprites = AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 2, 4),
                    BurstSprites = AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 4, 4),
                    PhysicsLayer = PhysicsLayers.PlayerHitbox,
                    HitLayers = new List<int> { PhysicsLayers.EnemyHurtbox },
                    DestroyOnWall = true
                };
            }
        }
    }
}
