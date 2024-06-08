using Microsoft.Xna.Framework;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class AsepriteExport
    {
        [DecodeAlias("frames")]
        public List<AsepriteFrameData> Frames;

        [DecodeAlias("meta")]
        public AsepriteMeta Meta;
    }

    public class AsepriteMeta
    {
        [DecodeAlias("app")]
        public string App;

        [DecodeAlias("version")]
        public string Version;

        [DecodeAlias("image")]
        public string Image;

        [DecodeAlias("format")]
        public string Format;

        [DecodeAlias("size")]
        public JsonSize Size;

        [DecodeAlias("scale")]
        public string Scale;

        [DecodeAlias("frameTags")]
        public FrameTag[] FrameTags;

        [DecodeAlias("layers")]
        public Layer[] Layers;

        [DecodeAlias("slices")]
        public Slice[] Slices;
    }

    public class AsepriteFrameData
    {
        [DecodeAlias("filename")]
        public string Filename;

        [DecodeAlias("frame")]
        public JsonRectangle Frame;

        [DecodeAlias("rotated")]
        public bool Rotated;

        [DecodeAlias("trimmed")]
        public bool Trimmed;

        [DecodeAlias("spriteSourceSize")]
        public JsonRectangle SpriteSourceSize;

        [DecodeAlias("sourceSize")]
        public JsonSize SourceSize;

        [DecodeAlias("duration")]
        public int Duration;
    }

    public class JsonRectangle
    {
        [DecodeAlias("x")]
        public int X;

        [DecodeAlias("y")]
        public int Y;

        [DecodeAlias("w")]
        public int Width;

        [DecodeAlias("h")]
        public int Height;

        public Rectangle ToRectangle()
        {
            return new Rectangle(X, Y, Width, Height);
        }
    }

    public class JsonSize
    {
        [DecodeAlias("w")]
        public int Width;

        [DecodeAlias("h")]
        public int Height;

        public Vector2 ToVector2()
        {
            return new Vector2(Width, Height);
        }
    }

    public class FrameTag
    {
        [DecodeAlias("name")]
        public string Name;

        [DecodeAlias("from")]
        public int From;

        [DecodeAlias("to")]
        public int To;

        [DecodeAlias("direction")]
        public string Direction;
    }

    public class Layer
    {
        [DecodeAlias("name")]
        public string Name;

        [DecodeAlias("group")]
        public string Group;

        [DecodeAlias("opacity")]
        public int Opacity;

        [DecodeAlias("blendMode")]
        public string BlendMode;
    }

    public class Slice
    {
        [DecodeAlias("name")]
        public string Name;

        [DecodeAlias("color")]
        public string Color;

        [DecodeAlias("keys")]
        public SliceKey[] Keys;
    }

    public class SliceKey
    {
        [DecodeAlias("frame")]
        public int Frame;

        [DecodeAlias("bounds")]
        public JsonRectangle Bounds;
    }
}
