/************************************************************************************
| File: BufferXSeg.cs                                                               |
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
    using static System.Runtime.InteropServices.Marshal;
    using static System.Buffer;

    [DebuggerDisplay("ToString()", Name = "BufferXSeg({_count})")]
    public struct BufferXSeg<T> : IListX<T> where T : unmanaged
    {
        public static readonly BufferXSeg<T> Null = default;
        public static int _elemLen => BufferX<T>._elemLen;

        //internal BufferX<T> _inner;
        internal IntPtr _ptrBuf;
        internal int _offset;
        internal int _capacity;
        internal int _count;
        internal bool _isReadOnly;
        internal int _maxCapacity;

        //internal int _allocByteSize => _inner._allocByteSize;
        private int CapacityOffset => _offset + _capacity;
        private int CountOffset => _offset + _count;

        public int Offset => _offset;
        public int Capacity => _capacity;
        public int Count => _count;
        public int MaxCapacity => _maxCapacity;

        public bool IsEmpty => _count <= 0;
        public bool NotEmpty => _count > 0;

        public int FreeCount => _capacity - _count;
        public bool IsFixedSize => true;
        public bool IsReadOnly { get => _isReadOnly; set => _isReadOnly = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BufferXSeg(BufferX<T> inner, int offset, int capacity)
        {
            if (inner._ptrBuf == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            if (offset < 0 || capacity < 0) throw new OverflowException("out range inner");
            _ptrBuf = inner._ptrBuf;
            _maxCapacity = inner.AllocCapacity;
            if (offset + capacity > _maxCapacity) throw new OverflowException("out range inner");
            _offset = offset;
            _capacity = capacity;
            _count = 0;
            _isReadOnly = inner._isReadOnly;
        }

        public BufferXSeg<T> Next(int capacity = -1)
        {
            if (_ptrBuf == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            int offset = _capacity + _offset;
            if (capacity < 0) capacity = _maxCapacity - offset;
            var seg = this;
            seg._offset = offset;
            seg._count = 0;
            seg._capacity = capacity;
            return seg;
        }
        public BufferXSeg<T> Shift(int offset, int capacity = -1)
        {
            if (_ptrBuf == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            offset = offset + _capacity + _offset;
            if (capacity < 0) capacity = _maxCapacity - offset;
            var seg = this;
            seg._offset = offset;
            seg._count = 0;
            seg._capacity = capacity;
            return seg;
        }
        public BufferXSeg<T> SubNext(int capacity = -1)
        {
            if (_ptrBuf == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            int offset = _count + _offset;
            if (capacity < 0) capacity = _capacity - _count;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("SubSegment count out range parent segment");
            var seg = this;
            seg._offset = offset;
            seg._count = 0;
            seg._capacity = capacity;
            return seg;
        }

        public BufferXSeg<T> SubShift(int offset, int capacity = -1)
        {
            if (_ptrBuf == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            offset = offset + _count;
            if (capacity < 0) capacity = _capacity - offset;
            offset += _offset;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("SubSegment count out range parent segment");
            var seg = this;
            seg._offset = offset;
            seg._count = 0;
            seg._capacity = capacity;
            return seg;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
                return PtrToStructure<T>(_ptrBuf + ((index + _offset) * _elemLen));
            }
            set
            {
                if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
                if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
                StructureToPtr(value, _ptrBuf + ((index + _offset) * _elemLen), false);
            }
        }

        public bool Contains(T item) => IndexOf(item) < 0;

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
                if (item.Equals(this[i]))
                    return i;
            return -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _count; arrayIndex++, i++)
                array[arrayIndex] = this[i];
        }

        public void Add(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (_count < _capacity) this[_count++] = item;
            else throw new OverflowException(" out range Capacity");
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
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
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int countAfterAdd = _count + elems.Length;
            if (countAfterAdd > _capacity) throw new OverflowException("out range Capacity");
            int countGrow = _count;
            for (; countGrow < countAfterAdd; countGrow++) this[countGrow] = elems[countGrow - _count];
            _count = countGrow;
        }

        public void Insert(int index, T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (_count >= _capacity) throw new OverflowException(" our range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException(" index out range count");
            int afterIndex = index + 1;
            int byteToShift = (_count - index) * _elemLen;
            int byteCapAfterInsertPoint = (_capacity - afterIndex) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    (_ptrBuf + index).ToPointer(),
                    (_ptrBuf + afterIndex).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            this[index] = item;
            _count++;
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int byteToShift = (_count - index) * _elemLen;
            var temp = BufferX<T>.cache.AllocateRaw(byteToShift);
            unsafe
            {
                MemoryCopy(
                    (_ptrBuf + index).ToPointer(),
                    temp.buf.ToPointer(),
                    temp.SizeOfByte, byteToShift);
            }
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
                    BufferX<T>.cache.FreeRaw(temp);
                    throw new OverflowException("out range Capacity");
                }
            }

            int byteCapAfterInsertPoint = (_capacity - index) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    temp.buf.ToPointer(),
                    (_ptrBuf + index).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            BufferX<T>.cache.FreeRaw(temp);
            _count += elemInserted;
        }

        public void InsertMany(int index, params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int elemToInsert = elems.Length;
            if (_count + elemToInsert > _capacity) throw new OverflowException("out range Capacity");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterIndex = index + elemToInsert;
            int byteToShift = (_count - index) * _elemLen;
            int byteCapAfterInsertPoint = (_capacity - afterIndex) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    (_ptrBuf + index).ToPointer(),
                    (_ptrBuf + afterIndex).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }

            for (int i = 0; i < elemToInsert; i++) this[i + index] = elems[i];
            _count += elemToInsert;
        }


        public bool Remove()
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (_count > 0) { _count--; return true; }
            else return false;
        }

        public bool Remove(T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (1 > _count) throw new OverflowException("out range count");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterRemove = index + 1;
            int byteToShift = (_count - afterRemove) * _elemLen;
            int byteCapAfterInsertPoint = (_capacity - afterRemove) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    (this._ptrBuf + afterRemove).ToPointer(),
                    (this._ptrBuf + index).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            _count -= 1;
        }

        public bool RemoveMany(int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (count >= 0 && _count >= count) { _count -= count; return true; }
            else return false;
        }

        public void RemoveRange(int index, int count)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (count < 0 || count > _count) throw new OverflowException("out range count");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int afterRemove = index + count;
            int byteToShift = (_count - afterRemove) * _elemLen;
            int byteCapAfterInsertPoint = (_capacity - afterRemove) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    (_ptrBuf + afterRemove).ToPointer(),
                    (_ptrBuf + index).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            _count -= count;
        }

        public void Clear()
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return this[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            if (_ptrBuf == IntPtr.Zero) return "null";
            else
            {
                StringBuilder sb = new StringBuilder($"{typeof(T).Name}[{_count}] ({_offset},{_count + _offset}:{_capacity}) [");
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