/************************************************************************************
| File: Segment.cs                                                                  |
| Project: SRTK.ListSegment                                                         |
| Created Date: Mon Sep 16 2019                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 15 2019                                                    |
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
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace SRTK
{
    [DebuggerDisplay("ToString()", Name = "Segment({_count})")]
    public struct Segment<T, TL> : IListX<T> where TL : IListX<T>
    {
        internal TL _inner;
        internal int _offset;
        internal int _capacity;
        internal int _count;
        internal bool _isReadOnly;

        internal int CapacityOffset => _offset + _capacity;
        internal int CountOffset => _offset + _count;

        public TL Inner => _inner;
        public int Offset => _offset;
        public int Capacity => _capacity;
        public int Count => _count;

        public bool IsEmpty => _count <= 0;
        public bool NotEmpty => _count > 0;

        public int FreeCount => _capacity - _count;
        public bool IsFixedSize => true;
        public bool IsReadOnly { get => _isReadOnly; set => _isReadOnly = value; }


        public Segment(TL inner, int offset, int count, int capacity = -1)
        {
            if (inner == null) throw new ArgumentNullException("[ListSeg:Ctor] inner list is null");
            if (capacity < 0) capacity = inner.Count - offset;
            if (count < 0 || count > capacity)
                throw new ArgumentOutOfRangeException("[ListSeg:Ctor] capacity out range inner list");
            if (offset < 0 || capacity < 0 || (offset + capacity) > inner.Count)
                throw new OverflowException("[ListSeg:Ctor] capacity out range inner list");
            _inner = inner;
            _offset = offset;
            _capacity = capacity;
            _count = count;
            _isReadOnly = _inner.IsReadOnly;
        }

        public Segment<T, TL> Next(int count, int capacity = -1)
        {
            int offset = _capacity + _offset;
            if (capacity < 0) capacity = (int)(_inner.Count - offset);
            return new Segment<T, TL>(_inner, offset, count, capacity);
        }

        public Segment<T, TL> Shift(int offset, int count, int capacity = -1)
        {
            offset = offset + _capacity + _offset;
            if (capacity < 0) capacity = (int)(_inner.Count - offset);
            return new Segment<T, TL>(_inner, offset, count, capacity);
        }

        public Segment<T, TL> SubNext(int count, int capacity = -1)
        {
            int offset = _count + _offset;
            if (capacity < 0) capacity = _capacity - _count;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("[Segment:Sub] SubSegment count out range parent segment");
            return new Segment<T, TL>(_inner, offset, count, capacity);
        }

        public Segment<T, TL> SubShift(int offset, int count, int capacity = -1)
        {
            offset = offset + _count;
            if (capacity < 0) capacity = (int)(_capacity - offset);
            offset += _offset;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("[Segment:Sub] SubSegment count out range parent segment");
            return new Segment<T, TL>(_inner, offset, count, capacity);
        }

        public T this[int index]
        {
            get
            {
                if (index < _offset || index >= _count)
                    throw new IndexOutOfRangeException();
                return _inner[index + _offset];
            }
            set
            {
                if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
                if (index < _offset || index >= _count)
                    throw new IndexOutOfRangeException();
                _inner[index + _offset] = value;
            }
        }

        public bool Contains(T item) => IndexOf(item) < 0;

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
                if (item.Equals(_inner[i + _offset]))
                    return i;
            return -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _count; i++)
                array[arrayIndex + i] = _inner[i + _offset];
        }

        public void Add(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (_count < _capacity) _inner[_count++] = item;
            else throw new OverflowException("out range Capacity");
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            int countGrow = _count;
            foreach (var item in collection)
            {
                if (countGrow >= Capacity) throw new OverflowException("out range Capacity");
                this[countGrow++] = item;
            }
            _count = countGrow;
        }

        public void AddMany(params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            int countAfterAdd = _count + elems.Length;
            if (countAfterAdd > _capacity) throw new OverflowException("out range Capacity");
            int countGrow = _count;
            for (; countGrow < countAfterAdd; countGrow++) this[countGrow] = elems[countGrow - _count];
            _count = countGrow;
        }


        public void Insert(int index, T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (_count >= _capacity) throw new OverflowException(" our range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException(" index out range count");
            for (int i = _count; i > index; i--)
                this[i] = this[i - 1];
            this[index] = item;
            _count++;
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            int elemToShift = _count - index;
            var temp = ArraySeg<T>.cache.Allocate(elemToShift);
            for (int i = 0; i < elemToShift; i++)
            { temp[i] = this[i + index]; }
            int elemInserted = 0;
            foreach (var item in collection)
            {
                if (index < _capacity)
                {
                    this[index++] = item;
                    elemInserted++;
                }
                else
                {
                    ArraySeg<T>.cache.Free(temp);
                    throw new OverflowException("out range Capacity");
                }
            }
            for (int i = 0; i < elemToShift; i++)
            { this[i + index] = temp[i]; }
            ArraySeg<T>.cache.Free(temp);
            _count += elemInserted;
        }

        public void InsertMany(int index, params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            int insertCount = elems.Length;
            if (_count + insertCount > _capacity) throw new OverflowException(" our range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException(" index out range count");
            for (int i = _count; i > index; i--)
                this[i + insertCount - 1] = this[i - 1];

            for (int i = 0; i < insertCount; i++)
            { this[i + index] = elems[i]; }
            _count++;
        }
        
        public bool Remove(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            int idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range segment");
            for (int i = index; i < _count; i++) _inner[i] = _inner[i + 1];
            _count--;
        }

        public bool Remove()
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (_count > 0) { _count--; return true; }
            else return false;
        }

        public bool RemoveMany(int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (count >= 0 && _count >= count) { _count -= count; return true; }
            else return false;
        }

        public void RemoveRange(int index, int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            if (count < 0 || index + count > _count) throw new OverflowException("out range count");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterCount=_count-count;
            for (int i = index; i < afterCount; i++) _inner[i] = _inner[i + count];
            _count = afterCount;
        }

        public void Clear()
        {
            if (_isReadOnly) throw new NotSupportedException("Segment is readonly");
            _count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _inner[i + _offset];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            if (_inner == null) return "null";
            else
            {
                StringBuilder sb = new StringBuilder($"{typeof(T).Name}[{_count}] ({_offset},{_count + _offset}:{_capacity}/{_inner.Count}) [");
                int countLmt = _count.MaxAt(16);
                for (int i = 0; i < countLmt; i++)
                {
                    sb.Append(this[i]);
                    if (i < countLmt - 1) sb.Append(',');
                }
                if (_capacity > countLmt) sb.Append("...");
                sb.Append("]");
                return sb.ToString();
            }
        }
    }
}