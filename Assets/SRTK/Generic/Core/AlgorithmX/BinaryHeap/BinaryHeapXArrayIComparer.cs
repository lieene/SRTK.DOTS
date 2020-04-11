/************************************************************************************
| File: BinaryHeapXArrayIComparer.cs                                                |
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
| 2019-09-19	GL	static BinaryHeap functions on array by IComparer<T>
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
        #region Push
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Push<T>(in T[] container, in T item, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            long curIdx = heapOffset + heapCount++;//cur heap index, initilized at heap item index at heap end
            container[curIdx] = item;
            long parentIdx;
            while (curIdx > heapOffset)
            {
                parentIdx = ((curIdx - heapOffset - 1) >> 1) + heapOffset;//parent index
                if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                else break;
            }
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Push<T>(in T[] container, in T item, IComparer<T> comparer, int heapCount)
        {
            int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
            container[curIdx] = item;
            int parentIdx;
            while (curIdx > 0)
            {
                parentIdx = ((curIdx - 1) >> 1);//parent index
                if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                else break;
            }
            return heapCount;
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset, in ICollection<T> items)
        {
            foreach (var item in items)
            {
                long curIdx = heapOffset + heapCount++;//cur heap index, initilized at heap item index at heap end
                container[curIdx] = item;
                long parentIdx;
                while (curIdx > heapOffset)
                {
                    parentIdx = ((curIdx - heapOffset - 1) >> 1) + heapOffset;//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in T[] container, IComparer<T> comparer, int heapCount, in ICollection<T> items)
        {
            foreach (var item in items)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                container[curIdx] = item;
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return heapCount;
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset, params T[] items)
        {
            int length = items.Length;
            Array.Copy(items, 0, container, heapOffset + heapCount, length);
            while (length-- > 0)
            {
                long curIdx = heapOffset + heapCount++;//cur heap index, initilized at heap item index at heap end
                long parentIdx;
                while (curIdx > heapOffset)
                {
                    parentIdx = ((curIdx - heapOffset - 1) >> 1) + heapOffset;//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in T[] container, IComparer<T> comparer, int heapCount, params T[] items)
        {
            int length = items.Length;
            Array.Copy(items, 0, container, heapCount, length);
            while (length-- > 0)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return heapCount;
        }

        //-------------------------------------------------------------------------------------
        #endregion Push
        //-------------------------------------------------------------------------------------
        #region Pop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T elem, int heapCount) Pop<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            heapCount--;
            int index = 0;
            long rawIdx = heapOffset;
            T item = container[rawIdx];//take de-heap item
            if (heapCount == 0) return (item, heapCount);

            long heapRangeEnd = heapOffset + heapCount;//reduce calculation
            container[rawIdx] = container[heapRangeEnd];//move last heap item to de-heap position
            long offsetL = heapOffset + 1;//reduce calculation
            long offsetR = heapOffset + 2;//reduce calculation
            long parentIdx;
            while (true)
            {
                long leftChildIdx = (index << 1) + offsetL;
                long rightChildIdx = (index << 1) + offsetR;
                parentIdx = rawIdx;
                if (leftChildIdx < heapRangeEnd)
                {
                    if (comparer.Compare(container[leftChildIdx], container[rawIdx]) < 0)
                        rawIdx = leftChildIdx;
                }

                if (rightChildIdx < heapRangeEnd)
                {
                    if (comparer.Compare(container[rightChildIdx], container[rawIdx]) < 0)
                        rawIdx = rightChildIdx;
                }

                if (rawIdx == parentIdx) break;
                else
                {
                    Swap<T>(container, ref rawIdx, ref parentIdx);
                    index = unchecked((int)((parentIdx = rawIdx) - heapOffset));
                }
            }
            return (item, heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T elem, int heapCount) Pop<T>(in T[] container, IComparer<T> comparer, int heapCount)
        {
            heapCount--;
            int index = 0;
            T item = container[index];//take de-heap item
            if (heapCount == 0) return (item, heapCount);

            container[index] = container[heapCount];//move last heap item to de-heap position
            int parentIdx = index;
            while (true)
            {
                int leftChildIdx = (index << 1) + 1;
                int rightChildIdx = (index << 1) + 2;
                if (leftChildIdx < heapCount)
                {
                    if (comparer.Compare(container[leftChildIdx], container[index]) < 0)
                        index = leftChildIdx;
                }

                if (rightChildIdx < heapCount)
                {
                    if (comparer.Compare(container[rightChildIdx], container[index]) < 0)
                        index = rightChildIdx;
                }

                if (index == parentIdx) break;
                else
                {
                    Swap<T>(container, ref index, ref parentIdx);
                    parentIdx = index;
                }
            }
            return (item, heapCount);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopMany<T>(T[] container, int itemCount, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            if (itemCount > heapCount) throw new IndexOutOfRangeException("[BinaryHeap:PopMany] itemCount out range heapCount");
            while (--itemCount >= 0) yield return Pop<T>(container, comparer, heapCount, heapOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopMany<T>(T[] container, int itemCount, IComparer<T> comparer, int heapCount)
        {
            if (itemCount > heapCount) throw new IndexOutOfRangeException("[BinaryHeap:PopMany] itemCount out range heapCount");
            while (--itemCount >= 0) yield return Pop<T>(container, comparer, heapCount);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopAll<T>(T[] container, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            while (heapCount > 0) yield return Pop<T>(container, comparer, heapCount, heapOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopAll<T>(T[] container, IComparer<T> comparer, int heapCount)
        {
            while (heapCount > 0) yield return Pop<T>(container, comparer, heapCount);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopWhileLessOrEqual<T>(T[] container, T limit, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[heapOffset]) >= 0)
                yield return Pop<T>(container, comparer, heapCount, heapOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopWhileLessOrEqual<T>(T[] container, T limit, IComparer<T> comparer, int heapCount)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[0]) >= 0)
                yield return Pop<T>(container, comparer, heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopWhileLess<T>(T[] container, T limit, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[heapOffset]) > 0)
                yield return Pop<T>(container, comparer, heapCount, heapOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T elem, int heapCount)> PopWhileLess<T>(T[] container, T limit, IComparer<T> comparer, int heapCount)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[0]) > 0)
                yield return Pop<T>(container, comparer, heapCount);
        }
        #endregion Pop
        //-------------------------------------------------------------------------------------
        #region Drop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool success, int heapCount) Drop<T>(in T[] container, in T item, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            for (int i = 0; i < heapCount; i++)
            {
                if (item.Equals(container[i + heapOffset]))
                {
                    (_, heapCount) = PopAt(container, i, comparer, heapCount, heapOffset);
                    return (true, heapCount);
                }
            }
            return (false, heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool success, int heapCount) Drop<T>(in T[] container, in T item, IComparer<T> comparer, int heapCount)
        {
            for (int i = 0; i < heapCount; i++)
            {
                if (item.Equals(container[i]))
                {
                    (_, heapCount) = PopAt(container, i, comparer, heapCount);
                    return (true, heapCount);
                }
            }
            return (false, heapCount);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool success, int heapCount) DropMany<T>(in T[] container, int itemCount, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            if (itemCount > heapCount) return (false, heapCount);
            while (--itemCount >= 0) (_, heapCount) = Pop<T>(container, comparer, heapCount, heapOffset);
            return (true, heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool success, int heapCount) DropMany<T>(in T[] container, int itemCount, IComparer<T> comparer, int heapCount)
        {
            if (itemCount > heapCount) return (false, heapCount);
            while (--itemCount >= 0) (_, heapCount) = Pop<T>(container, comparer, heapCount);
            return (true, heapCount);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DropWhileLessOrEqual<T>(in T[] container, in T limit, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[heapOffset]) >= 0)
                (_, heapCount) = Pop<T>(container, comparer, heapCount, heapOffset);
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DropWhileLessOrEqual<T>(in T[] container, in T limit, IComparer<T> comparer, int heapCount)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[0]) >= 0)
                (_, heapCount) = Pop<T>(container, comparer, heapCount);
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DropWhileLess<T>(in T[] container, in T limit, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[heapOffset]) > 0)
                (_, heapCount) = Pop<T>(container, comparer, heapCount, heapOffset);
            return heapCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DropWhileLess<T>(in T[] container, in T limit, IComparer<T> comparer, int heapCount)
        {
            while (heapCount > 0 && comparer.Compare(limit, container[0]) > 0)
                (_, heapCount) = Pop<T>(container, comparer, heapCount);
            return heapCount;
        }

        #endregion Drop
        //-------------------------------------------------------------------------------------
        #region Heapify
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinHeapify<T>(in T[] container, IComparer<T> comparer, int count, long offset)
        {
            int heapCount = 0;
            while (heapCount < count)
            {
                long curIdx = offset + heapCount++;//cur heap index, initilized at heap item index at heap end
                long parentIdx;
                while (curIdx > offset)
                {
                    parentIdx = ((curIdx - offset - 1) >> 1) + offset;//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinHeapify<T>(in T[] container, IComparer<T> comparer, int count)
        {
            int heapCount = 0;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinHeapify<T>(in T[] container, IComparer<T> comparer)
        {
            int heapCount = 0;
            int count = container.Length;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, ref curIdx, ref parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaxHeapify<T>(in T[] container, IComparer<T> comparer, int count, long offset)
        {
            int heapCount = 0;
            while (heapCount < count)
            {
                long curIdx = offset + heapCount++;//cur heap index, initilized at heap item index at heap end
                long parentIdx;
                while (curIdx > offset)
                {
                    parentIdx = ((curIdx - offset - 1) >> 1) + offset;//parent index
                    if (GreaterSwap(container, ref curIdx, ref parentIdx, comparer) > 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaxHeapify<T>(in T[] container, IComparer<T> comparer, int count)
        {
            int heapCount = 0;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (GreaterSwap(container, ref curIdx, ref parentIdx, comparer) > 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaxHeapify<T>(in T[] container, IComparer<T> comparer)
        {
            int heapCount = 0;
            int count = container.Length;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (GreaterSwap(container, ref curIdx, ref parentIdx, comparer) > 0) curIdx = parentIdx;
                    else break;
                }
            }
        }
        //-------------------------------------------------------------------------------------
        #endregion Heapify
        //-------------------------------------------------------------------------------------
        #region Sort
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            MaxHeapify(container, comparer, heapCount, heapOffset);
            SortMaxHeap(container, comparer, heapCount, heapOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(in T[] container, IComparer<T> comparer, int heapCount)
        {
            MaxHeapify(container, comparer, heapCount);
            SortMaxHeap(container, comparer, heapCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(in T[] container, IComparer<T> comparer)
        {
            MaxHeapify(container, comparer);
            SortMaxHeap(container, comparer);
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMinHeap<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            SortMaxHeap(container, comparer, heapCount, heapOffset);
            int step = heapCount << 1;
            for (int i = 0; i < step; i++) //reverse order
            {
                long from = i + heapOffset, to = (heapCount - i) + heapOffset;
                Swap(container, ref from, ref to);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMinHeap<T>(in T[] container, IComparer<T> comparer, int heapCount)
        {
            SortMaxHeap(container, comparer, heapCount);
            int step = heapCount << 1;
            for (int i = 0; i < step; i++) //reverse order
            {
                int to = (heapCount - i);
                Swap(container, ref i, ref to);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMinHeap<T>(in T[] container, IComparer<T> comparer)
        {
            SortMaxHeap(container, comparer);
            int heapCount = container.Length;
            int step = heapCount << 1;
            for (int i = 0; i < step; i++) //reverse order
            {
                int to = (heapCount - i);
                Swap(container, ref i, ref to);
            }
        }

        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMaxHeap<T>(in T[] container, IComparer<T> comparer, int heapCount, long heapOffset)
        {
            if (heapCount < 2) return;
            int count2Pop = heapCount;
            while (count2Pop-- > 0)
            {
                int index = 0;
                long rawIdx = heapOffset;
                T item = container[rawIdx];//take de-heap item

                long heapRangeEnd = heapOffset + count2Pop;//reduce calculation
                container[rawIdx] = container[heapRangeEnd];//move last heap item to de-heap position
                long offsetL = heapOffset + 1;//reduce calculation
                long offsetR = heapOffset + 2;//reduce calculation
                long parentIdx;
                while (true)
                {
                    long leftChildIdx = (index << 1) + offsetL;
                    long rightChildIdx = (index << 1) + offsetR;
                    parentIdx = rawIdx;
                    if (leftChildIdx < heapRangeEnd)
                    {
                        if (comparer.Compare(container[leftChildIdx], container[rawIdx]) > 0)
                            rawIdx = leftChildIdx;
                    }

                    if (rightChildIdx < heapRangeEnd)
                    {
                        if (comparer.Compare(container[rightChildIdx], container[rawIdx]) > 0)
                            rawIdx = rightChildIdx;
                    }

                    if (rawIdx == parentIdx) break;
                    else
                    {
                        Swap<T>(container, ref rawIdx, ref parentIdx);
                        index = unchecked((int)((parentIdx = rawIdx) - heapOffset));
                    }
                }
                container[heapRangeEnd] = item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMaxHeap<T>(in T[] container, IComparer<T> comparer, int heapCount)
        {
            int count2Pop = heapCount;
            if (count2Pop < 2) return;
            while (count2Pop-- > 0)
            {
                int index = 0;
                T item = container[index];//take de-heap item
                container[index] = container[count2Pop];//move last heap item to de-heap position
                int parentIdx = index;
                while (true)
                {
                    int leftChildIdx = (index << 1) + 1;
                    int rightChildIdx = (index << 1) + 2;
                    if (leftChildIdx < count2Pop)
                    {
                        if (comparer.Compare(container[leftChildIdx], container[index]) > 0)
                            index = leftChildIdx;
                    }

                    if (rightChildIdx < count2Pop)
                    {
                        if (comparer.Compare(container[rightChildIdx], container[index]) > 0)
                            index = rightChildIdx;
                    }

                    if (index == parentIdx) break;
                    else
                    {
                        Swap<T>(container, ref index, ref parentIdx);
                        parentIdx = index;
                    }
                }
                container[count2Pop] = item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMaxHeap<T>(in T[] container, IComparer<T> comparer)
        {
            int count2Pop = container.Length;
            if (count2Pop < 2) return;
            while (count2Pop-- > 0)
            {
                int index = 0;
                T item = container[index];//take de-heap item
                container[index] = container[count2Pop];//move last heap item to de-heap position
                int parentIdx = index;
                while (true)
                {
                    int leftChildIdx = (index << 1) + 1;
                    int rightChildIdx = (index << 1) + 2;
                    if (leftChildIdx < count2Pop)
                    {
                        if (comparer.Compare(container[leftChildIdx], container[index]) > 0)
                            index = leftChildIdx;
                    }

                    if (rightChildIdx < count2Pop)
                    {
                        if (comparer.Compare(container[rightChildIdx], container[index]) > 0)
                            index = rightChildIdx;
                    }

                    if (index == parentIdx) break;
                    else
                    {
                        Swap<T>(container, ref index, ref parentIdx);
                        parentIdx = index;
                    }
                }
                container[count2Pop] = item;
            }
        }
        //-------------------------------------------------------------------------------------
        #endregion Sort
        //-------------------------------------------------------------------------------------
    }

}