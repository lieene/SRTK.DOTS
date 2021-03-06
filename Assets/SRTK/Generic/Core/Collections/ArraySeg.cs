/************************************************************************************
| File: ArraySeg.cs                                                                 |
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
using System.Runtime.CompilerServices;

namespace SRTK
{
    [DebuggerDisplay("ToString()", Name = "ArraySeg({_count})")]
    public struct ArraySeg<T> : IListX<T>
    {
        internal T[] _inner;
        internal long _offset;
        internal int _capacity;
        internal int _count;
        internal bool _isReadOnly;


        private long CapacityOffset => _offset + _capacity;
        private long CountOffset => _offset + _count;

        public T[] Inner => _inner;
        public long Offset => _offset;
        public int Capacity => _capacity;
        public int Count => _count;

        public bool IsEmpty => _count <= 0;
        public bool NotEmpty => _count > 0;

        public int FreeCount => _capacity - _count;

        public bool IsFixedSize => true;
        public bool IsReadOnly { get => _isReadOnly; set => _isReadOnly = value; }


        public ArraySeg(T[] inner, long offset, int count, int capacity = -1, bool readOnly = false)
        {
            if (inner == null) throw new ArgumentNullException("[ArraySeg:Ctor] inner list is null");
            if (capacity < 0) capacity = (int)(inner.LongLength - offset);
            if (count < 0 || count > capacity)
                throw new ArgumentOutOfRangeException("[ArraySeg:Ctor] capacity out range inner list");
            if (offset < 0 || capacity < 0 || (offset + capacity) > inner.LongLength)
                throw new OverflowException("[ArraySeg:Ctor] capacity out range inner list");
            _inner = inner;
            _offset = offset;
            _capacity = capacity;
            _count = count;
            _isReadOnly = readOnly;
        }

        public ArraySeg<T> Next(int count, int capacity = -1)
        {
            long offset = _capacity + _offset;
            if (capacity < 0) capacity = (int)(_inner.LongLength - offset);
            return new ArraySeg<T>(_inner, offset, count, capacity);
        }
        public ArraySeg<T> Shift(long offset, int count, int capacity = -1)
        {
            offset = offset + _capacity + _offset;
            if (capacity < 0) capacity = (int)(_inner.LongLength - offset);
            return new ArraySeg<T>(_inner, offset, count, capacity);
        }
        public ArraySeg<T> SubNext(int count, int capacity = -1)
        {
            long offset = _count + _offset;
            if (capacity < 0) capacity = _capacity - _count;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("[ArraySeg:Sub] SubSegment count out range parent segment");
            return new ArraySeg<T>(_inner, offset, count, capacity);
        }

        public ArraySeg<T> SubShift(long offset, int count, int capacity = -1)
        {
            offset = offset + _count;
            if (capacity < 0) capacity = (int)(_capacity - offset);
            offset += _offset;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("[ArraySeg:Sub] SubSegment count out range parent segment");
            return new ArraySeg<T>(_inner, offset, count, capacity);
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
                if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
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

        public void CopyTo(T[] array, int arrayIndex) =>
            Array.Copy(_inner, _offset, array, arrayIndex, _count);

        public void Add(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (_count < _capacity) _inner[_count++] = item;
            else throw new OverflowException("[ListSeg:Add] out range Capacity");
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
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
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            int countAfterAdd = _count + elems.Length;
            if (countAfterAdd > _capacity) throw new OverflowException("out range Capacity");
            int countGrow = _count;
            for (; countGrow < countAfterAdd; countGrow++) this[countGrow] = elems[countGrow - _count];
            _count = countGrow;
        }


        public void Insert(int index, T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (_count >= _capacity) throw new OverflowException(" our range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException(" index out range count");
            int afterIndex = index + 1;
            int elemToShift = _count - index;
            Array.Copy(_inner, index, _inner, afterIndex, elemToShift);
            this[index] = item;
            _count++;
        }

        internal static Pool.IArrayCache<T> cache = Pool.CacheEx.GrabArrayCache<T>();
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            int elemToShift = _count - index;
            var temp = cache.Allocate(elemToShift);
            Array.Copy(_inner, index, temp._inner, 0, elemToShift);
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
                    cache.Free(temp);
                    throw new OverflowException("out range Capacity");
                }
            }
            Array.Copy(temp._inner, 0, _inner, index, elemToShift);
            cache.Free(temp);
            _count += elemInserted;
        }

        public void InsertMany(int index, params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            int elemToInsert = elems.Length;
            if (_count + elemToInsert > _capacity) throw new OverflowException("out range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterIndex = index + elemToInsert;
            int elemToShift = _count - index;
            Array.Copy(_inner, index, _inner, afterIndex, elemToShift);
            for (int i = 0; i < elemToInsert; i++) this[i + index] = elems[i];
            _count += elemToInsert;
        }

        public bool Remove(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            int idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);
            return true;
        }

        public bool Remove()
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (_count > 0) { _count--; return true; }
            else return false;
        }

        public void RemoveAt(int index)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (1 > _count) throw new OverflowException("out range count");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterRemove = index + 1;
            int elemToShift = _count - afterRemove;
            Array.Copy(_inner, afterRemove, _inner, index, elemToShift);
            _count -= 1;
        }


        public bool RemoveMany(int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (count >= 0 && _count >= count) { _count -= count; return true; }
            else return false;
        }

        public void RemoveRange(int index, int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
            if (count < 0 || count > _count) throw new OverflowException("out range count");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterRemove = index + count;
            int elemToShift = _count - afterRemove;
            Array.Copy(_inner, afterRemove, _inner, index, elemToShift);
            _count -= count;
        }

        public void Clear()
        {
            if (_isReadOnly) throw new NotSupportedException("Array Segment is readonly");
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
                StringBuilder sb = new StringBuilder($"{typeof(T).Name}[{_count}] ({_offset},{_count + _offset}:{_capacity}/{_inner.LongLength}) [");
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