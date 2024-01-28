using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.FSM;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.States;
using Threadlock.Helpers;

namespace Threadlock.Entities.Characters
{
    public class Player : Entity
    {
        public float MoveSpeed = 150f;
        public Vector2 SpriteOffset = new Vector2(13, -2);

        //state machine
        public StateMachine<Player> StateMachine { get; set; }

        //components
        Mover _mover { get; set; }
        SpriteAnimator _animator { get; set; }
        VelocityComponent _velocityComponent { get; set; }
        SpriteFlipper _spriteFlipper { get; set; }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _mover = AddComponent(new Mover());
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(SpriteOffset);
            _velocityComponent = AddComponent(new VelocityComponent(_mover));
            _spriteFlipper = AddComponent(new SpriteFlipper());

            AddAnimations();

            StateMachine = new StateMachine<Player>(this, new Idle());
            StateMachine.AddState(new Move());
        }

        void AddAnimations()
        {
            var texture = Game1.Content.LoadTexture(Nez.Content.Textures.Characters.Player.Sci_fi_player_with_sword);
            var noSwordTexture = Game1.Content.LoadTexture(Nez.Content.Textures.Characters.Player.Sci_fi_player_no_sword);
            var sprites = Sprite.SpritesFromAtlas(texture, 64, 65);
            var noSwordSprites = Sprite.SpritesFromAtlas(noSwordTexture, 64, 65);

            var totalCols = 13;
            var slashFps = 15;
            var thrustFps = 15;
            var rollFps = 20;

            //down
            _animator.AddAnimation($"IdleDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 0, 12, totalCols));
            _animator.AddAnimation($"IdleDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 0, 12, totalCols));
            _animator.AddAnimation($"WalkDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 1, 8, totalCols));
            _animator.AddAnimation($"WalkDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 1, 8, totalCols));
            _animator.AddAnimation($"RunDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 2, 8, totalCols));
            _animator.AddAnimation($"RunDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 2, 8, totalCols));
            _animator.AddAnimation($"ThrustDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 3, 4, totalCols), thrustFps);
            _animator.AddAnimation($"SlashDown", AnimatedSpriteHelper.GetSpriteArrayFromRange(sprites, 54, 58), slashFps);
            _animator.AddAnimation($"RollDown", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 5, 8, totalCols), rollFps);
            _animator.AddAnimation($"RollDownNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 3, 8, totalCols), rollFps);

            //side
            _animator.AddAnimation($"Idle", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 6, 12, totalCols));
            _animator.AddAnimation($"IdleNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 4, 12, totalCols));
            _animator.AddAnimation($"Walk", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 7, 8, totalCols));
            _animator.AddAnimation($"WalkNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 5, 8, totalCols));
            _animator.AddAnimation($"Run", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 8, 8, totalCols));
            _animator.AddAnimation($"RunNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 6, 8, totalCols));
            _animator.AddAnimation($"Roll", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 9, 6, totalCols), rollFps);
            _animator.AddAnimation($"RollNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 7, 6, totalCols), rollFps);
            _animator.AddAnimation($"Thrust", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 10, 4, totalCols), thrustFps);
            _animator.AddAnimation($"Slash", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 11, 5, totalCols), slashFps);

            //up
            _animator.AddAnimation($"IdleUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 12, 12, totalCols));
            _animator.AddAnimation($"IdleUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 8, 12, totalCols));
            _animator.AddAnimation($"WalkUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 13, 8, totalCols));
            _animator.AddAnimation($"WalkUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 9, 8, totalCols));
            _animator.AddAnimation($"RunUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 14, 8, totalCols));
            _animator.AddAnimation($"RunUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 10, 8, totalCols));
            _animator.AddAnimation($"ThrustUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 15, 4, totalCols), thrustFps);
            _animator.AddAnimation($"SlashUp", AnimatedSpriteHelper.GetSpriteArrayFromRange(sprites, 210, 214), slashFps);
            _animator.AddAnimation($"RollUp", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 17, 8, totalCols), rollFps);
            _animator.AddAnimation($"RollUpNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 11, 8, totalCols), rollFps);

            //death
            _animator.AddAnimation($"Die", AnimatedSpriteHelper.GetSpriteArrayByRow(sprites, 18, 13, totalCols));
            _animator.AddAnimation($"DieNoSword", AnimatedSpriteHelper.GetSpriteArrayByRow(noSwordSprites, 12, 13, totalCols));
        }

        public override void Update()
        {
            base.Update();

            StateMachine.Update(Time.DeltaTime);
        }
    }
}
