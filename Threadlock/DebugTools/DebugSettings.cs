﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.DebugTools
{
    public class DebugSettings
    {
        public static bool FreeActions { get; set; } = false;
        public static bool PlayerHurtboxEnabled { get; set; } = true;
        public static bool EnemyAIEnabled { get; set; } = true;
    }
}
