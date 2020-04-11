/************************************************************************************
| File: PowerOf2.cs                                                                 |
| Project: SRTK.MathX                                                               |
| Created Date: Mon Sep 9 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 22 2019                                                    |
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
using System.Runtime.CompilerServices;


namespace SRTK
{
    public static partial class MathX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(uint x) => (x & (x - 1)) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(ulong x) => (x & (x - 1)) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(ushort x) => (x & (x - 1)) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(byte x) => (x & (x - 1)) == 0;

        public const byte PowOf2_0 = 1 << 0, PowOf2_1 = 1 << 1, PowOf2_2 = 1 << 2, PowOf2_3 = 1 << 3, PowOf2_4 = 1 << 4, PowOf2_5 = 1 << 5, PowOf2_6 = 1 << 6, PowOf2_7 = 1 << 7;
        public const ushort PowOf2_8 = 1 << 8, PowOf2_9 = 1 << 9, PowOf2_10 = 1 << 10, PowOf2_11 = 1 << 11, PowOf2_12 = 1 << 12, PowOf2_13 = 1 << 13, PowOf2_14 = 1 << 14, PowOf2_15 = 1 << 15;
        public const uint
        PowOf2_16 = 1u << 16, PowOf2_17 = 1u << 17, PowOf2_18 = 1u << 18, PowOf2_19 = 1u << 19, PowOf2_20 = 1u << 20, PowOf2_21 = 1u << 21, PowOf2_22 = 1u << 22, PowOf2_23 = 1u << 23,
        PowOf2_24 = 1u << 24, PowOf2_25 = 1u << 25, PowOf2_26 = 1u << 26, PowOf2_27 = 1u << 27, PowOf2_28 = 1u << 28, PowOf2_29 = 1u << 29, PowOf2_30 = 1u << 30, PowOf2_31 = 1u << 31;
        public const ulong
        PowOf2_32 = 1u << 32, PowOf2_33 = 1u << 33, PowOf2_34 = 1u << 34, PowOf2_35 = 1u << 35, PowOf2_36 = 1u << 36, PowOf2_37 = 1u << 37, PowOf2_38 = 1u << 38, PowOf2_39 = 1u << 39,
        PowOf2_40 = 1u << 40, PowOf2_41 = 1u << 41, PowOf2_42 = 1u << 42, PowOf2_43 = 1u << 43, PowOf2_44 = 1u << 44, PowOf2_45 = 1u << 45, PowOf2_46 = 1u << 46, PowOf2_47 = 1u << 47,
        PowOf2_48 = 1u << 48, PowOf2_49 = 1u << 49, PowOf2_50 = 1u << 50, PowOf2_51 = 1u << 51, PowOf2_52 = 1u << 52, PowOf2_53 = 1u << 53, PowOf2_54 = 1u << 54, PowOf2_55 = 1u << 55,
        PowOf2_56 = 1u << 56, PowOf2_57 = 1u << 57, PowOf2_58 = 1u << 58, PowOf2_59 = 1u << 59, PowOf2_60 = 1u << 60, PowOf2_61 = 1u << 61, PowOf2_62 = 1u << 62, PowOf2_63 = 1u << 63;

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this byte x)
        {
            if (x < PowOf2_0) return 0;
            return x <= PowOf2_4 ?
                x <= PowOf2_2 ? x <= PowOf2_1 ? 1 : 2 : x <= PowOf2_3 ? 3 : 4 :
                x <= PowOf2_6 ? x <= PowOf2_5 ? 5 : 6 : x <= PowOf2_7 ? 7 : 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CeilingPowOf2(this byte x)
        {
            if (x < PowOf2_0) return PowOf2_0;
            return x <= PowOf2_4 ?
                x <= PowOf2_2 ?
                    x <= PowOf2_1 ? PowOf2_1 : PowOf2_2 :
                    x <= PowOf2_3 ? PowOf2_3 : PowOf2_4 :
                x <= PowOf2_6 ?
                    x <= PowOf2_5 ? PowOf2_5 : PowOf2_6 :
                    x <= PowOf2_7 ? PowOf2_7 : PowOf2_8;
        }


        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this sbyte x)
        {
            if (x == sbyte.MinValue) return -sizeof(sbyte);
            return x.Sign() * unchecked((byte)(x & sbyte.MaxValue)).CeilingLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CeilingPowOf2(this sbyte x)
        {
            if (x == sbyte.MinValue) return x;
            return unchecked((short)(x.Sign() * unchecked((byte)(x & sbyte.MaxValue)).CeilingPowOf2()));
        }


        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this byte x)
        {
            return x < PowOf2_4 ?
                x < PowOf2_2 ?
                    x < PowOf2_1 ? 0 : 1 :
                    x < PowOf2_3 ? 2 : 3 :
                x < PowOf2_6 ?
                    x < PowOf2_5 ? 4 : 5 :
                    x < PowOf2_7 ? 6 : 7;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte FloorPowOf2(this byte x)
        {
            return x < PowOf2_4 ?
                x < PowOf2_2 ?
                    x < PowOf2_1 ? PowOf2_0 : PowOf2_1 :
                    x < PowOf2_3 ? PowOf2_2 : PowOf2_3 :
                x < PowOf2_6 ?
                    x < PowOf2_5 ? PowOf2_4 : PowOf2_5 :
                    x < PowOf2_7 ? PowOf2_6 : PowOf2_7;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this sbyte x)
        {
            if (x == sbyte.MinValue) return -sizeof(sbyte);
            return x.Sign() * unchecked((byte)(x & sbyte.MaxValue)).FloorLog2();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte FloorPowOf2(this sbyte x)
        {
            if (x == sbyte.MinValue) return x;
            return unchecked((sbyte)(x.Sign() * unchecked((byte)(x & sbyte.MaxValue)).FloorPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this ushort x)
        {
            if (x < PowOf2_0) return 0;
            return x <= PowOf2_8 ?
                x <= PowOf2_4 ?
                    x <= PowOf2_2 ?
                        x <= PowOf2_1 ? 1 : 2 :
                        x <= PowOf2_3 ? 3 : 4 :
                    x <= PowOf2_6 ?
                        x <= PowOf2_5 ? 5 : 6 :
                        x <= PowOf2_7 ? 7 : 8 :
                x <= PowOf2_12 ?
                    x <= PowOf2_10 ?
                        x <= PowOf2_9 ? 9 : 10 :
                        x <= PowOf2_11 ? 11 : 12 :
                    x <= PowOf2_14 ?
                        x <= PowOf2_13 ? 13 : 14 :
                        x <= PowOf2_15 ? 15 : 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CeilingPowOf2(this ushort x)
        {
            if (x < PowOf2_0) return PowOf2_0;
            return x <= PowOf2_8 ?
                x <= PowOf2_4 ?
                    x <= PowOf2_2 ?
                        x <= PowOf2_1 ? PowOf2_1 : PowOf2_2 :
                        x <= PowOf2_3 ? PowOf2_3 : PowOf2_4 :
                    x <= PowOf2_6 ?
                        x <= PowOf2_5 ? PowOf2_5 : PowOf2_6 :
                        x <= PowOf2_7 ? PowOf2_7 : PowOf2_8 :
                x <= PowOf2_12 ?
                    x <= PowOf2_10 ?
                        x <= PowOf2_9 ? PowOf2_9 : PowOf2_10 :
                        x <= PowOf2_11 ? PowOf2_11 : PowOf2_12 :
                    x <= PowOf2_14 ?
                        x <= PowOf2_13 ? PowOf2_13 : PowOf2_14 :
                        x <= PowOf2_15 ? PowOf2_15 : PowOf2_16;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this short x)
        {
            if (x == short.MinValue) return -sizeof(short);
            return x.Sign() * unchecked((ushort)(x & short.MaxValue)).CeilingLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingPowOf2(this short x)
        {
            if (x == short.MinValue) return x;
            return unchecked((int)(x.Sign() * unchecked((ushort)(x & short.MaxValue)).CeilingPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this ushort x)
        {
            return x < PowOf2_8 ?
                x < PowOf2_4 ?
                    x < PowOf2_2 ?
                        x < PowOf2_1 ? 0 : 1 :
                        x < PowOf2_3 ? 2 : 3 :
                    x < PowOf2_6 ?
                        x < PowOf2_5 ? 4 : 5 :
                        x < PowOf2_7 ? 6 : 7 :
                x < PowOf2_12 ?
                    x < PowOf2_10 ?
                        x < PowOf2_9 ? 8 : 9 :
                        x < PowOf2_11 ? 10 : 11 :
                    x < PowOf2_14 ?
                        x < PowOf2_13 ? 12 : 13 :
                        x < PowOf2_15 ? 14 : 15;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort FloorPowOf2(this ushort x)
        {
            return x < PowOf2_8 ?
                x < PowOf2_4 ?
                    x < PowOf2_2 ?
                        x < PowOf2_1 ? PowOf2_0 : PowOf2_1 :
                        x < PowOf2_3 ? PowOf2_2 : PowOf2_3 :
                    x < PowOf2_6 ?
                        x < PowOf2_5 ? PowOf2_4 : PowOf2_5 :
                        x < PowOf2_7 ? PowOf2_6 : PowOf2_7 :
                x < PowOf2_12 ?
                    x < PowOf2_10 ?
                        x < PowOf2_9 ? PowOf2_8 : PowOf2_9 :
                        x < PowOf2_11 ? PowOf2_10 : PowOf2_11 :
                    x < PowOf2_14 ?
                        x < PowOf2_13 ? PowOf2_12 : PowOf2_13 :
                        x < PowOf2_15 ? PowOf2_14 : PowOf2_15;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this short x)
        {
            if (x == short.MinValue) return -sizeof(short);
            return x.Sign() * unchecked((ushort)(x & short.MaxValue)).FloorLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short FloorPowOf2(this short x)
        {
            if (x == short.MinValue) return x;
            return unchecked((short)(x.Sign() * unchecked((ushort)(x & short.MaxValue)).FloorPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this uint x)
        {
            if (x < PowOf2_0) return 0;
            return x <= PowOf2_16 ?
            x <= PowOf2_8 ?
                x <= PowOf2_4 ?
                    x <= PowOf2_2 ?
                        x <= PowOf2_1 ? 1 : 2 :
                        x <= PowOf2_3 ? 3 : 4 :
                    x <= PowOf2_6 ?
                        x <= PowOf2_5 ? 5 : 6 :
                        x <= PowOf2_7 ? 7 : 8 :
                x <= PowOf2_12 ?
                    x <= PowOf2_10 ?
                        x <= PowOf2_9 ? 9 : 10 :
                        x <= PowOf2_11 ? 11 : 12 :
                    x <= PowOf2_14 ?
                        x <= PowOf2_13 ? 13 : 14 :
                        x <= PowOf2_15 ? 15 : 16 :
            x <= PowOf2_24 ?
                x <= PowOf2_20 ?
                    x <= PowOf2_18 ?
                        x <= PowOf2_17 ? 17 : 18 :
                        x <= PowOf2_19 ? 19 : 20 :
                    x <= PowOf2_22 ?
                        x <= PowOf2_21 ? 21 : 22 :
                        x <= PowOf2_23 ? 23 : 24 :
                x <= PowOf2_28 ?
                    x <= PowOf2_26 ?
                        x <= PowOf2_25 ? 25 : 26 :
                        x <= PowOf2_27 ? 27 : 28 :
                    x <= PowOf2_30 ?
                        x <= PowOf2_29 ? 29 : 30 :
                        x <= PowOf2_31 ? 31 : 32;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CeilingPowOf2(this uint x)
        {
            if (x < PowOf2_0) return PowOf2_0;
            return x <= PowOf2_16 ?
            x <= PowOf2_8 ?
                x <= PowOf2_4 ?
                    x <= PowOf2_2 ?
                        x <= PowOf2_1 ? PowOf2_1 : PowOf2_2 :
                        x <= PowOf2_3 ? PowOf2_3 : PowOf2_4 :
                    x <= PowOf2_6 ?
                        x <= PowOf2_5 ? PowOf2_5 : PowOf2_6 :
                        x <= PowOf2_7 ? PowOf2_7 : PowOf2_8 :
                x <= PowOf2_12 ?
                    x <= PowOf2_10 ?
                        x <= PowOf2_9 ? PowOf2_9 : PowOf2_10 :
                        x <= PowOf2_11 ? PowOf2_11 : PowOf2_12 :
                    x <= PowOf2_14 ?
                        x <= PowOf2_13 ? PowOf2_13 : PowOf2_14 :
                        x <= PowOf2_15 ? PowOf2_15 : PowOf2_16 :
            x <= PowOf2_24 ?
                x <= PowOf2_20 ?
                    x <= PowOf2_18 ?
                        x <= PowOf2_17 ? PowOf2_17 : PowOf2_18 :
                        x <= PowOf2_19 ? PowOf2_19 : PowOf2_20 :
                    x <= PowOf2_22 ?
                        x <= PowOf2_21 ? PowOf2_21 : PowOf2_22 :
                        x <= PowOf2_23 ? PowOf2_23 : PowOf2_24 :
                x <= PowOf2_28 ?
                    x <= PowOf2_26 ?
                        x <= PowOf2_25 ? PowOf2_25 : PowOf2_26 :
                        x <= PowOf2_27 ? PowOf2_27 : PowOf2_28 :
                    x <= PowOf2_30 ?
                        x <= PowOf2_29 ? PowOf2_29 : PowOf2_30 :
                        x <= PowOf2_31 ? PowOf2_31 : PowOf2_32;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this int x)
        {
            if (x == int.MinValue) return -sizeof(int);
            return x.Sign() * unchecked((uint)(x & int.MaxValue)).CeilingLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CeilingPowOf2(this int x)
        {
            if (x == int.MinValue) return x;
            return unchecked((long)(x.Sign() * unchecked((long)(x & int.MaxValue)).CeilingPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this uint x)
        {
            return x < PowOf2_16 ?
            x < PowOf2_8 ?
                x < PowOf2_4 ?
                    x < PowOf2_2 ?
                        x < PowOf2_1 ? 0 : 1 :
                        x < PowOf2_3 ? 2 : 3 :
                    x < PowOf2_6 ?
                        x < PowOf2_5 ? 4 : 5 :
                        x < PowOf2_7 ? 6 : 7 :
                x < PowOf2_12 ?
                    x < PowOf2_10 ?
                        x < PowOf2_9 ? 8 : 9 :
                        x < PowOf2_11 ? 10 : 11 :
                    x < PowOf2_14 ?
                        x < PowOf2_13 ? 12 : 13 :
                        x < PowOf2_15 ? 14 : 15 :
            x < PowOf2_24 ?
                x < PowOf2_20 ?
                    x < PowOf2_18 ?
                        x < PowOf2_17 ? 16 : 17 :
                        x < PowOf2_19 ? 18 : 19 :
                    x < PowOf2_22 ?
                        x < PowOf2_21 ? 20 : 21 :
                        x < PowOf2_23 ? 22 : 23 :
                x < PowOf2_28 ?
                    x < PowOf2_26 ?
                        x < PowOf2_25 ? 24 : 25 :
                        x < PowOf2_27 ? 26 : 27 :
                    x < PowOf2_30 ?
                        x < PowOf2_29 ? 28 : 29 :
                        x < PowOf2_31 ? 30 : 31;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FloorPowOf2(this uint x)
        {
            return x < PowOf2_16 ?
            x < PowOf2_8 ?
                x < PowOf2_4 ?
                    x < PowOf2_2 ?
                        x < PowOf2_1 ? PowOf2_0 : PowOf2_1 :
                        x < PowOf2_3 ? PowOf2_2 : PowOf2_3 :
                    x < PowOf2_6 ?
                        x < PowOf2_5 ? PowOf2_4 : PowOf2_5 :
                        x < PowOf2_7 ? PowOf2_6 : PowOf2_7 :
                x < PowOf2_12 ?
                    x < PowOf2_10 ?
                        x < PowOf2_9 ? PowOf2_8 : PowOf2_9 :
                        x < PowOf2_11 ? PowOf2_10 : PowOf2_11 :
                    x < PowOf2_14 ?
                        x < PowOf2_13 ? PowOf2_12 : PowOf2_13 :
                        x < PowOf2_15 ? PowOf2_14 : PowOf2_15 :
            x < PowOf2_24 ?
                x < PowOf2_20 ?
                    x < PowOf2_18 ?
                        x < PowOf2_17 ? PowOf2_16 : PowOf2_17 :
                        x < PowOf2_19 ? PowOf2_18 : PowOf2_19 :
                    x < PowOf2_22 ?
                        x < PowOf2_21 ? PowOf2_20 : PowOf2_21 :
                        x < PowOf2_23 ? PowOf2_22 : PowOf2_23 :
                x < PowOf2_28 ?
                    x < PowOf2_26 ?
                        x < PowOf2_25 ? PowOf2_24 : PowOf2_25 :
                        x < PowOf2_27 ? PowOf2_26 : PowOf2_27 :
                    x < PowOf2_30 ?
                        x < PowOf2_29 ? PowOf2_28 : PowOf2_29 :
                        x < PowOf2_31 ? PowOf2_30 : PowOf2_31;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this int x)
        {
            if (x == int.MinValue) return -(sizeof(int) - 1);
            return x.Sign() * unchecked((uint)(x & int.MaxValue)).FloorLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorPowOf2(this int x)
        {
            if (x == int.MinValue) return x;
            return unchecked((int)(x.Sign() * unchecked((uint)(x & int.MaxValue)).FloorPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this ulong x)
        {
            if (x < PowOf2_0) return 0;
            return x <= PowOf2_32 ?
            x <= PowOf2_16 ?
                x <= PowOf2_8 ?
                    x <= PowOf2_4 ?
                        x <= PowOf2_2 ?
                            x <= PowOf2_1 ? 1 : 2 :
                            x <= PowOf2_3 ? 3 : 4 :
                        x <= PowOf2_6 ?
                            x <= PowOf2_5 ? 5 : 6 :
                            x <= PowOf2_7 ? 7 : 8 :
                    x <= PowOf2_12 ?
                        x <= PowOf2_10 ?
                            x <= PowOf2_9 ? 9 : 10 :
                            x <= PowOf2_11 ? 11 : 12 :
                        x <= PowOf2_14 ?
                            x <= PowOf2_13 ? 13 : 14 :
                            x <= PowOf2_15 ? 15 : 16 :
                x <= PowOf2_24 ?
                    x <= PowOf2_20 ?
                        x <= PowOf2_18 ?
                            x <= PowOf2_17 ? 17 : 18 :
                            x <= PowOf2_19 ? 19 : 20 :
                        x <= PowOf2_22 ?
                            x <= PowOf2_21 ? 21 : 22 :
                            x <= PowOf2_23 ? 23 : 24 :
                    x <= PowOf2_28 ?
                        x <= PowOf2_26 ?
                            x <= PowOf2_25 ? 25 : 26 :
                            x <= PowOf2_27 ? 27 : 28 :
                        x <= PowOf2_30 ?
                            x <= PowOf2_29 ? 29 : 30 :
                            x <= PowOf2_31 ? 31 : 32 :
            x <= PowOf2_48 ?
                x <= PowOf2_40 ?
                    x <= PowOf2_36 ?
                        x <= PowOf2_34 ?
                            x <= PowOf2_33 ? 33 : 34 :
                            x <= PowOf2_35 ? 35 : 36 :
                        x <= PowOf2_38 ?
                            x <= PowOf2_37 ? 37 : 38 :
                            x <= PowOf2_39 ? 39 : 40 :
                    x <= PowOf2_44 ?
                        x <= PowOf2_42 ?
                            x <= PowOf2_41 ? 41 : 42 :
                            x <= PowOf2_43 ? 43 : 44 :
                        x <= PowOf2_46 ?
                            x <= PowOf2_45 ? 45 : 46 :
                            x <= PowOf2_47 ? 47 : 48 :
                x <= PowOf2_56 ?
                    x <= PowOf2_52 ?
                        x <= PowOf2_50 ?
                            x <= PowOf2_49 ? 49 : 50 :
                            x <= PowOf2_51 ? 51 : 52 :
                        x <= PowOf2_54 ?
                            x <= PowOf2_53 ? 53 : 54 :
                            x <= PowOf2_55 ? 55 : 56 :
                    x <= PowOf2_60 ?
                        x <= PowOf2_58 ?
                            x <= PowOf2_57 ? 57 : 58 :
                            x <= PowOf2_29 ? 59 : 60 :
                        x <= PowOf2_62 ?
                            x <= PowOf2_61 ? 61 : 62 :
                            x <= PowOf2_63 ? 63 : 64;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CeilingPowOf2(this ulong x)
        {
            if (x > PowOf2_63) throw new OverflowException();
            return x <= PowOf2_32 ?
            x <= PowOf2_16 ?
                x <= PowOf2_8 ?
                    x <= PowOf2_4 ?
                        x <= PowOf2_2 ?
                            x <= PowOf2_1 ? PowOf2_1 : PowOf2_2 :
                            x <= PowOf2_3 ? PowOf2_3 : PowOf2_4 :
                        x <= PowOf2_6 ?
                            x <= PowOf2_5 ? PowOf2_5 : PowOf2_6 :
                            x <= PowOf2_7 ? PowOf2_7 : PowOf2_8 :
                    x <= PowOf2_12 ?
                        x <= PowOf2_10 ?
                            x <= PowOf2_9 ? PowOf2_9 : PowOf2_10 :
                            x <= PowOf2_11 ? PowOf2_11 : PowOf2_12 :
                        x <= PowOf2_14 ?
                            x <= PowOf2_13 ? PowOf2_13 : PowOf2_14 :
                            x <= PowOf2_15 ? PowOf2_15 : PowOf2_16 :
                x <= PowOf2_24 ?
                    x <= PowOf2_20 ?
                        x <= PowOf2_18 ?
                            x <= PowOf2_17 ? PowOf2_17 : PowOf2_18 :
                            x <= PowOf2_19 ? PowOf2_19 : PowOf2_20 :
                        x <= PowOf2_22 ?
                            x <= PowOf2_21 ? PowOf2_21 : PowOf2_22 :
                            x <= PowOf2_23 ? PowOf2_23 : PowOf2_24 :
                    x <= PowOf2_28 ?
                        x <= PowOf2_26 ?
                            x <= PowOf2_25 ? PowOf2_25 : PowOf2_26 :
                            x <= PowOf2_27 ? PowOf2_27 : PowOf2_28 :
                        x <= PowOf2_30 ?
                            x <= PowOf2_29 ? PowOf2_29 : PowOf2_30 :
                            x <= PowOf2_31 ? PowOf2_31 : PowOf2_32 :
            x <= PowOf2_48 ?
                x <= PowOf2_40 ?
                    x <= PowOf2_36 ?
                        x <= PowOf2_34 ?
                            x <= PowOf2_33 ? PowOf2_33 : PowOf2_34 :
                            x <= PowOf2_35 ? PowOf2_35 : PowOf2_36 :
                        x <= PowOf2_38 ?
                            x <= PowOf2_37 ? PowOf2_37 : PowOf2_38 :
                            x <= PowOf2_39 ? PowOf2_39 : PowOf2_40 :
                    x <= PowOf2_44 ?
                        x <= PowOf2_42 ?
                            x <= PowOf2_41 ? PowOf2_41 : PowOf2_42 :
                            x <= PowOf2_43 ? PowOf2_43 : PowOf2_44 :
                        x <= PowOf2_46 ?
                            x <= PowOf2_45 ? PowOf2_45 : PowOf2_46 :
                            x <= PowOf2_47 ? PowOf2_47 : PowOf2_48 :
                x <= PowOf2_56 ?
                    x <= PowOf2_52 ?
                        x <= PowOf2_50 ?
                            x <= PowOf2_49 ? PowOf2_49 : PowOf2_50 :
                            x <= PowOf2_51 ? PowOf2_51 : PowOf2_52 :
                        x <= PowOf2_54 ?
                            x <= PowOf2_53 ? PowOf2_53 : PowOf2_54 :
                            x <= PowOf2_55 ? PowOf2_55 : PowOf2_56 :
                    x <= PowOf2_60 ?
                        x <= PowOf2_58 ?
                            x <= PowOf2_57 ? PowOf2_57 : PowOf2_58 :
                            x <= PowOf2_29 ? PowOf2_59 : PowOf2_60 :
                        x <= PowOf2_62 ?
                            x <= PowOf2_61 ? PowOf2_61 : PowOf2_62 :
                                PowOf2_63;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not smaller than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Ceiling()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilingLog2(this long x)
        {
            if (x == long.MinValue) return -sizeof(long);
            return x.Sign() * unchecked((ulong)(x & long.MaxValue)).CeilingLog2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CeilingPowOf2(this long x)
        {
            if (x == long.MinValue) return x;
            return unchecked((long)(x.Sign() * unchecked((long)(x & long.MaxValue)).CeilingPowOf2()));
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this ulong x)
        {
            return x < PowOf2_32 ?
            x < PowOf2_16 ?
                x < PowOf2_8 ?
                    x < PowOf2_4 ?
                        x < PowOf2_2 ?
                            x < PowOf2_1 ? 0 : 1 :
                            x < PowOf2_3 ? 2 : 3 :
                        x < PowOf2_6 ?
                            x < PowOf2_5 ? 4 : 5 :
                            x < PowOf2_7 ? 6 : 7 :
                    x < PowOf2_12 ?
                        x < PowOf2_10 ?
                            x < PowOf2_9 ? 8 : 9 :
                            x < PowOf2_11 ? 10 : 11 :
                        x < PowOf2_14 ?
                            x < PowOf2_13 ? 12 : 13 :
                            x < PowOf2_15 ? 14 : 15 :
                x < PowOf2_24 ?
                    x < PowOf2_20 ?
                        x < PowOf2_18 ?
                            x < PowOf2_17 ? 16 : 17 :
                            x < PowOf2_19 ? 18 : 19 :
                        x < PowOf2_22 ?
                            x < PowOf2_21 ? 20 : 21 :
                            x < PowOf2_23 ? 22 : 23 :
                    x < PowOf2_28 ?
                        x < PowOf2_26 ?
                            x < PowOf2_25 ? 24 : 25 :
                            x < PowOf2_27 ? 26 : 27 :
                        x < PowOf2_30 ?
                            x < PowOf2_29 ? 28 : 29 :
                            x < PowOf2_31 ? 30 : 312 :
            x < PowOf2_48 ?
                x < PowOf2_40 ?
                    x < PowOf2_36 ?
                        x < PowOf2_34 ?
                            x < PowOf2_33 ? 32 : 33 :
                            x < PowOf2_35 ? 34 : 35 :
                        x < PowOf2_38 ?
                            x < PowOf2_37 ? 36 : 37 :
                            x < PowOf2_39 ? 38 : 39 :
                    x < PowOf2_44 ?
                        x < PowOf2_42 ?
                            x < PowOf2_41 ? 40 : 41 :
                            x < PowOf2_43 ? 42 : 43 :
                        x < PowOf2_46 ?
                            x < PowOf2_45 ? 44 : 45 :
                            x < PowOf2_47 ? 46 : 47 :
                x < PowOf2_56 ?
                    x < PowOf2_52 ?
                        x < PowOf2_50 ?
                            x < PowOf2_49 ? 48 : 49 :
                            x < PowOf2_51 ? 50 : 51 :
                        x < PowOf2_54 ?
                            x < PowOf2_53 ? 52 : 53 :
                            x < PowOf2_55 ? 54 : 55 :
                    x < PowOf2_60 ?
                        x < PowOf2_58 ?
                            x < PowOf2_57 ? 56 : 57 :
                            x < PowOf2_29 ? 58 : 59 :
                        x < PowOf2_62 ?
                            x < PowOf2_61 ? 60 : 61 :
                            x < PowOf2_63 ? 62 : 63;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong FloorPowOf2(this ulong x)
        {
            return x < PowOf2_32 ?
            x < PowOf2_16 ?
                x < PowOf2_8 ?
                    x < PowOf2_4 ?
                        x < PowOf2_2 ?
                            x < PowOf2_1 ? PowOf2_0 : PowOf2_1 :
                            x < PowOf2_3 ? PowOf2_2 : PowOf2_3 :
                        x < PowOf2_6 ?
                            x < PowOf2_5 ? PowOf2_4 : PowOf2_5 :
                            x < PowOf2_7 ? PowOf2_6 : PowOf2_7 :
                    x < PowOf2_12 ?
                        x < PowOf2_10 ?
                            x < PowOf2_9 ? PowOf2_8 : PowOf2_9 :
                            x < PowOf2_11 ? PowOf2_10 : PowOf2_11 :
                        x < PowOf2_14 ?
                            x < PowOf2_13 ? PowOf2_12 : PowOf2_13 :
                            x < PowOf2_15 ? PowOf2_14 : PowOf2_15 :
                x < PowOf2_24 ?
                    x < PowOf2_20 ?
                        x < PowOf2_18 ?
                            x < PowOf2_17 ? PowOf2_16 : PowOf2_17 :
                            x < PowOf2_19 ? PowOf2_18 : PowOf2_19 :
                        x < PowOf2_22 ?
                            x < PowOf2_21 ? PowOf2_20 : PowOf2_21 :
                            x < PowOf2_23 ? PowOf2_22 : PowOf2_23 :
                    x < PowOf2_28 ?
                        x < PowOf2_26 ?
                            x < PowOf2_25 ? PowOf2_24 : PowOf2_25 :
                            x < PowOf2_27 ? PowOf2_26 : PowOf2_27 :
                        x < PowOf2_30 ?
                            x < PowOf2_29 ? PowOf2_28 : PowOf2_29 :
                            x < PowOf2_31 ? PowOf2_30 : PowOf2_31 :
            x < PowOf2_48 ?
                x < PowOf2_40 ?
                    x < PowOf2_36 ?
                        x < PowOf2_34 ?
                            x < PowOf2_33 ? PowOf2_32 : PowOf2_33 :
                            x < PowOf2_35 ? PowOf2_34 : PowOf2_35 :
                        x < PowOf2_38 ?
                            x < PowOf2_37 ? PowOf2_36 : PowOf2_37 :
                            x < PowOf2_39 ? PowOf2_38 : PowOf2_39 :
                    x < PowOf2_44 ?
                        x < PowOf2_42 ?
                            x < PowOf2_41 ? PowOf2_40 : PowOf2_41 :
                            x < PowOf2_43 ? PowOf2_42 : PowOf2_43 :
                        x < PowOf2_46 ?
                            x < PowOf2_45 ? PowOf2_44 : PowOf2_45 :
                            x < PowOf2_47 ? PowOf2_46 : PowOf2_47 :
                x < PowOf2_56 ?
                    x < PowOf2_52 ?
                        x < PowOf2_50 ?
                            x < PowOf2_49 ? PowOf2_48 : PowOf2_49 :
                            x < PowOf2_51 ? PowOf2_50 : PowOf2_51 :
                        x < PowOf2_54 ?
                            x < PowOf2_53 ? PowOf2_52 : PowOf2_53 :
                            x < PowOf2_55 ? PowOf2_54 : PowOf2_55 :
                    x < PowOf2_60 ?
                        x < PowOf2_58 ?
                            x < PowOf2_57 ? PowOf2_56 : PowOf2_57 :
                            x < PowOf2_29 ? PowOf2_58 : PowOf2_59 :
                        x < PowOf2_62 ?
                            x < PowOf2_61 ? PowOf2_60 : PowOf2_61 :
                            x < PowOf2_63 ? PowOf2_62 : PowOf2_63;
        }

        /// <summary>
        /// Get the minimal power p of 2, that 2^p is not geater than the value
        /// </summary>
        /// <param name="x">value to test</param>
        /// <returns>Log2(value).Floor()</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorLog2(this long x)
        {
            if (x == long.MinValue) return -sizeof(int) - 1;
            return x.Sign() * unchecked((ulong)(x & long.MaxValue)).FloorLog2();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FloorPowOf2(this long x)
        {
            if (x == long.MinValue) return -sizeof(int) - 1;
            return unchecked((long)(x.Sign() * unchecked((long)(x & long.MaxValue)).FloorPowOf2()));
        }
    }
}