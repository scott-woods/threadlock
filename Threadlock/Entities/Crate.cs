using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Crate : Entity
    {
        LootDropper _lootDropper;
        Destructable _destructable;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            var texture = Game1.Scene.Content.LoadTexture(Nez.Content.Textures.Tilesets.Dungeon_prison_props);
            var sprite = new Sprite(texture, new Rectangle(0, 0, 16, 32));
            var renderer = AddComponent(new SpriteRenderer(sprite));
            renderer.SetRenderLayer(RenderLayers.YSort);

            var collider = AddComponent(new BoxCollider(-8, 0, 16, 16));
            Flags.SetFlag(ref collider.PhysicsLayer, PhysicsLayers.Environment);

            _lootDropper = AddComponent(new LootDropper(LootTables.Crate));

            _destructable = AddComponent(new Destructable(collider, Nez.Content.Audio.Sounds.FartWithReverb));
            _destructable.OnDestruction += OnDestruction;
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _destructable.OnDestruction -= OnDestruction;
        }

        void OnDestruction()
        {
            _lootDropper.DropLoot();
            Destroy();
        }
    }
}
