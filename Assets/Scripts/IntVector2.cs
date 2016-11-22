#define VECTOR_CLOCKWISE
using UnityEngine;
using System.Collections;
using System;
namespace StageNine
{
    [Serializable]
    public class IntVector2
    {
        public enum DirectionCase
        {
            RIGHT,
            ONE,
            UP,
            LEFT,
            MINUS_ONE,
            DOWN,
            RIGHT_OR_ONE,
            ONE_OR_UP,
            UP_OR_LEFT,
            LEFT_OR_MINUS_ONE,
            MINUS_ONE_OR_DOWN,
            DOWN_OR_RIGHT,
            ZERO
        }

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

        public static IntVector2 operator *(IntVector2 v, int i)
        {
            return new IntVector2(v.x * i, v.y * i);
        }

        public static IntVector2 operator *(int i, IntVector2 v)
        {
            return v * i;
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

        public static IntVector2 GetVectorFromCase(DirectionCase dc)
        {
            switch (dc)
            {
#if VECTOR_CLOCKWISE || VECTOR_TO_RIGHT || VECTOR_TO_DOWN || VECTOR_TO_MINUS_ONE
                case DirectionCase.RIGHT_OR_ONE:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_RIGHT || VECTOR_TO_ONE || VECTOR_TO_UP
                case DirectionCase.DOWN_OR_RIGHT:
#endif
                case DirectionCase.RIGHT:
                    return IntVector2.RIGHT;
#if VECTOR_CLOCKWISE || VECTOR_TO_ONE || VECTOR_TO_RIGHT || VECTOR_TO_DOWN
                case DirectionCase.ONE_OR_UP:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_ONE || VECTOR_TO_UP || VECTOR_TO_LEFT
                case DirectionCase.RIGHT_OR_ONE:
#endif
                case DirectionCase.ONE:
                    return IntVector2.ONE;
#if VECTOR_CLOCKWISE || VECTOR_TO_UP || VECTOR_TO_ONE || VECTOR_TO_RIGHT
                case DirectionCase.UP_OR_LEFT:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_UP || VECTOR_TO_LEFT || VECTOR_TO_MINUS_ONE
                case DirectionCase.ONE_OR_UP:
#endif
                case DirectionCase.UP:
                    return IntVector2.UP;
#if VECTOR_CLOCKWISE || VECTOR_TO_LEFT || VECTOR_TO_UP || VECTOR_TO_ONE
                case DirectionCase.LEFT_OR_MINUS_ONE:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_LEFT || VECTOR_TO_MINUS_ONE || VECTOR_TO_DOWN
                case DirectionCase.UP_OR_LEFT:
#endif
                case DirectionCase.LEFT:
                    return -IntVector2.RIGHT;
#if VECTOR_CLOCKWISE || VECTOR_TO_MINUS_ONE || VECTOR_TO_LEFT || VECTOR_TO_UP
                case DirectionCase.MINUS_ONE_OR_DOWN:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_MINUS_ONE || VECTOR_TO_DOWN || VECTOR_TO_RIGHT
                case DirectionCase.LEFT_OR_MINUS_ONE:
#endif
                case DirectionCase.MINUS_ONE:
                    return -IntVector2.ONE;
#if VECTOR_CLOCKWISE || VECTOR_TO_DOWN || VECTOR_TO_MINUS_ONE || VECTOR_TO_LEFT 
                case DirectionCase.DOWN_OR_RIGHT:
#endif
#if VECTOR_COUNTERCLOCKWISE || VECTOR_TO_DOWN || VECTOR_TO_RIGHT || VECTOR_TO_ONE
                case DirectionCase.MINUS_ONE_OR_DOWN:
#endif
                case DirectionCase.DOWN:
                    return -IntVector2.UP;
                default:
                case DirectionCase.ZERO:
                    return _ZERO;
            }
        }

        public static DirectionCase GetDirectionFromVector(IntVector2 vector)
        {
            if (vector == _ZERO)
            {
                return DirectionCase.ZERO;
            }
            if(vector.x == 0)
            {
                if (vector.y > 0)
                    return DirectionCase.UP;
                else
                    return DirectionCase.DOWN;
            }
            float slope = (float)vector.y / (float)vector.x;
            if (slope > 2.0f)
            {
                if (vector.x > 0)
                    return DirectionCase.UP;
                else
                    return DirectionCase.DOWN;
            }
            if (slope == 2.0f)
            {
                if (vector.x > 0)
                    return DirectionCase.ONE_OR_UP;
                else
                    return DirectionCase.MINUS_ONE_OR_DOWN;
            }
            if (slope > .5f)
            {
                if (vector.x > 0)
                    return DirectionCase.ONE;
                else
                    return DirectionCase.MINUS_ONE;
            }
            if (slope == .5f)
            {
                if (vector.x > 0)
                    return DirectionCase.RIGHT_OR_ONE;
                else
                    return DirectionCase.LEFT_OR_MINUS_ONE;
            }
            if (slope > -1f)
            {
                if (vector.x > 0)
                    return DirectionCase.RIGHT;
                else
                    return DirectionCase.LEFT;
            }
            if (slope == -1f)
            {
                if (vector.x > 0)
                    return DirectionCase.DOWN_OR_RIGHT;
                else
                    return DirectionCase.UP_OR_LEFT;
            }
            if (vector.x > 0)
                return DirectionCase.DOWN;
            else
                return DirectionCase.UP;
        }

        #endregion
    }
}
