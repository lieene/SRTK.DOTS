/************************************************************************************
| File: BinaryHeapXIListIComparer.cs                                                |
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
| 2019-09-19	GL	static BinaryHeap functions on IList<T> by IComparer<T>
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
        public static int Push<T>(in IListX<T> container, in T item, IComparer<T> comparer)
        {
            int curIdx = container.Count;//cur heap index, initilized at heap item index at heap end
            container.Add(item);
            int parentIdx;
            while (curIdx > 0)
            {
                parentIdx = ((curIdx - 1) >> 1);//parent index
                if (SmallerSwap(container, curIdx, parentIdx, comparer) < 0) curIdx = parentIdx;
                else break;
            }
            return container.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in IListX<T> container, IComparer<T> comparer, in ICollection<T> items)
        {
            foreach (var item in items)
            {
                int curIdx = container.Count;//cur heap index, initilized at heap item index at heap end
                container.Add(item);
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, curIdx, parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return container.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T>(in IListX<T> container, IComparer<T> comparer, params T[] items)
        {
            int length = items.Length;
            for (int i = 0; i < length; i++) Push<T>(container, items[i], comparer);
            return container.Count;
        }

        //-------------------------------------------------------------------------------------
        #endregion Push
        //-------------------------------------------------------------------------------------
        #region Pop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Pop<T>(in IListX<T> container, IComparer<T> comparer)
        {
            int heapCount = container.Count - 1;
            int index = 0;
            T item = container[index];//take de-heap item
            if (index == heapCount)
            {
                container.RemoveAt(heapCount);
                return item;
            }
            container[index] = container[heapCount];//move last heap item to de-heap position
            container.RemoveAt(heapCount);
            int parentIdx;
            while (true)
            {
                int leftChildIdx = (index << 1) + 1;
                int rightChildIdx = (index << 1) + 2;
                parentIdx = index;
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
                    Swap<T>(container, index, parentIdx);
                    parentIdx = index;
                }
            }
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopMany<T>(IListX<T> container, int itemCount, IComparer<T> comparer)
        {
            if (itemCount > container.Count) throw new IndexOutOfRangeException("[BinaryHeap:PopMany] itemCount out range heapCount");
            while (--itemCount >= 0) yield return Pop<T>(container, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopAll<T>(IListX<T> container, IComparer<T> comparer)
        {
            while (container.Count > 0) yield return Pop<T>(container, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopWhileLessOrEqual<T>(IListX<T> container, T limit, IComparer<T> comparer)
        {
            while (container.Count > 0 && comparer.Compare(limit, container[0]) >= 0)
                yield return Pop<T>(container, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopWhileLess<T>(IListX<T> container, T limit, IComparer<T> comparer)
        {
            while (container.Count > 0 && comparer.Compare(limit, container[0]) > 0)
                yield return Pop<T>(container, comparer);
        }
        #endregion Pop
        //-------------------------------------------------------------------------------------
        #region Drop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Drop<T>(in IListX<T> container, in T item, IComparer<T> comparer)
        {
            int index = container.IndexOf(item);
            if (index < 0) return false;
            PopAt(container, index, comparer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DropMany<T>(in IListX<T> container, int itemCount, IComparer<T> comparer)
        {
            if (itemCount > container.Count) return false;
            while (--itemCount >= 0) Pop<T>(container, comparer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DropWhileLessOrEqual<T>(in IListX<T> container, in T limit, IComparer<T> comparer)
        {
            while (container.Count > 0 && comparer.Compare(limit, container[0]) >= 0)
                Pop<T>(container, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DropWhileLess<T>(in IListX<T> container, in T limit, IComparer<T> comparer)
        {
            while (container.Count > 0 && comparer.Compare(limit, container[0]) > 0)
                Pop<T>(container, comparer);
        }

        #endregion Drop
        //-------------------------------------------------------------------------------------
        #region Heapify
        //-------------------------------------------------------------------------------------
         [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinHeapify<T>(in IListX<T> container, IComparer<T> comparer)
        {
            int heapCount = 0;
            int count = container.Count;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap(container, curIdx, parentIdx, comparer) < 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaxHeapify<T>(in IListX<T> container, IComparer<T> comparer)
        {
            int heapCount = 0;
            int count = container.Count;
            while (heapCount < count)
            {
                int curIdx = heapCount++;//cur heap index, initilized at heap item index at heap end
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (GreaterSwap(container, curIdx, parentIdx, comparer) > 0) curIdx = parentIdx;
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
        public static void Sort<T>(in IListX<T> container, IComparer<T> comparer)
        {
            MaxHeapify(container, comparer);
            SortMaxHeap(container, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMinHeap<T>(in IListX<T> container, IComparer<T> comparer)
        {
            SortMaxHeap(container, comparer);
            int heapCount = container.Count;
            int step = heapCount << 1;
            for (int i = 0; i < step; i++) //reverse order
                Swap(container, i, (heapCount - i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMaxHeap<T>(in IListX<T> container, IComparer<T> comparer)
        {
            int count2Pop = container.Count;
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
                        Swap<T>(container, index, parentIdx);
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