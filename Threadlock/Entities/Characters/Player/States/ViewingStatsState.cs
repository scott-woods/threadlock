using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.UI.Canvases;

namespace Threadlock.Entities.Characters.Player.States
{
    public class ViewingStatsState : PlayerState
    {
        Entity _uiEntity;

        public override void Begin()
        {
            base.Begin();

            _context.Idle();

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Open_stats_menu);

            _uiEntity = _context.Scene.CreateEntity("stats-ui");
            _uiEntity.AddComponent(new CharacterOverview(OnClosed));
        }

        void OnClosed()
        {
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Close_stats_menu);

            _uiEntity.Destroy();
            _uiEntity = null;
            _machine.ChangeState<Idle>();
        }
    }
}
