using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.SaveData;

namespace Threadlock.Models
{
    public class LootConfig
    {
        //animator
        public string TexturePath { get; set; }
        public int CellWidth { get; set; } = 16;
        public int CellHeight { get; set; } = 16;
        public int StartCell { get; set; }
        public int EndCell { get; set; }

        //sound
        public string PickupSoundPath { get; set; }

        //misc
        public bool Magnetized { get; set; }
        public float DelayBeforeEnabled { get; set; } = 1.5f;
        public float Radius { get; set; } = 8;
        public Action HandlePickup { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;


        public static LootConfig HealthOrb
        {
            get
            {
                return new LootConfig()
                {
                    TexturePath = Nez.Content.Textures.Drops.Health_orb,
                    PickupSoundPath = Nez.Content.Audio.Sounds.Burp,
                    Magnetized = false,
                    HandlePickup = () =>
                    {
                        if (Player.Instance.TryGetComponent<HealthComponent>(out var hc))
                            hc.Health += (int)(hc.MaxHealth * .15f);
                    },
                    Scale = new Vector2(.5f, .5f)
                };
            }
        }

        public static LootConfig Coin
        {
            get
            {
                return new LootConfig()
                {
                    TexturePath = Nez.Content.Textures.Drops.CollectablesSheet,
                    PickupSoundPath = Nez.Content.Audio.Sounds.Dollah_pickup,
                    Magnetized = true,
                    HandlePickup = () =>
                    {
                        PlayerData.Instance.Dollahs += 1;
                    },
                    StartCell = 0,
                    EndCell = 5
                };
            }
        }

        public static LootConfig BigCoin
        {
            get
            {
                return new LootConfig()
                {
                    TexturePath = Nez.Content.Textures.Drops.CollectablesSheet,
                    PickupSoundPath = Nez.Content.Audio.Sounds.Dollah_pickup,
                    Magnetized = false,
                    HandlePickup = () =>
                    {
                        PlayerData.Instance.Dollahs += 5;
                    },
                    StartCell = 12,
                    EndCell = 19
                };
            }
        }

        public static LootConfig Dust
        {
            get
            {
                return new LootConfig()
                {
                    TexturePath = Nez.Content.Textures.Drops.CollectablesSheet,
                    PickupSoundPath = Nez.Content.Audio.Sounds.Dust_pickup,
                    Magnetized = true,
                    HandlePickup = () =>
                    {
                        PlayerData.Instance.Dust += 1;
                    },
                    StartCell = 35,
                    EndCell = 41,
                    Scale = new Vector2(.5f, .5f)
                };
            }
        }
    }
}
