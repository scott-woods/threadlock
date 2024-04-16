using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Threadlock.SceneComponents.Dungenerator.CorridorPainter;

namespace Threadlock.StaticData
{
    public static class Tiles
    {
        public static class Forge
        {
            public static class Floor
            {
                public const int StoneCenter = 202;
                public const int StoneTopLeftCorner = 176;
                public const int StoneTopEdge = 177;
                public const int StoneTopRightCorner = 178;
                public const int StoneLeftEdge = 201;
                public const int StoneRightEdge = 203;
                public const int StoneBottomLeftCorner = 226;
                public const int StoneBottomEdge = 227;
                public const int StoneBottomRightCorner = 228;
                public const int StoneTopLeftInverse = 179;
                public const int StoneTopRightInverse = 181;
                public const int StoneBottomLeftInverse = 229;
                public const int StoneBottomRightInverse = 231;
            }

            public static class Walls
            {
                public const int LeftCornerLower = 57;
                public const int LeftCornerMid = 32;
                public const int LeftCornerTop = 7;
                public const int RightCornerLower = 56;
                public const int RightCornerMid = 31;
                public const int RightCornerTop = 6;
                public const int NormalLower = 152;
                public const int NormalMid = 127;
                public const int NormalTop = 102;
                public const int LeftEdgeLower = 151;
                public const int LeftEdgeMid = 126;
                public const int LeftEdgeTop = 101;
                public const int LeftEdgeCornerLower = 125;
                public const int LeftEdgeCornerMid = 100;
                public const int LeftEdgeCornerTop = 75;
                public const int RightEdgeCornerLower = 129;
                public const int RightEdgeCornerMid = 104;
                public const int RightEdgeCornerTop = 79;
                public const int RightEdgeLower = 153;
                public const int RightEdgeMid = 128;
                public const int RightEdgeTop = 103;
                public const int RightSide = 50;
                public const int LeftSide = 54;
                public const int RightSideLeftTurn = 76;
                public const int LeftSideRightTurn = 78;
                public const int Bottom = 2;
                public const int BottomTurnLeft = 1;
                public const int BottomTurnRight = 3;
                public const int BottomRightSideTurnLeft = 26;
                public const int BottomLeftSideTurnRight = 28;
                public const int Collider = 27;
            }
        }
    }
}
