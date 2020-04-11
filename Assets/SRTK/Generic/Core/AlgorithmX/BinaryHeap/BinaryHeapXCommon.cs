/************************************************************************************
| File: BinaryHeapXCommon.cs                                                        |
| Project: SRTK.BinaryHeap                                                          |
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
| 2019-09-19	GL	static BinaryHeap compare irrelavent funtions
************************************************************************************/

using System;
using System.Collections.Generic;
using SRTK.Pool;
using System.Runtime.CompilerServices;

//TODO:CS73:2019 add burst compile support
using static SRTK.MathX;

namespace SRTK
{
    public static partial class BinaryHeapX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ValidateEmptyCheck<T>(in IListX<T> container, int heapCount, int heapOffset)
        {
            if (heapOffset < 0 || heapCount < 0 || heapOffset + heapCount > container.Count)
                throw new OverflowException("[BinaryHeapX] out range container");
            return heapCount == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ValidateEmptyCheck<T>(in T[] container, int heapCount, int heapOffset)
        {
            if (heapOffset < 0 || heapCount < 0 || heapOffset + heapCount > container.Length)
                throw new OverflowException("[BinaryHeapX] out range container");
            return heapCount == 0;
        }
        //----------------------------------------------------------------------------------
        #region Peek

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(in T[] container) => container[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(in IListX<T> container) => container[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(in T[] container, int heapOffset) => container[heapOffset];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(in IListX<T> container, int heapOffset) => container[heapOffset];

        #endregion Peek
        //-----------------------------------------------------------------------------------
        #region Peek

        public static int DropAll<T>(in T[] container) => 0;
        public static int DropAll<T>(in IListX<T> container)
        {
            container.Clear();
            return 0;
        }
        public static int DropAll<T>(in IListX<T> container, int heapCount, int heapOffset)
        {
            container.RemoveRange(heapOffset,heapCount);
            return container.Count;
        }

        #endregion Peek

        //-----------------------------------------------------------------------------------
    }
}