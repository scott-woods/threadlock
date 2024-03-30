using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.StaticData
{
    public class HitEffectModel
    {
        public string EffectPath;
        public int CellWidth;
        public int CellHeight;

        public HitEffectModel(string effectPath, int cellWidth, int cellHeight)
        {
            EffectPath = effectPath;
            CellWidth = cellWidth;
            CellHeight = cellHeight;
        }
    }

    public class HitEffects
    {
        public static HitEffectModel Hit1 { get => new(Nez.Content.Textures.Effects.Hit1, 82, 65); }
        public static HitEffectModel Hit2 { get => new(Nez.Content.Textures.Effects.Hit2, 82, 65); }
        public static HitEffectModel Hit3 { get => new(Nez.Content.Textures.Effects.Hit3, 82, 65); }
        public static HitEffectModel Hit4 { get => new(Nez.Content.Textures.Effects.Hit4, 82, 65); }
        public static HitEffectModel Lightning1 { get => new(Nez.Content.Textures.Effects.Electric_hit_1, 82, 65); }
        public static HitEffectModel Lightning2 { get => new(Nez.Content.Textures.Effects.Electric_hit_2, 82, 65); }
    }
}
