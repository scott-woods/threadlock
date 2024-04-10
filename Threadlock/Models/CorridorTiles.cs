using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class CorridorTiles
    {
        Dictionary<string, List<int>> _tileVariants = new Dictionary<string, List<int>>();

        public int FloorCenter { get => GetRandomTile("FloorCenter"); set => SetTile("FloorCenter", value); }
        public int FloorTopLeft { get => GetRandomTile("FloorTopLeft"); set => SetTile("FloorTopLeft", value); }
        public int FloorTopEdge { get => GetRandomTile("FloorTopEdge"); set => SetTile("FloorTopEdge", value); }
        public int FloorTopRight { get => GetRandomTile("FloorTopRight"); set => SetTile("FloorTopRight", value); }
        public int FloorRightEdge { get => GetRandomTile("FloorRightEdge"); set => SetTile("FloorRightEdge", value); }
        public int FloorBottomRight { get => GetRandomTile("FloorBottomRight"); set => SetTile("FloorBottomRight", value); }
        public int FloorBottomEdge { get => GetRandomTile("FloorBottomEdge"); set => SetTile("FloorBottomEdge", value); }
        public int FloorBottomLeft { get => GetRandomTile("FloorBottomLeft"); set => SetTile("FloorBottomLeft", value); }
        public int FloorLeftEdge { get => GetRandomTile("FloorLeftEdge"); set => SetTile("FloorLeftEdge", value); }
        public int FloorTopLeftInverse { get => GetRandomTile("FloorTopLeftInverse"); set => SetTile("FloorTopLeftInverse", value); }
        public int FloorTopRightInverse { get => GetRandomTile("FloorTopRightInverse"); set => SetTile("FloorTopRightInverse", value); }
        public int FloorBottomRightInverse { get => GetRandomTile("FloorBottomRightInverse"); set => SetTile("FloorBottomRightInverse", value); }
        public int FloorBottomLeftInverse { get => GetRandomTile("FloorBottomLeftInverse"); set => SetTile("FloorBottomLeftInverse", value); }
        public int WallLeftEdgeCornerLower { get => GetRandomTile("WallLeftEdgeCornerLower"); set => SetTile("WallLeftEdgeCornerLower", value); }
        public int WallLeftEdgeCornerMid { get => GetRandomTile("WallLeftEdgeCornerMid"); set => SetTile("WallLeftEdgeCornerMid", value); }
        public int WallLeftEdgeCornerTop { get => GetRandomTile("WallLeftEdgeCornerTop"); set => SetTile("WallLeftEdgeCornerTop", value); }
        public int WallRightEdgeCornerLower { get => GetRandomTile("WallRightEdgeCornerLower"); set => SetTile("WallRightEdgeCornerLower", value); }
        public int WallRightEdgeCornerMid { get => GetRandomTile("WallRightEdgeCornerMid"); set => SetTile("WallRightEdgeTop", value); }
        public int WallRightEdgeCornerTop { get => GetRandomTile("WallRightEdgeCornerTop"); set => SetTile("WallRightEdgeCornerTop", value); }
        public int WallLeftEdgeLower { get => GetRandomTile("WallLeftEdgeLower"); set => SetTile("WallLeftEdgeLower", value); }
        public int WallLeftEdgeMid { get => GetRandomTile("WallLeftEdgeMid"); set => SetTile("WallLeftEdgeMid", value); }
        public int WallLeftEdgeTop { get => GetRandomTile("WallLeftEdgeTop"); set => SetTile("WallLeftEdgeTop", value); }
        public int WallRightEdgeLower { get => GetRandomTile("WallRightEdgeLower"); set => SetTile("WallRightEdgeLower", value); }
        public int WallRightEdgeMid { get => GetRandomTile("WallRightEdgeMid"); set => SetTile("WallRightEdgeMid", value); }
        public int WallRightEdgeTop { get => GetRandomTile("WallRightEdgeTop"); set => SetTile("WallRightEdgeTop", value); }
        public int WallRightSide { get => GetRandomTile("WallRightSide"); set => SetTile("WallRightSide", value); }
        public int WallLeftSide { get => GetRandomTile("WallLeftSide"); set => SetTile("WallLeftSide", value); }
        public int WallBottomLeftCorner { get => GetRandomTile("WallBottomLeftCorner"); set => SetTile("WallBottomLeftCorner", value); }
        public int FrontBottomLeftCorner { get => GetRandomTile("FrontBottomLeftCorner"); set => SetTile("FrontBottomLeftCorner", value); }
        public int WallBottomRightCorner { get => GetRandomTile("WallBottomRightCorner"); set => SetTile("WallBottomRightCorner", value); }
        public int FrontBottomRightCorner { get => GetRandomTile("FrontBottomRightCorner"); set => SetTile("FrontBottomRightCorner", value); }
        public int WallRightSideLeftTurn { get => GetRandomTile("WallRightSideLeftTurn"); set => SetTile("WallRightSideLeftTurn", value); }
        public int WallLeftSideRightTurn { get => GetRandomTile("WallLeftSideRightTurn"); set => SetTile("WallLeftSideRightTurn", value); }
        public int WallCenter { get => GetRandomTile("WallCenter"); set => SetTile("WallCenter", value); }
        public int FrontTopEdge { get => GetRandomTile("FrontTopEdge"); set => SetTile("FrontTopEdge", value); }
        public int FrontTopLeftInverse { get => GetRandomTile("FrontTopLeftInverse"); set => SetTile("FrontTopLeftInverse", value); }
        public int FrontTopRightInverse { get => GetRandomTile("FrontTopRightInverse"); set => SetTile("FrontTopRightInverse", value); }

        int GetRandomTile(string tileType)
        {
            if (_tileVariants.ContainsKey(tileType))
            {
                var variants = _tileVariants[tileType];
                return variants.RandomItem();
            }

            return 0;
        }

        void SetTile(string tileType, int tileId)
        {
            if (!_tileVariants.ContainsKey(tileType))
                _tileVariants[tileType] = new List<int>();
            _tileVariants[tileType].Add(tileId);
        }
    }
}
