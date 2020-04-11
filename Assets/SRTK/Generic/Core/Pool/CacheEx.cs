/************************************************************************************
| File: CacheEx.cs                                                                  |
| Project: SRTK.Pool                                                                |
| Created Date: Wed Sep 4 2019                                                      |
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SRTK.Pool
{
    using static MathX;

    public static partial class CacheEx
    {
        public static readonly int DefaultCacheCapacity = Environment.ProcessorCount * 2;

        //TODO:CS73 this should be adjusted to a larger value as stackalloc is perfered for smaller arrays (<=1024) when availabe
        internal const ushort DefualArraySizeLog2Min = 3;
        internal const ushort DefualArraySizeMin = 1 << DefualArraySizeLog2Min;

        internal const ushort DefualArraySizeLog2Max = 10;
        internal const ushort DefualArraySizeMax = 1 << DefualArraySizeLog2Max;
        //TODO:CS73 this should be adjusted to a larger value as stackalloc is perfered for smaller arrays (<=1024) when availabe

        internal const ushort ArraySizeLog2Min = 1;
        internal const ushort ArraySizeLmtMin = 1 << ArraySizeLog2Min;
        internal const ushort ArraySizeLevelSearchCount = 2;



        //TODO:CS73 this should be adjusted to a larger value as stackalloc is perfered for smaller arrays (<=1024) when availabe
        internal const ushort DefualBufferSizeLog2Min = 8;
        internal const ushort DefualBufferSizeMin = 1 << DefualBufferSizeLog2Min;

        internal const ushort DefualBufferSizeLog2Max = 13;
        internal const ushort DefualBufferSizeMax = 1 << DefualBufferSizeLog2Max;
        //TODO:CS73 this should be adjusted to a larger value as stackalloc is perfered for smaller arrays (<=1024) when availabe

        internal const ushort BufferSizeLog2Min = 6;
        internal const ushort BufferSizeLmtMin = 1 << BufferSizeLog2Min;
        internal const ushort BufferSizeLevelSearchCount = 3;

        //----------------------------------------------------------------------------------
        const short VoteToRemoveSmallArray = 3;

        private struct CacheHolder<T, P>
            where T : class
            where P : ICache<T>
        { public static P Cache; }

        private struct ArrayCacheHolder<T, P>
            where P : IArrayCache<T>
        { public static P Cache; }

        //----------------------------------------------------------------------------------
        #region FastPool
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cache<T> GrabCache<T>(Func<T> factory, int size) where T : class
            => CacheHolder<T, Cache<T>>.Cache as Cache<T> ??
                (CacheHolder<T, Cache<T>>.Cache =
                new Cache<T>(factory, size));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cache<T> GrabCache<T>(Func<T> factory) where T : class
            => CacheHolder<T, Cache<T>>.Cache as Cache<T> ??
                (CacheHolder<T, Cache<T>>.Cache =
                new Cache<T>(factory, DefaultCacheCapacity));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetGrabCache<T>(Cache<T> cache) where T : class
            => CacheHolder<T, Cache<T>>.Cache = cache;

        #endregion FastPool
        //----------------------------------------------------------------------------------
        #region ArrayPool
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IArrayCache<T> GrabArrayCache<T>(int capacity, int maxArraySize)
            => ArrayCacheHolder<T, IArrayCache<T>>.Cache ??
                (ArrayCacheHolder<T, IArrayCache<T>>.Cache =
                new ArrayCache<T>(capacity, maxArraySize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IArrayCache<T> GrabArrayCache<T>(
            int capacity,
            ushort maxFixArraySizeLog2,
            ushort minFixArraySizeLog2,
            bool hadDynamicSize = true)
            => ArrayCacheHolder<T, IArrayCache<T>>.Cache ??
                (ArrayCacheHolder<T, IArrayCache<T>>.Cache =
                new ArrayCache<T>(capacity, maxFixArraySizeLog2, minFixArraySizeLog2, hadDynamicSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IArrayCache<T> GrabArrayCache<T>(int maxArraySize)
            => ArrayCacheHolder<T, IArrayCache<T>>.Cache ??
                (ArrayCacheHolder<T, IArrayCache<T>>.Cache =
                new ArrayCache<T>(DefaultCacheCapacity, maxArraySize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IArrayCache<T> GrabArrayCache<T>()
            => ArrayCacheHolder<T, IArrayCache<T>>.Cache ??
                (ArrayCacheHolder<T, IArrayCache<T>>.Cache =
                new ArrayCache<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetArrayCache<T>(IArrayCache<T> pool) where T : class
            => ArrayCacheHolder<T, IArrayCache<T>>.Cache = pool;
        #endregion ArrayPool

        //----------------------------------------------------------------------------------
        #region BufferPool

        internal static IBufferCache _bufferCache;
        private static object bufferCacheLock = new object();

        public static IBufferCache BufferCache
        {
            get
            {
                lock (bufferCacheLock)
                { if (_bufferCache == null) _bufferCache = new BufferCache(); }
                return _bufferCache;
            }
        }

        internal static void ReleaseCacheMemory() { if (_bufferCache != null) _bufferCache.Release(); }

        #endregion BufferPool

    }
}
