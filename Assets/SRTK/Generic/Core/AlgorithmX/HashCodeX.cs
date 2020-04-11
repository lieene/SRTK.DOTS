using System.Collections.Generic;
/************************************************************************************
| File: HashCodeX.cs                                                                |
| Project: SRTK.AlgorithmX                                                          |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Mar 23 2020                                                    |
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
namespace SRTK
{
    using static MathX;
    using static RandomX;
    public static class HashCodeX
    {
        //-------------------------------------------------------------------------------------
        #region HashCode
        public static readonly int HashSeed = PrimesIn100[Range(0, 10)];
        public static readonly int HashShuffle = PrimesIn100[Range(10, 20)];

        public static int NextHash => PositiveInt;
        public static int CombineTypeHash<T>(this T firstHash, params int[] combineHashs) => CombineHash(GetTypeHash<T>(), combineHashs);
        public static int CombineHash(this int firstHash, params int[] combineHashs)
        {
            int hash = 0;
            unchecked
            {
                hash = HashSeed * HashShuffle + firstHash;
                int len = combineHashs.Length;
                for (int i = 0; i < len; i++)
                    hash = hash * HashShuffle + combineHashs[i];
            }
            return hash;
        }
        #endregion HashCode
        //-------------------------------------------------------------------------------------
        #region Type HashCode

        /// <summary>
        /// A Compile time Collection of type hash using Generic feature
        /// get hash from type with only one instruction
        /// </summary>
        internal struct TypeHash<T>
        {
            internal static readonly Type type = typeof(T);
            internal static readonly int hash = nextTypeHash++;
        }

        private static int nextTypeHash = ushort.MaxValue + 1; //start from 65536

        public static int GetTypeHash<T>() => TypeHash<T>.hash;
        public static int GetTypeHash<T>(this T o) => TypeHash<T>.hash;

        #endregion Type HashCode
        //-------------------------------------------------------------------------------------
    }
}