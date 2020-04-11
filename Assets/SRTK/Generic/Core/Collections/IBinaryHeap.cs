/************************************************************************************
| File: IBinaryHeap.cs                                                              |
| Project: SRTK.Collections                                                         |
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
using System.Collections.Generic;

//TODO:CS73:2019 add burst compile support

namespace SRTK
{
    using BHX = BinaryHeapX;
    //----------------------------------------------------------------------------------
    public interface IBinaryHeap<T, P> : ICollection<T>
    {
        bool Empty { get; }
        bool NotEmpty { get; }
        T Peek { get; }

        void Push(T item);
        void PushMany(params T[] items);
        void PushMany(ICollection<T> items);

        T Pop();
        IEnumerable<T> PopMany(int itemCount);
        IEnumerable<T> PopAll();
        IEnumerable<T> PopWhileLessOrEqual(P limit);
        IEnumerable<T> PopWhileLess(P limit);

        bool Drop(T item);
        bool Drop(P priority);
        bool DropMany(int itemCount);
        void DropWhileLessOrEqual(P limit);
        void DropWhileLess(P limit);
        void Sort();

        T this[int index] { get; }
    }

    public static class IBinaryHeapX
    {
        public static IBinaryHeap<T, T> Wrap<T>(this IListX<T> inner, int count, long offset = 0) where T : IComparable<T>
        {
            if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
            if (inner is T[])
            {
                T[] _inner = inner as T[];
                if (offset == 0) BHX.MinHeapify<T>(_inner, count);
                else BHX.MinHeapify<T>(_inner, count, offset);
                return new BinaryHeap_Array<T>() { _inner = _inner.Segment(offset, count) };
            }
            else if (inner is ArraySeg<T>)
            {
                var _inner = (ArraySeg<T>)inner;
                _inner = _inner.SubShift(offset, count, count);
                if (_inner._offset == 0) BHX.MinHeapify<T>(_inner._inner, _inner._count);
                else BHX.MinHeapify<T>(_inner._inner, _inner._count, _inner._offset);
                return new BinaryHeap_Array<T>() { _inner = _inner };
            }
            else if (inner is Segment<T, IListX<T>>)
            {
                var _inner = (Segment<T, IListX<T>>)inner;
                _inner = _inner.SubShift((int)offset, count, count);
                BHX.MinHeapify<T>(_inner);
                return new BinaryHeap_List<T>() { _inner = _inner };
            }
            else
            {
                var _inner = inner.SegmentI((int)offset, count);
                BHX.MinHeapify<T>(_inner);
                return new BinaryHeap_List<T>() { _inner = _inner };
            }
        }

        public static IBinaryHeap<T, T> Wrap<T>(this IListX<T> inner) where T : IComparable<T>
        {
            if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
            if (inner is T[])
            {
                T[] _inner = inner as T[];
                BHX.MinHeapify<T>(_inner);
                return new BinaryHeap_Array<T>() { _inner = _inner.Segment(0, _inner.Length) };
            }
            else if (inner is ArraySeg<T>)
            {
                var _inner = (ArraySeg<T>)inner;
                if (_inner._offset == 0) BHX.MinHeapify<T>(_inner._inner, _inner._count);
                else BHX.MinHeapify<T>(_inner._inner, _inner._count, _inner._offset);
                return new BinaryHeap_Array<T>() { _inner = _inner };
            }
            else
            {
                BHX.MinHeapify<T>(inner);
                return new BinaryHeap_List<T>() { _inner = inner };
            }
        }

        public static IBinaryHeap<T, P> Wrap<T, P>(this IListX<T> inner, int count, long offset = 0)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
            if (inner is T[])
            {
                T[] _inner = inner as T[];
                if (offset == 0) BHX.MinHeapify<T, P>(_inner, count);
                else BHX.MinHeapify<T, P>(_inner, count, offset);
                return new BinaryHeap_Array<T, P>() { _inner = _inner.Segment(offset, count) };
            }
            else if (inner is ArraySeg<T>)
            {
                var _inner = (ArraySeg<T>)inner;
                _inner = _inner.SubShift((int)offset, count, count);
                if (_inner._offset == 0) BHX.MinHeapify<T, P>(_inner._inner, _inner._count);
                else BHX.MinHeapify<T, P>(_inner._inner, _inner._count, _inner._offset);
                return new BinaryHeap_Array<T, P>() { _inner = _inner };
            }
            else if (inner is Segment<T, IListX<T>>)
            {
                var _inner = (Segment<T, IListX<T>>)inner;
                _inner = _inner.SubShift((int)offset, count, count);
                BHX.MinHeapify<T, P>(_inner);
                return new BinaryHeap_List<T, P>() { _inner = _inner };
            }
            else
            {
                var _inner = inner.SegmentI((int)offset, count);
                BHX.MinHeapify<T, P>(_inner);
                return new BinaryHeap_List<T, P>() { _inner = _inner };
            }
        }

        public static IBinaryHeap<T, P> Wrap<T, P>(this IListX<T> inner)//, P priorityRef)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
            if (inner is T[])
            {
                T[] _inner = inner as T[];
                BHX.MinHeapify<T, P>(_inner);
                return new BinaryHeap_Array<T, P>() { _inner = _inner.Segment(0, _inner.Length) };
            }
            else if (inner is ArraySeg<T>)
            {
                var _inner = (ArraySeg<T>)inner;
                if (_inner._offset == 0) BHX.MinHeapify<T, P>(_inner._inner, _inner._count);
                else BHX.MinHeapify<T, P>(_inner._inner, _inner._count, _inner._offset);
                return new BinaryHeap_Array<T, P>() { _inner = _inner };
            }
            else if (inner is Segment<T, IListX<T>>)
            {
                var _inner = (Segment<T, IListX<T>>)inner;
                BHX.MinHeapify<T, P>(_inner);
                return new BinaryHeap_List<T, P>() { _inner = _inner };
            }
            else
            {
                BHX.MinHeapify<T, P>(inner);
                return new BinaryHeap_List<T, P>() { _inner = inner };
            }
        }
    }
}