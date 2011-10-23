using System;

namespace Zepheus.Util
{
    public sealed class Vector2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2()
        {
        }

        public Vector2(Vector2 pBase)
        {
            X = pBase.X;
            Y = pBase.Y;
        }

        public static float DistanceSquared(Vector2 value1, Vector2 value2)
        {
            float num2 = value1.X - value2.X;
            float num = value1.Y - value2.Y;
            return ((num2 * num2) + (num * num));
        }

        public static double Distance(int x1, int x2, int y1, int y2)
        {
            int xdelta = x2 - x1;
            int ydelta = y2 - y1;
            int pyth = (xdelta * xdelta) + (ydelta * ydelta);
            return Math.Sqrt(pyth);
        }

        public static float Distance(Vector2 value1, Vector2 value2)
        {
            float num2 = value1.X - value2.X;
            float num = value1.Y - value2.Y;
            float num3 = (num2 * num2) + (num * num);
            return (float)Math.Sqrt((double)num3);
        }

        public static Vector2 operator +(Vector2 value1, Vector2 value2)
        {
            Vector2 vector = new Vector2();
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            return vector;
        }

        public static Vector2 operator -(Vector2 value1, Vector2 value2)
        {
            Vector2 vector = new Vector2();
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            return vector;
        }

        public static Vector2 Zero { get { return new Vector2(); } }

        public override bool Equals(object obj)
        {
            if (obj is Vector2)
            {
                var t = obj as Vector2;
                return t.X == this.X && t.Y == this.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // lulz?
            return this.X + (this.Y * 30); // Should be enough
        }

        public static Vector2 GetRandomSpotAround(Random pRandomizer, Vector2 pBase, int pRadius)
        {
            int xmin = pBase.X - pRadius, xmax = pBase.X + pRadius;
            int ymin = pBase.Y - pRadius, ymax = pBase.Y + pRadius;
            return new Vector2(pRandomizer.Next(xmin, xmax), pRandomizer.Next(ymin, ymax));
        }
    }
}
