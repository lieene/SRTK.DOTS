/************************************************************************************
| File: MathX.cs                                                                    |
| Project: SRTK.MathX                                                               |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Thu Feb 20 2020                                                    |
| Modified By: Lieene Guo                                                           |
| -----                                                                             |
| MIT License                                                                       |
|                                                                                   |
| Copyright (c) 2019 Lieene@ShadeRealm                                              |
|                                                                                   |
| Permission is hereby granted, free of charge, to any person obtaining a copy of   |
| this software and associated documentation files (the "Software"), to deal in     |
| the Software without restriction, including without limitation the rights to      |
| use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies     |
| of the Software, and to permit persons to whom the Software is furnished to do    |
| so, subject to the following conditions:                                          |
|                                                                                   |
| The above copyright notice and this permission notice shall be included in all    |
| copies or substantial portions of the Software.                                   |
|                                                                                   |
| THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR        |
| IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,          |
| FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE       |
| AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER            |
| LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,     |
| OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE     |
| SOFTWARE.                                                                         |
|                                                                                   |
| -----                                                                             |
| HISTORY:                                                                          |
| Date      	By	Comments                                                        |
| ----------	---	----------------------------------------------------------      |
************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

//TODO:CS73:2019 add burst compile support
namespace SRTK
{
    public static partial class MathX
    {
        /// <summary>
        /// Prime numbers in 1 to 100
        /// </summary>
        /// <value></value>
        public static readonly int[] PrimesIn100 = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97 };

        /// <summary>
        /// π 
        /// </summary>
        public const float Pi = 3.14159265359f;
        public const float Pi_INV = 1f / Pi;
        public const double Pi_Double = 3.14159265358979323846264338328;
        public const double Pi_INV_Double = 1.0 / Pi_Double;

        /// <summary>
        /// τ 2*π
        /// </summary>
        public const float Tau = 6.28318530718f;
        public const float Tau_INV = 1f / Tau;
        public const double Tau_Double = 6.28318530717958647692528676656;
        public const double Tau_INV_Double = 1.0 / Tau_Double;

        /// <summary>
        /// ι π/2
        /// </summary>
        public const float HalfPi = 1.570796326795f;
        public const float HalfPi_INV = 1f / HalfPi;
        public const double HalfPi_Double = 1.5707963267948966192313216916398;
        public const double HalfPi_INV_Double = 1.0 / HalfPi_Double;

        /// <summary>
        /// Eular's number
        /// </summary>
        public const float e = 2.71828182846f;
        public const float e_INV = 1f / e;
        public const double e_Double = 2.7182818284590452353602874713527;
        public const double e_INV_Double = 1.0 / e_Double;
        /// <summary>
        /// ε Very small value
        /// </summary>
        public const float Epsilon = float.Epsilon;
        public const double Epsilon_Double = double.Epsilon;

        public static IEnumerable ALL<T>(this IEnumerator<T> itor) => itor as IEnumerable;

        public static void Swap<K, V>(this Dictionary<K, V> dict, K i, K j) { V t = dict[i]; dict[i] = dict[j]; dict[j] = t; }
        public static T Max<T>(T l, T r) where T : IComparable<T> => unchecked(l.CompareTo(r) >= 0 ? l : r);
        public static T Min<T>(T l, T r) where T : IComparable<T> => unchecked(l.CompareTo(r) <= 0 ? l : r);
        public static T Median<T>(T x, T y, T z) where T : IComparable<T> => unchecked(x.Clamp(y, z));
        public static T MaxAt<T>(this T value, T cap) where T : IComparable<T> => unchecked(value.CompareTo(cap) > 0 ? cap : value);
        public static T MinAt<T>(this T value, T cap) where T : IComparable<T> => unchecked(value.CompareTo(cap) < 0 ? cap : value);
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> => unchecked(value.CompareTo(min) < 0 ? min : (value.CompareTo(max) > 0 ? max : value));

        public static byte ClampToPositive(this byte value) => value;
        public static ushort ClampToPositive(this ushort value) => value;
        public static uint ClampToPositive(this uint value) => value;
        public static ulong ClampToPositive(this ulong value) => value;
        public static sbyte ClampToPositive(this sbyte value) => value < 0 ? unchecked((sbyte)0) : value;
        public static short ClampToPositive(this short value) => value < 0 ? unchecked((short)0) : value;
        public static int ClampToPositive(this int value) => value < 0 ? 0 : value;
        public static long ClampToPositive(this long value) => value < 0 ? 0 : value;
        public static float ClampToPositive(this float value) => value < 0 ? 0 : value;
        public static double ClampToPositive(this double value) => value < 0 ? 0 : value;
        public static decimal ClampToPositive(this decimal value) => value < 0 ? 0 : value;

        public static byte ClampToNegtive(this byte value) => 0;
        public static ushort ClampToNegtive(this ushort value) => 0;
        public static uint ClampToNegtive(this uint value) => 0;
        public static ulong ClampToNegtive(this ulong value) => 0;
        public static sbyte ClampToNegtive(this sbyte value) => value > 0 ? unchecked((sbyte)0) : value;
        public static short ClampToNegtive(this short value) => value > 0 ? unchecked((short)0) : value;
        public static int ClampToNegtive(this int value) => value > 0 ? 0 : value;
        public static long ClampToNegtive(this long value) => value > 0 ? 0 : value;
        public static float ClampToNegtive(this float value) => value > 0 ? 0 : value;
        public static double ClampToNegtive(this double value) => value > 0 ? 0 : value;
        public static decimal ClampToNegtive(this decimal value) => value > 0 ? 0 : value;

        public static byte Clamp01(this byte value) => value > 1 ? unchecked((byte)1) : value;
        public static ushort Clamp01(this ushort value) => value > 1 ? unchecked((ushort)1) : value;
        public static uint Clamp01(this uint value) => value > 1 ? unchecked((uint)1) : value;
        public static ulong Clamp01(this ulong value) => value > 1 ? (ulong)1 : value;
        public static sbyte Clamp01(this sbyte value) => unchecked((sbyte)(value < 0 ? 0 : (value > 1 ? 1 : value)));
        public static short Clamp01(this short value) => unchecked((short)(value < 0 ? 0 : (value > 1 ? 1 : value)));
        public static int Clamp01(this int value) => value < 0 ? 0 : (value > 1 ? 1 : value);
        public static long Clamp01(this long value) => value < 0 ? 0 : (value > 1 ? 1 : value);
        public static float Clamp01(this float value) => (value < 0 ? 0 : (value > 1 ? 1 : value));
        public static double Clamp01(this double value) => (value < 0 ? 0 : (value > 1 ? 1 : value));
        public static decimal Clamp01(this decimal value) => (value < 0 ? 0 : (value > 1 ? 1 : value));

        public static byte Sign(this byte value) => unchecked((byte)(value > 0 ? 1 : 0));
        public static byte Sign(this ushort value) => unchecked((byte)(value > 0 ? 1 : 0));
        public static byte Sign(this uint value) => unchecked((byte)(value > 0 ? 1 : 0));
        public static byte Sign(this ulong value) => unchecked((byte)(value > 0 ? 1 : 0));
        public static sbyte Sign(this sbyte value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this short value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this int value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this long value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this float value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this double value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));
        public static sbyte Sign(this decimal value) => unchecked((sbyte)(value > 0 ? 1 : (value < 0) ? -1 : 0));

        public static byte Abs(this byte value) => value;
        public static ushort Abs(this ushort value) => value;
        public static uint Abs(this uint value) => value;
        public static ulong Abs(this ulong value) => value;
        public static byte Abs(this sbyte value) => unchecked((byte)(value.Sign() * value).MaxAt<int>(sbyte.MaxValue));
        public static ushort Abs(this short value) => unchecked((ushort)(value.Sign() * value).MaxAt<int>(short.MaxValue));
        public static uint Abs(this int value) => unchecked((uint)(value.Sign() * value).MaxAt<int>(int.MaxValue));
        public static ulong Abs(this long value) => unchecked((ulong)(value.Sign() * value).MaxAt<long>(long.MaxValue));
        public static float Abs(this float value) => value.Sign() * value;
        public static double Abs(this double value) => value.Sign() * value;
        public static decimal Abs(this decimal value) => value.Sign() * value;

        public static float Lerp(this float p, float from, float to) => (from * (1 - p)) + (p * to);
        public static double Lerp(this double p, double from, double to) => (from * (1 - p)) + (p * to);

        //TODO:CS73: use unsafe code to edit memory directly
        public static int RoundToInt(this float value) => (int)Round(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static int RoundToInt(this double value) => (int)Round(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static float Round(this float value) => (float)Math.Round(value, MidpointRounding.ToEven);
        //TODO:CS73: use unsafe code to edit memory directly
        public static double Round(this double value) => Math.Round(value, MidpointRounding.ToEven);

        //TODO:CS73: use unsafe code to edit memory directly
        public static int FloorToInt(this float value) => (int)value;//OverflowException could be thrown by casting
        //TODO:CS73: use unsafe code to edit memory directly
        public static int FloorToInt(this double value) => (int)value;//OverflowException could be thrown by casting
        //TODO:CS73: use unsafe code to edit memory directly
        public static float Floor(this float value) => (float)Math.Floor(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static double Floor(this double value) => Math.Floor(value);

        //TODO:CS73: use unsafe code to edit memory directly
        public static int CeilToInt(this float value) => (int)Ceil(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static int CeilToInt(this double value) => (int)Ceil(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static float Ceil(this float value) => (float)Math.Ceiling(value);
        //TODO:CS73: use unsafe code to edit memory directly
        public static double Ceil(this double value) => Math.Ceiling(value);

        public static float RoundAway(this float value) => (float)Math.Round(value, MidpointRounding.AwayFromZero);
        public static double RoundAway(this double value) => Math.Round(value, MidpointRounding.AwayFromZero);
        public static int RoundAwayToInt(this float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
        public static int RoundAwayToInt(this double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);


        public static double RoundIn(this double value)
        {
            var r = Math.Round(value, MidpointRounding.AwayFromZero);
            if (r > 0 && r > value) r -= 1;
            return r;
        }
        public static float RoundIn(this float value) => (float)RoundIn((double)value);
        public static int RoundInToInt(this float value) => (int)RoundIn((double)value);
        public static int RoundInToInt(this double value) => (int)RoundIn(value);
    
    }
}
