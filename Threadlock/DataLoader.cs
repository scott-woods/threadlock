using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock
{
    public class DataLoader
    {
        public static readonly Dictionary<string, EnemyData> EnemyDataDictionary = LoadEnemyData();
        public static readonly Dictionary<string, AnimationConfig> AnimationData = LoadAnimationData();

        static Dictionary<string, EnemyData> LoadEnemyData()
        {
            if (File.Exists("Content/Data/EnemyData.json"))
            {
                var json = File.ReadAllText("Content/Data/EnemyData.json");
                var settings = new JsonSettings();
                var enemyConfigs = Json.FromJson<Dictionary<string, EnemyData>>(json, settings);
                return enemyConfigs;
            }
            else
                throw new Exception("Could not find EnemyData.json");
        }

        static Dictionary<string, AnimationConfig> LoadAnimationData()
        {
            if (File.Exists("Content/Data/Animations.json"))
            {
                var json = File.ReadAllText("Content/Data/Animations.json");
                var animations = Json.FromJson<AnimationConfig[]>(json);

                var dict = animations.ToDictionary(a => a.Name, a => a);

                foreach (var anim in dict.Values)
                {
                    //yeah i know this is terrible, my b
                    if (anim.FrameData != null && anim.FrameData.Count > 0)
                    {
                        foreach (var kvp in anim.FrameData)
                            anim.FrameData[kvp.Key].Frame = kvp.Key;
                    }

                    ApplyInheritance(anim, dict);
                }

                return dict;
            }
            else
                throw new Exception("Could not find AnimationData.json");
        }

        static void ApplyInheritance(AnimationConfig anim, Dictionary<string, AnimationConfig> dict)
        {
            if (!string.IsNullOrWhiteSpace(anim.Base) && dict.TryGetValue(anim.Base, out var parentAnimation))
            {
                if (parentAnimation.Base != null)
                    ApplyInheritance(parentAnimation, dict);

                anim.CellWidth ??= parentAnimation.CellWidth;
                anim.CellHeight ??= parentAnimation.CellHeight;
                anim.Origin ??= parentAnimation.Origin;
                anim.Row ??= parentAnimation.Row;
                anim.Frames ??= parentAnimation.Frames;
                anim.StartFrame ??= parentAnimation.StartFrame;
                anim.Loop ??= parentAnimation.Loop;
                anim.Path ??= parentAnimation.Path;
                anim.ChainTo ??= parentAnimation.ChainTo;
                anim.FPS ??= parentAnimation.FPS;

                if (parentAnimation.FrameData != null && parentAnimation.FrameData.Count > 0)
                {
                    foreach (var kvp in parentAnimation.FrameData)
                    {
                        if (!anim.FrameData.ContainsKey(kvp.Key))
                            anim.FrameData[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
}
