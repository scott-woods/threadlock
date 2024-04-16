using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Models
{
    public class ExtraTile
    {
        public int TileId { get; set; }
        public int RenderLayer { get; set; }
        public Vector2 Offset { get; set; }

        public ExtraTile(int id, string renderLayer, string offset)
        {
            TileId = id;
            RenderLayer = RenderLayers.GetLayerValue(renderLayer);

            var splitOffset = offset.Split(' ').Select(o => Convert.ToInt32(o)).ToList();
            if (splitOffset.Count != 2)
                return;

            Offset = new Vector2(splitOffset[0], splitOffset[1]);
        }
    }
}
