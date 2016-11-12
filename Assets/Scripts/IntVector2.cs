using UnityEngine;
using System.Collections;
using System;
namespace StageNine
{
    [Serializable]
    public class IntVector2
    {
        public const int HASH_FACTOR = 32768;

        private readonly static IntVector2 _RIGHT = new IntVector2(1,0);
        private readonly static IntVector2 _UP = new IntVector2(0, 1); // actually up-left for our implementation
        private readonly static IntVector2 _ONE = new IntVector2(1, 1);
        private readonly static IntVector2 _ZERO = new IntVector2(0, 0);

        private int _x;
        private int _y;

        public static IntVector2 RIGHT
        {
            get { return _RIGHT; }
        }
        public static IntVector2 UP
        {
            get { return _UP; }
        }
        public static IntVector2 ONE
        {
            get { return _ONE; }
        }
        public static IntVector2 ZERO
        {
            get { return _ZERO; }
        }

        public int x
        {
            get { return _x; }
        }
        public int y
        {
            get { return _y; }
        }

        // magnitude, in this case, is set up for hexagon manhattan distance
        // in a more standard integer vector implementation, the square of the magnitude should be used instead
        public int magnitude
        {
            get
            {
                if (x * y < 0)
                {
                    return Math.Abs(x - y);
                }

                return Math.Max(Math.Abs(x), Math.Abs(y));
            }
        }

        #region constructors

        public IntVector2()
        {
            _x = 0;
            _y = 0;
        }

        public IntVector2(int newX, int newY)
        {
            _x = newX;
            _y = newY;
        }

        public IntVector2(IntVector2 toCopy)
        {
            _x = toCopy.x;
            _y = toCopy.y;
        }

        #endregion

        #region public methods

        public static bool operator ==(IntVector2 first, IntVector2 second)
        {
            if ((object)first == null)
            {
                return (object)second == null;
            }
            if ((object)second == null)
            {
                return false;
            }
            return (first.x == second.x && first.y == second.y);
        }

        public static bool operator !=(IntVector2 first, IntVector2 second)
        {
            if ((object)first == null)
            {
                return (object)second != null;
            }
            if ((object)second == null)
            {
                return true;
            }
            return !(first.x == second.x && first.y == second.y);
        }

        public static IntVector2 operator +(IntVector2 first, IntVector2 second)
        {
            return new IntVector2(first.x + second.x, first.y + second.y);
        }

        public static IntVector2 operator -(IntVector2 first, IntVector2 second)
        {
            return new IntVector2(first.x - second.x, first.y - second.y);
        }

        public static IntVector2 operator -(IntVector2 first)
        {
            return new IntVector2(-first.x, -first.y);
        }

        public bool Equals(IntVector2 operand)
        {
            return (x == operand.x && y == operand.y);
        }

        public override bool Equals(object operand)
        {
            if (operand is IntVector2)
            {
                return this.Equals((IntVector2)operand);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (HASH_FACTOR * y + x);
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }

        #endregion
    }
}
