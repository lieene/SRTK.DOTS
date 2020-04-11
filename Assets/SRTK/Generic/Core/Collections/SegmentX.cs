/************************************************************************************
| File: SegmentX.cs                                                                 |
| Project: SRTK.ListSegment                                                         |
| Created Date: Mon Sep 16 2019                                                     |
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SRTK
{
    public static class SegmentX
    {
        #region IList
        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> SegmentI<T>(this IListX<T> l, int offset, int capacity)
            => new Segment<T, IListX<T>>(l, offset, capacity, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> SegmentIFrom<T>(this IListX<T> l, int offset)
        {
            var count = l.Count - offset;
            return new Segment<T, IListX<T>>(l, offset, count, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> SegmentIBefore<T>(this IListX<T> l, int capacity)
            => new Segment<T, IListX<T>>(l, 0, capacity, capacity);

        //NEW--------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> NewSegI<T>(this IListX<T> l, int offset, int capacity)
            => new Segment<T, IListX<T>>(l, offset, 0, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> NewSegIFrom<T>(this IListX<T> l, int offset)
            => new Segment<T, IListX<T>>(l, offset, 0, l.Count - offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Segment<T, IListX<T>> NewSegIBefore<T>(this IListX<T> l, int capacity)
            => new Segment<T, IListX<T>>(l, 0, 0, capacity);
        //----------------------------------------------------------------------------------
        #endregion IList
        //----------------------------------------------------------------------------------
        #region Array
        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> Segment<T>(this T[] l, long offset, int capacity)
            => new ArraySeg<T>(l, offset, capacity, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> SegmentFrom<T>(this T[] l, long offset)
        {
            int count = (int)(l.LongLength - offset);
            return new ArraySeg<T>(l, offset, count, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> SegmentBefore<T>(this T[] l, int capacity)
            => new ArraySeg<T>(l, 0, capacity, capacity);

        //NEW--------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> NewSeg<T>(this T[] l, long offset, int capacity)
            => new ArraySeg<T>(l, offset, 0, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> NewSegFrom<T>(this T[] l, long offset)
            => new ArraySeg<T>(l, offset, 0, (int)(l.LongLength - offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArraySeg<T> NewSegBefore<T>(this T[] l, int capacity)
            => new ArraySeg<T>(l, 0, 0, capacity);

        //----------------------------------------------------------------------------------
        #endregion Array
    }
}