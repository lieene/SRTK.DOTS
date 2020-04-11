/************************************************************************************
| File: PoolEx.cs                                                                   |
| Project: SRTK.Pool                                                                |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Oct 14 2019                                                    |
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SRTK.Pool
{
    using static MathX;

    public static partial class PoolEx
    {
        public const int DefaultObjectPoolCapacity = 32;
        internal const int MinObjectPoolCapacity = 4;
        //----------------------------------------------------------------------------------
        const short VoteToRemoveSmallArray = 3;

        private struct ObjectPoolHolder<T, P>
            where T : class
            where P : IObjectPool<T>
        { public static P Pool; }

        //----------------------------------------------------------------------------------
        #region ObjectPool

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectPool<T> GrabObjectPool<T>(
            Func<T> factory,
            int capacity = DefaultObjectPoolCapacity,
            bool sync = false,
            bool trackAlloc = false) where T : class
            => ObjectPoolHolder<T, ObjectPool<T>>.Pool as ObjectPool<T> ??
                (ObjectPoolHolder<T, ObjectPool<T>>.Pool =
                new ObjectPool<T>(factory, capacity, sync, trackAlloc));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetObjectPool<T>(ObjectPool<T> pool) where T : class
            => ObjectPoolHolder<T, ObjectPool<T>>.Pool = pool;
        #endregion ObjectPool
    }
}
