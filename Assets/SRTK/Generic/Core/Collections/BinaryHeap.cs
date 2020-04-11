/************************************************************************************
| File: BinaryHeap.cs                                                               |
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
using System.Collections;
using System.Collections.Generic;
using SRTK.Pool;

//TODO:CS73:2019 add burst compile support

namespace SRTK
{
    using System.Text;
    using static MathX;
    using BHX = BinaryHeapX;

    public class BinaryHeap_List<T> : IBinaryHeap<T, T> where T : IComparable<T>
    {
        internal protected IListX<T> _inner;
        //----------------------------------------------------------------------------------
        #region Ctor
        internal protected BinaryHeap_List() { _inner = null; }

        public BinaryHeap_List(ICollection<T> items)
        {
            _inner = new ListX<T>(items);
            BHX.MinHeapify<T>(_inner);
        }
        public BinaryHeap_List(params T[] items)
        {
            _inner = new ListX<T>(items);
            BHX.MinHeapify<T>(_inner);
        }
        // public static BinaryHeap_List<T> Wrap(IList<T> inner)
        // {
        //     if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
        //     BHX.MinHeapify<T, IList<T>>(ref inner);
        //     return new BinaryHeap_List<T> { _container = inner };
        // }
        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region IBinaryHeap
        public bool Empty => _inner.Count == 0;
        public bool NotEmpty => _inner.Count > 0;
        public T Peek => _inner[0];
        public void Push(T item) => BHX.Push<T>(_inner, item);
        public void PushMany(params T[] items) => BHX.PushMany<T>(_inner, items);
        public void PushMany(ICollection<T> items) => BHX.PushMany<T>(_inner, items);
        public T Pop()
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            else return BHX.Pop<T>(_inner);
        }
        public IEnumerable<T> PopMany(int itemCount)
        {
            if (_inner.Count < itemCount) throw new OverflowException("[BinaryHeap:Pop] not enough item in heap");
            return BHX.PopMany<T>(_inner, itemCount);
        }
        public IEnumerable<T> PopAll() => BHX.PopAll<T>(_inner);
        public IEnumerable<T> PopWhileLessOrEqual(T limit) => BHX.PopWhileLessOrEqual<T>(_inner, limit);
        public IEnumerable<T> PopWhileLess(T limit) => BHX.PopWhileLess<T>(_inner, limit);
        public bool Drop(T item) => BHX.Drop<T>(_inner, item);
        public bool DropMany(int itemCount) => BHX.DropMany<T>(_inner, itemCount);
        public void DropWhileLessOrEqual(T limit) => BHX.DropWhileLessOrEqual<T>(_inner, limit);
        public void DropWhileLess(T limit) => BHX.DropWhileLess<T>(_inner, limit);
        public void Sort() => BHX.SortMinHeap<T>(_inner);
        public T this[int index] => _inner[index];
        #endregion IBinaryHeap
        //----------------------------------------------------------------------------------
        #region ICollection
        public int Count => _inner.Count;
        public bool IsReadOnly => true;
        public void Add(T item) => BHX.Push<T>(_inner, item);
        public void Clear() => _inner.Clear();
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public bool Remove(T item) => BHX.Drop<T>(_inner, item);
        #endregion ICollection
        //----------------------------------------------------------------------------------
        #region IEnumerator
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion IEnumerator
        //----------------------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"[");
            int countLmt = _inner.Count.MaxAt(16);
            for (int i = 0; i < countLmt; i++)
            {
                sb.Append(this[i]);
                if (i < countLmt - 1)
                {
                    if (i == 0 || i == 2 || i == 6) sb.Append(';');
                    else sb.Append(',');
                }
            }
            if (_inner.Count > countLmt) sb.Append("...");
            sb.Append("]");
            return sb.ToString();
        }
    }

    public class BinaryHeap_List<T, P> : IBinaryHeap<T, P>
        where T : IPriority<P>
        where P : IComparable<P>
    {
        internal protected IListX<T> _inner;
        //----------------------------------------------------------------------------------
        #region Ctor
        internal protected BinaryHeap_List() { _inner = null; }

        public BinaryHeap_List(ICollection<T> items)
        {
            _inner = new ListX<T>(items);
            BHX.MinHeapify<T, P>(_inner);
        }

        public BinaryHeap_List(params T[] items)
        {
            _inner = new ListX<T>(items);
            BHX.MinHeapify<T, P>(_inner);
        }

        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region IBinaryHeap
        public bool Empty => _inner.Count == 0;
        public bool NotEmpty => _inner.Count > 0;
        public T Peek => _inner[0];
        public void Push(T item) => BHX.Push<T, P>(_inner, item);
        public void PushMany(params T[] items) => BHX.PushMany<T, P>(_inner, items);
        public void PushMany(ICollection<T> items) => BHX.PushMany<T, P>(_inner, items);
        public T Pop()
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            else return BHX.Pop<T, P>(_inner);
        }
        public IEnumerable<T> PopMany(int itemCount)
        {
            if (_inner.Count < itemCount) throw new OverflowException("[BinaryHeap:Pop] not enough item in heap");
            return BHX.PopMany<T, P>(_inner, itemCount);
        }
        public IEnumerable<T> PopAll() => BHX.PopAll<T, P>(_inner);
        public IEnumerable<T> PopWhileLessOrEqual(P limit) => BHX.PopWhileLessOrEqual<T, P>(_inner, limit);
        public IEnumerable<T> PopWhileLess(P limit) => BHX.PopWhileLess<T, P>(_inner, limit);
        public bool Drop(T item) => BHX.Drop<T, P>(_inner, item);
        public bool Drop(P priority) => BHX.Drop<T, P>(_inner, priority);
        public bool DropMany(int itemCount) => BHX.DropMany<T, P>(_inner, itemCount);
        public void DropWhileLessOrEqual(P limit) => BHX.DropWhileLessOrEqual<T, P>(_inner, limit);
        public void DropWhileLess(P limit) => BHX.DropWhileLess<T, P>(_inner, limit);
        public void Sort() => BHX.SortMinHeap<T, P>(_inner);
        public T this[int index] => _inner[index];
        #endregion IBinaryHeap
        //----------------------------------------------------------------------------------
        #region ICollection
        public int Count => _inner.Count;
        public bool IsReadOnly => true;
        public void Add(T item) => BHX.Push<T, P>(_inner, item);
        public void Clear() => _inner.Clear();
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public bool Remove(T item) => BHX.Drop<T, P>(_inner, item);
        #endregion ICollection
        //----------------------------------------------------------------------------------
        #region IEnumerator
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion IEnumerator
        //----------------------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"[");
            int countLmt = _inner.Count.MaxAt(16);
            for (int i = 0; i < countLmt; i++)
            {
                sb.Append(this[i]);
                if (i < countLmt - 1)
                {
                    if (i == 0 || i == 2 || i == 6) sb.Append(';');
                    else sb.Append(',');
                }
            }
            if (_inner.Count > countLmt) sb.Append("...");
            sb.Append("]");
            return sb.ToString();
        }
    }

    public class BinaryHeap_Array<T> : IBinaryHeap<T, T> where T : IComparable<T>
    {
        internal protected ArraySeg<T> _inner;
        //----------------------------------------------------------------------------------
        #region Ctor
        internal protected BinaryHeap_Array() { _inner = default(ArraySeg<T>); }

        public BinaryHeap_Array(int capacity)
        { _inner = new T[capacity].NewSeg(0, capacity); }

        public BinaryHeap_Array(params T[] items)
        {
            _inner = items.Segment(0, items.Length);
            BHX.MinHeapify<T>(_inner._inner, _inner._count, _inner._offset);
        }
        // public static BinaryHeap_List<T> Wrap(IList<T> inner)
        // {
        //     if (inner == null) throw new ArgumentNullException("BinaryHeap_List inner list can not be null");
        //     BHX.MinHeapify<T, IList<T>>(ref inner);
        //     return new BinaryHeap_List<T> { _container = inner };
        // }
        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region IBinaryHeap
        public bool Empty => _inner.Count == 0;
        public bool NotEmpty => _inner.Count > 0;
        public T Peek => _inner[0];
        public void Push(T item)
        {
            if (_inner._count >= _inner._capacity) throw new OverflowException("[BinaryHeap:Push] no more capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.Push<T>(_inner._inner, item, _inner._count, _inner._offset);
            else _inner._count = BHX.Push<T>(_inner._inner, item, _inner._count);
        }
        public void PushMany(params T[] items)
        {
            if (_inner.FreeCount < items.Length) throw new OverflowException("[BinaryHeap:Push] not enough capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.PushMany<T>(_inner._inner, _inner._count, _inner._offset, items);
            else _inner._count = BHX.PushMany<T>(_inner._inner, _inner._count, items);
        }
        public void PushMany(ICollection<T> items)
        {
            if (_inner.FreeCount < items.Count) throw new OverflowException("[BinaryHeap:Push] not enough capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.PushMany<T>(_inner._inner, _inner._count, _inner._offset, items);
            else _inner._count = BHX.PushMany<T>(_inner._inner, _inner._count, items);
        }
        public T Pop()
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            else if (_inner._offset > 0)
            {
                T elem;
                (elem, _inner._count) = BHX.Pop<T>(_inner._inner, _inner._count, _inner._offset);
                return elem;
            }
            else
            {
                T elem;
                (elem, _inner._count) = BHX.Pop<T>(_inner._inner, _inner._count);
                return elem;
            }
        }

        public IEnumerable<T> PopMany(int itemCount)
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            var many = _inner._offset > 0 ?
                BHX.PopMany<T>(_inner._inner, itemCount, _inner._count, _inner._offset) :
                BHX.PopMany<T>(_inner._inner, itemCount, _inner._count);
            foreach (var item in many)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopAll()
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            var all = _inner._offset > 0 ?
                BHX.PopAll<T>(_inner._inner, _inner._count, _inner._offset) :
                BHX.PopAll<T>(_inner._inner, _inner._count);

            foreach (var item in all)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopWhileLessOrEqual(T limit)
        {
            if (Empty) yield break;
            var until = _inner._offset > 0 ?
                BHX.PopWhileLessOrEqual<T>(_inner._inner, limit, _inner._count, _inner._offset) :
                BHX.PopWhileLessOrEqual<T>(_inner._inner, limit, _inner._count);

            foreach (var item in until)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopWhileLess(T limit)
        {
            if (Empty) yield break;
            var until = _inner._offset > 0 ?
                BHX.PopWhileLess<T>(_inner._inner, limit, _inner._count, _inner._offset) :
                BHX.PopWhileLess<T>(_inner._inner, limit, _inner._count);

            foreach (var item in until)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public bool Drop(T item)
        {
            bool suc;
            if (_inner._offset > 0) (suc, _inner._count) = BHX.Drop<T>(_inner._inner, item, _inner._count, _inner._offset);
            else (suc, _inner._count) = BHX.Drop<T>(_inner._inner, item, _inner._count);
            return suc;
        }

        public bool DropMany(int itemCount)
        {
            bool suc;
            if (_inner._offset > 0) (suc, _inner._count) = BHX.DropMany<T>(_inner._inner, itemCount, _inner._count, _inner._offset);
            else (suc, _inner._count) = BHX.DropMany<T>(_inner._inner, itemCount, _inner._count);
            return suc;
        }

        public void DropWhileLessOrEqual(T limit)
        {
            if (_inner._offset > 0) _inner._count = BHX.DropWhileLessOrEqual<T>(_inner._inner, limit, _inner._count, _inner._offset);
            else _inner._count = BHX.DropWhileLessOrEqual<T>(_inner._inner, limit, _inner._count);
        }

        public void DropWhileLess(T limit)
        {
            if (_inner._offset > 0) _inner._count = BHX.DropWhileLess<T>(_inner._inner, limit, _inner._count, _inner._offset);
            else _inner._count = BHX.DropWhileLess<T>(_inner._inner, limit, _inner._count);
        }

        public void Sort()
        {
            if (_inner._offset > 0) BHX.SortMinHeap<T>(_inner._inner, _inner._count, _inner._offset);
            else BHX.SortMinHeap<T>(_inner._inner, _inner._count);
        }
        public T this[int index] => _inner[index];
        #endregion IBinaryHeap
        //----------------------------------------------------------------------------------
        #region ICollection
        public int Count => _inner.Count;
        public bool IsReadOnly => true;
        public void Add(T item) => Push(item);
        public void Clear() => _inner.Clear();
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public bool Remove(T item) => Drop(item);
        #endregion ICollection
        //----------------------------------------------------------------------------------
        #region IEnumerator
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion IEnumerator
        //----------------------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"[");
            int countLmt = _inner.Count.MaxAt(16);
            for (int i = 0; i < countLmt; i++)
            {
                sb.Append(this[i]);
                if (i < countLmt - 1)
                {
                    if (i == 0 || i == 2 || i == 6) sb.Append(';');
                    else sb.Append(',');
                }
            }
            if (_inner.Count > countLmt) sb.Append("...");
            sb.Append("]");
            return sb.ToString();
        }
    }

    public class BinaryHeap_Array<T, P> : IBinaryHeap<T, P>
        where T : IPriority<P>
        where P : IComparable<P>
    {
        internal protected ArraySeg<T> _inner;
        //----------------------------------------------------------------------------------
        #region Ctor
        internal protected BinaryHeap_Array() { _inner = default(ArraySeg<T>); }

        public BinaryHeap_Array(int capacity)
        { _inner = new T[capacity].NewSeg(0, capacity); }

        public BinaryHeap_Array(params T[] items)
        {
            _inner = items.Segment(0, items.Length);
            BHX.MinHeapify<T, P>(_inner._inner, _inner._count);
        }
        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region IBinaryHeap
        public bool Empty => _inner.Count == 0;
        public bool NotEmpty => _inner.Count > 0;
        public T Peek => _inner[0];
        public void Push(T item)
        {
            if (_inner._count >= _inner._capacity) throw new OverflowException("[BinaryHeap:Push] no more capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.Push<T, P>(_inner._inner, item, _inner._count, _inner._offset);
            else _inner._count = BHX.Push<T, P>(_inner._inner, item, _inner._count);
        }
        public void PushMany(params T[] items)
        {
            if (_inner.FreeCount < items.Length) throw new OverflowException("[BinaryHeap:Push] not enough capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.PushMany<T, P>(_inner._inner, _inner._count, _inner._offset, items);
            else _inner._count = BHX.PushMany<T, P>(_inner._inner, _inner._count, items);
        }
        public void PushMany(ICollection<T> items)
        {
            if (_inner.FreeCount < items.Count) throw new OverflowException("[BinaryHeap:Push] not enough capacity in heap");
            else if (_inner._offset > 0) _inner._count = BHX.PushMany<T, P>(_inner._inner, _inner._count, _inner._offset, items);
            else _inner._count = BHX.PushMany<T, P>(_inner._inner, _inner._count, items);
        }
        public T Pop()
        {
            T elem;
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            else if (_inner._offset > 0) (elem, _inner._count) = BHX.Pop<T, P>(_inner._inner, _inner._count, _inner._offset);
            else (elem, _inner._count) = BHX.Pop<T, P>(_inner._inner, _inner._count);
            return elem;
        }

        public IEnumerable<T> PopMany(int itemCount)
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            var many = _inner._offset > 0 ?
                BHX.PopMany<T, P>(_inner._inner, itemCount, _inner._count, _inner._offset) :
                BHX.PopMany<T, P>(_inner._inner, itemCount, _inner._count);
            foreach (var item in many)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopAll()
        {
            if (Empty) throw new OverflowException("[BinaryHeap:Pop] no more item in heap");
            var all = _inner._offset > 0 ?
                BHX.PopAll<T, P>(_inner._inner, _inner._count, _inner._offset) :
                BHX.PopAll<T, P>(_inner._inner, _inner._count);

            foreach (var item in all)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopWhileLessOrEqual(P limit)
        {
            if (Empty) yield break;
            var until = _inner._offset > 0 ?
                BHX.PopWhileLessOrEqual<T, P>(_inner._inner, limit, _inner._count, _inner._offset) :
                BHX.PopWhileLessOrEqual<T, P>(_inner._inner, limit, _inner._count);

            foreach (var item in until)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public IEnumerable<T> PopWhileLess(P limit)
        {
            if (Empty) yield break;
            var until = _inner._offset > 0 ?
                BHX.PopWhileLess<T, P>(_inner._inner, limit, _inner._count, _inner._offset) :
                BHX.PopWhileLess<T, P>(_inner._inner, limit, _inner._count);

            foreach (var item in until)
            {
                _inner._count = item.heapCount;
                yield return item.elem;
            }
        }

        public bool Drop(T item)
        {
            bool suc;
            if (_inner._offset > 0) (suc, _inner._count) = BHX.Drop<T, P>(_inner._inner, item, _inner._count, _inner._offset);
            else (suc, _inner._count) = BHX.Drop<T, P>(_inner._inner, item, _inner._count);
            return suc;
        }

        public bool Drop(P priority)
        {
            bool suc;
            if (_inner._offset > 0) (suc, _inner._count) = BHX.Drop<T, P>(_inner._inner, priority, _inner._count, _inner._offset);
            else (suc, _inner._count) = BHX.Drop<T, P>(_inner._inner, priority, _inner._count);
            return suc;
        }

        public bool DropMany(int itemCount)
        {
            bool suc;
            if (_inner._offset > 0) (suc, _inner._count) = BHX.DropMany<T, P>(_inner._inner, itemCount, _inner._count, _inner._offset);
            else (suc, _inner._count) = BHX.DropMany<T, P>(_inner._inner, itemCount, _inner._count);
            return suc;
        }

        public void DropWhileLessOrEqual(P limit)
        {
            if (_inner._offset > 0) _inner._count = BHX.DropWhileLessOrEqual<T, P>(_inner._inner, limit, _inner._count, _inner._offset);
            else _inner._count = BHX.DropWhileLessOrEqual<T, P>(_inner._inner, limit, _inner._count);
        }

        public void DropWhileLess(P limit)
        {
            if (_inner._offset > 0) _inner._count = BHX.DropWhileLess<T, P>(_inner._inner, limit, _inner._count, _inner._offset);
            else _inner._count = BHX.DropWhileLess<T, P>(_inner._inner, limit, _inner._count);
        }

        public void Sort()
        {
            if (_inner._offset > 0) BHX.SortMinHeap<T, P>(_inner._inner, _inner._count, _inner._offset);
            else BHX.SortMinHeap<T, P>(_inner._inner, _inner._count);
        }
        public T this[int index] => _inner[index];
        #endregion IBinaryHeap
        //----------------------------------------------------------------------------------
        #region ICollection
        public int Count => _inner.Count;
        public bool IsReadOnly => true;
        public void Add(T item) => Push(item);
        public void Clear() => _inner.Clear();
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public bool Remove(T item) => Drop(item);
        #endregion ICollection
        //----------------------------------------------------------------------------------
        #region IEnumerator
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        #endregion IEnumerator
        //----------------------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"[");
            int countLmt = _inner.Count.MaxAt(16);
            for (int i = 0; i < countLmt; i++)
            {
                sb.Append(this[i]);
                if (i < countLmt - 1)
                {
                    if (i == 0 || i == 2 || i == 6) sb.Append(';');
                    else sb.Append(',');
                }
            }
            if (_inner.Count > countLmt) sb.Append("...");
            sb.Append("]");
            return sb.ToString();
        }
    }

}