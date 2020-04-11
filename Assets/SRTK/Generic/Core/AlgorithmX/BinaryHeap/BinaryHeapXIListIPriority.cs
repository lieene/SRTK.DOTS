/************************************************************************************
| File: BinaryHeapXIListIPriority.cs                                                |
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
| 2019-09-19	GL	static BinaryHeap functions on IList<T> by IPriority<P>
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
        public static int Push<T, P>(in IListX<T> container, in T item)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            int curIdx = container.Count;//cur heap index, initilized at heap item index at heap end
            container.Add(item);
            int parentIdx;
            while (curIdx > 0)
            {
                parentIdx = ((curIdx - 1) >> 1);//parent index
                if (SmallerSwap<T, P>(container, curIdx, parentIdx) < 0) curIdx = parentIdx;
                else break;
            }
            return container.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T, P>(in IListX<T> container, in ICollection<T> items)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            foreach (var item in items)
            {
                int curIdx = container.Count;//cur heap index, initilized at heap item index at heap end
                container.Add(item);
                int parentIdx;
                while (curIdx > 0)
                {
                    parentIdx = ((curIdx - 1) >> 1);//parent index
                    if (SmallerSwap<T, P>(container, curIdx, parentIdx) < 0) curIdx = parentIdx;
                    else break;
                }
            }
            return container.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PushMany<T, P>(in IListX<T> container, params T[] items)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            int length = items.Length;
            for (int i = 0; i < length; i++) Push<T, P>(container, items[i]);
            return container.Count;
        }

        //-------------------------------------------------------------------------------------
        #endregion Push
        //-------------------------------------------------------------------------------------
        #region Pop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Pop<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
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
                    if (container[leftChildIdx].Priority.CompareTo(container[index].Priority) < 0)
                        index = leftChildIdx;
                }

                if (rightChildIdx < heapCount)
                {
                    if (container[rightChildIdx].Priority.CompareTo(container[index].Priority) < 0)
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
        public static IEnumerable<T> PopMany<T, P>(IListX<T> container, int itemCount)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (itemCount > container.Count) throw new IndexOutOfRangeException("[BinaryHeap:PopMany] itemCount out range heapCount");
            while (--itemCount >= 0) yield return Pop<T, P>(container);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopAll<T, P>(IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            while (container.Count >= 0) yield return Pop<T, P>(container);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopWhileLessOrEqual<T, P>(IListX<T> container, P limit)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            while (container.Count >= 0 && limit.CompareTo(container[0].Priority) >= 0)
                yield return Pop<T, P>(container);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> PopWhileLess<T, P>(IListX<T> container, P limit)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            while (container.Count >= 0 && limit.CompareTo(container[0].Priority) > 0)
                yield return Pop<T, P>(container);
        }
        #endregion Pop
        //-------------------------------------------------------------------------------------
        #region Drop
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Drop<T, P>(in IListX<T> container, in T item)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            int index = container.IndexOf(item);
            if (index < 0) return false;
            PopAt<T, P>(container, index);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Drop<T, P>(in IListX<T> container, in P priority)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            int heapCount = container.Count;
            for (int i = 0; i < heapCount; i++)
            {
                if (priority.Equals(container[i].Priority))
                {
                    PopAt<T, P>(container, i);
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DropMany<T, P>(in IListX<T> container, int itemCount)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (itemCount > container.Count) return false;
            while (--itemCount >= 0) Pop<T, P>(container);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DropWhileLessOrEqual<T, P>(in IListX<T> container, in P limit)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            while (container.Count >= 0 && limit.CompareTo(container[0].Priority) >= 0)
                Pop<T, P>(container);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DropWhileLess<T, P>(in IListX<T> container, in P limit)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            while (container.Count >= 0 && limit.CompareTo(container[0].Priority) > 0)
                Pop<T, P>(container);
        }

        #endregion Drop
        //-------------------------------------------------------------------------------------
        #region Heapify
        //-------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinHeapify<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
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
                    if (SmallerSwap<T, P>(container, curIdx, parentIdx) < 0) curIdx = parentIdx;
                    else break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MaxHeapify<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
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
                    if (GreaterSwap<T, P>(container, curIdx, parentIdx) > 0) curIdx = parentIdx;
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
        public static void Sort<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            MaxHeapify<T, P>(container);
            SortMaxHeap<T, P>(container);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMinHeap<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            SortMaxHeap<T, P>(container);
            int heapCount = container.Count;
            int step = heapCount << 1;
            for (int i = 0; i < step; i++) //reverse order
                Swap<T>(container, i, (heapCount - i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortMaxHeap<T, P>(in IListX<T> container)
            where T : IPriority<P>
            where P : IComparable<P>
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
                        if (container[leftChildIdx].Priority.CompareTo(container[index].Priority) > 0)
                            index = leftChildIdx;
                    }

                    if (rightChildIdx < count2Pop)
                    {
                        if (container[rightChildIdx].Priority.CompareTo(container[index].Priority) > 0)
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