/************************************************************************************
| File: BinaryHeapXInternalIComparable.cs                                           |
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
| 2019-09-19	GL	static BinaryHeap internal functions by IComparable<T>
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
        //-------------------------------------------------------------------------------------
        #region internal
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // internal static int RePushCache<T>(in IListX<T> container, int heapCount, int heapOffset, int cacheOffset) where T : IComparable<T>
        // {
        //     int curIdx = heapOffset + heapCount++;//cur heap index, initilized at heap item index at heap end
        //     container[curIdx] = container[curIdx + cacheOffset];
        //     int parentIdx;
        //     while (curIdx > heapOffset)
        //     {
        //         parentIdx = ((curIdx - heapOffset - 1) >> 1) + heapOffset;//parent index
        //         if (SmallerSwap(container, curIdx, parentIdx) < 0) curIdx = parentIdx;
        //         else break;
        //     }
        //     return heapCount;
        // }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // internal static (T,int) PopAt<T>(in IListX<T> container, int index, int heapCount, int heapOffset) where T : IComparable<T>
        // {
        //     heapCount--;
        //     int rawIdx = heapOffset + index;
        //     T item = container[rawIdx];//take de-heap item
        //     if (index == heapCount) return (item,heapCount);
        //     int heapRangeEnd = heapOffset + heapCount;//reduce calculation
        //     container[rawIdx] = container[heapRangeEnd];//move last heap item to de-heap position
        //     int offsetL = heapOffset + 1;//reduce calculation
        //     int offsetR = heapOffset + 2;//reduce calculation
        //     int parentIdx;
        //     if (index > 0)
        //     {
        //         parentIdx = heapOffset + ((index - 1) >> 1);
        //         var dir = SmallerSwap(container, rawIdx, parentIdx);
        //         if (dir == 0) return (item,heapCount);
        //         else if (dir < 0)
        //         {
        //             rawIdx = parentIdx;
        //             while (rawIdx > heapOffset)
        //             {
        //                 parentIdx = heapOffset + ((rawIdx - heapOffset - 1) >> 1);//parent index
        //                 if (SmallerSwap(container, rawIdx, parentIdx) < 0) rawIdx = parentIdx;
        //                 else break;
        //             }
        //             return (item,heapCount);
        //         }
        //     }

        //     while (true)
        //     {
        //         int leftChildIdx = (index << 1) + offsetL;
        //         int rightChildIdx = (index << 1) + offsetR;
        //         parentIdx = rawIdx;
        //         if (leftChildIdx < heapRangeEnd)
        //         {
        //             if (container[leftChildIdx].CompareTo(container[rawIdx]) < 0)
        //                 rawIdx = leftChildIdx;
        //         }

        //         if (rightChildIdx < heapRangeEnd)
        //         {
        //             if (container[rightChildIdx].CompareTo(container[rawIdx]) < 0)
        //                 rawIdx = rightChildIdx;
        //         }

        //         if (rawIdx == parentIdx) break;
        //         else
        //         {
        //             Swap(container, rawIdx, parentIdx);
        //             index = ((parentIdx = rawIdx) - heapOffset);
        //         }
        //     }
        //     return (item,heapCount);
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T PopAt<T>(in IListX<T> container, int index) where T : IComparable<T>
        {
            int heapCount = container.Count - 1;
            T item = container[index];//take de-heap item
            if (index == heapCount)
            {
                container.RemoveAt(heapCount);
                return item;
            }
            container[index] = container[heapCount];//move last heap item to de-heap position
            container.RemoveAt(heapCount);
            int parentIdx;
            if (index > 0)
            {
                parentIdx = (index - 1) >> 1;
                var dir = SmallerSwap(container, index, parentIdx);
                if (dir == 0) return item;
                else if (dir < 0)
                {
                    index = parentIdx;
                    while (index > 0)
                    {
                        parentIdx = (index - 1) >> 1;
                        if (SmallerSwap(container, index, parentIdx) < 0) index = parentIdx;
                        else break;
                    }
                    return item;
                }
            }

            while (true)
            {
                int leftChildIdx = (index << 1) + 1;
                int rightChildIdx = (index << 1) + 2;
                parentIdx = index;
                if (leftChildIdx < heapCount)
                {
                    if (container[leftChildIdx].CompareTo(container[index]) < 0)
                        index = leftChildIdx;
                }

                if (rightChildIdx < heapCount)
                {
                    if (container[rightChildIdx].CompareTo(container[index]) < 0)
                        index = rightChildIdx;
                }

                if (index == parentIdx) break;
                else
                {
                    Swap(container, index, parentIdx);
                    parentIdx = index;
                }
            }
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (T,int) PopAt<T>(in T[] container, int index, int heapCount, long heapOffset) where T : IComparable<T>
        {
            heapCount--;
            long rawIdx = heapOffset + index;
            T item = container[rawIdx];//take de-heap item
            if (index == heapCount) return (item,heapCount);
            long heapRangeEnd = heapOffset + heapCount;//reduce calculation
            container[rawIdx] = container[heapRangeEnd];//move last heap item to de-heap position
            long offsetL = heapOffset + 1;//reduce calculation
            long offsetR = heapOffset + 2;//reduce calculation
            long parentIdx;
            if (index > 0)
            {
                parentIdx = heapOffset + ((index - 1) >> 1);
                var dir = SmallerSwap(container, ref rawIdx, ref parentIdx);
                if (dir == 0) return (item,heapCount);
                else if (dir < 0)
                {
                    rawIdx = parentIdx;
                    while (rawIdx > heapOffset)
                    {
                        parentIdx = heapOffset + ((rawIdx - heapOffset - 1) >> 1);//parent index
                        if (SmallerSwap(container, ref rawIdx, ref parentIdx) < 0) rawIdx = parentIdx;
                        else break;
                    }
                    return (item,heapCount);
                }
            }

            while (true)
            {
                long leftChildIdx = (index << 1) + offsetL;
                long rightChildIdx = (index << 1) + offsetR;
                parentIdx = rawIdx;
                if (leftChildIdx < heapRangeEnd)
                {
                    if (container[leftChildIdx].CompareTo(container[rawIdx]) < 0)
                        rawIdx = leftChildIdx;
                }

                if (rightChildIdx < heapRangeEnd)
                {
                    if (container[rightChildIdx].CompareTo(container[rawIdx]) < 0)
                        rawIdx = rightChildIdx;
                }

                if (rawIdx == parentIdx) break;
                else
                {
                    Swap(container, ref rawIdx, ref parentIdx);
                    index = unchecked((int)((parentIdx = rawIdx) - heapOffset));
                }
            }
            return (item,heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (T,int) PopAt<T>(in T[] container, int index, int heapCount) where T : IComparable<T>
        {
            heapCount--;
            T item = container[index];//take de-heap item
            if (index == heapCount) return (item,heapCount);
            container[index] = container[heapCount];//move last heap item to de-heap position
            int parentIdx;
            if (index > 0)
            {
                parentIdx = (index - 1) >> 1;
                var dir = SmallerSwap(container, ref index, ref parentIdx);
                if (dir == 0) return (item,heapCount);
                else if (dir < 0)
                {
                    index = parentIdx;
                    while (index > 0)
                    {
                        parentIdx = (index - 1) >> 1;//parent index
                        if (SmallerSwap(container, ref index, ref parentIdx) < 0) index = parentIdx;
                        else break;
                    }
                    return (item,heapCount);
                }
            }

            while (true)
            {
                int leftChildIdx = (index << 1) + 1;
                int rightChildIdx = (index << 1) + 2;
                parentIdx = index;
                if (leftChildIdx < heapCount)
                {
                    if (container[leftChildIdx].CompareTo(container[index]) < 0)
                        index = leftChildIdx;
                }

                if (rightChildIdx < heapCount)
                {
                    if (container[rightChildIdx].CompareTo(container[index]) < 0)
                        index = rightChildIdx;
                }

                if (index == parentIdx) break;
                else
                {
                    Swap(container, ref index, ref parentIdx);
                    parentIdx = index;
                }
            }
            return (item,heapCount);
        }
        #endregion internal
    }
}