using System.Runtime.InteropServices;
/************************************************************************************
| File: BufferX.cs                                                                  |
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
using SRTK.Pool;

namespace SRTK
{
    using static System.Runtime.InteropServices.Marshal;
    using static System.Buffer;

    [DebuggerDisplay("ToString()", Name = "BufferX({_count})")]
    public struct BufferX<T> : IListX<T> where T : unmanaged
    {
        public static readonly BufferX<T> Null = default;
        public static readonly int _elemLen = SizeOf(default(T));

        internal IntPtr _ptrBuf;
        internal int _capacity;
        internal int _count;
        internal int _allocByteSize;

        internal bool _isFixedSize;
        internal bool _isReadOnly;

        public int AllocCapacity => _allocByteSize / _elemLen;

        public int Capacity => _capacity;
        public int Count => _count;

        public bool IsEmpty => _count <= 0;
        public bool NotEmpty => _count > 0;

        public int FreeCount => _capacity - _count;

        public bool IsFixedSize { get => _isFixedSize; set => _isFixedSize = value; }
        public bool IsReadOnly { get => _isReadOnly; set => _isReadOnly = value; }

        public static IBufferCache cache = CacheEx.BufferCache;

        public BufferX(int capacity, bool readOnly = false, bool fixedSize = false)
        {
            var rawBuf = cache.AllocateRaw(capacity * _elemLen);
            _allocByteSize = rawBuf.SizeOfByte;
            _ptrBuf = rawBuf.buf;
            _count = 0;
            _capacity = capacity;
            _isReadOnly = readOnly;
            _isFixedSize = fixedSize;
        }

        internal BufferX(IntPtr buffer, int capacity, int allocByteSize)
        {
            if (buffer == IntPtr.Zero) throw new ArgumentNullException("IntPtr is null");
            if (capacity * _elemLen > allocByteSize)
                throw new OverflowException("capacity out range IntPtr");
            _ptrBuf = buffer;
            _count = 0;
            _capacity = capacity;
            _allocByteSize = allocByteSize;
            _isReadOnly = false;
            _isFixedSize = false;
        }

        public BufferXSeg<T> Next(int capacity = -1)
        {
            int offset = _capacity;
            if (capacity < 0) capacity = AllocCapacity - offset;
            return new BufferXSeg<T>(this, offset, capacity);
        }
        public BufferXSeg<T> Shift(int offset, int capacity = -1)
        {
            offset = offset + _capacity;
            if (capacity < 0) capacity = AllocCapacity - offset;
            return new BufferXSeg<T>(this, offset, capacity);
        }
        public BufferXSeg<T> SubNext(int capacity = -1)
        {
            int offset = _count;
            if (capacity < 0) capacity = _capacity - offset;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("SubSegment count out range parent segment");
            return new BufferXSeg<T>(this, offset, capacity);
        }

        public BufferXSeg<T> SubShift(int offset, int capacity = -1)
        {
            offset = offset + _count;
            if (capacity < 0) capacity = _capacity - offset;
            if (offset + capacity > _capacity) throw new ArgumentOutOfRangeException("SubSegment count out range parent segment");
            return new BufferXSeg<T>(this, offset, capacity);
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
                return PtrToStructure<T>(_ptrBuf + (index * _elemLen));
            }
            set
            {
                if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
                if (index < 0 || index >= _count) throw new IndexOutOfRangeException();
                StructureToPtr(value, _ptrBuf + (index * _elemLen), false);
            }
        }

        public bool Contains(T item) => IndexOf(item) < 0;

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++) if (item.Equals(this[i])) return i;
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
            if (_count >= Capacity)
            {
                if (_isFixedSize) throw new OverflowException("out range Capacity");
                else Grow();
            }
            this[_count++] = item;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int countGrow = _count;
            foreach (var item in collection)
            {
                if (countGrow >= Capacity)
                {
                    if (_isFixedSize) throw new OverflowException("out range Capacity");
                    else Grow();
                }
                this[countGrow++] = item;
            }
            _count = countGrow;
        }

        public void AddMany(params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int countAfterAdd = _count + elems.Length;
            if (countAfterAdd > _capacity)
            {
                if (_isFixedSize) throw new OverflowException("out range Capacity");
                else Grow();
            }
            int countGrow = _count;
            for (; countGrow < countAfterAdd; countGrow++) this[countGrow] = elems[countGrow - _count];
            _count = countGrow;
        }

        public void Insert(int index, T item)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            if (_count >= _capacity)
            {
                if (_isFixedSize) throw new OverflowException("out range Capacity");
                else Grow();
            }
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

        public void InsertMany(int index, params T[] elems)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            int elemToInsert = elems.Length;
            if (_count + elemToInsert > _capacity)
            {
                if (_isFixedSize) throw new OverflowException("out range Capacity");
                else Grow();
            }
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

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            if (index < 0 || index > _count) throw new IndexOutOfRangeException("index out range count");
            int byteToShift = (_count - index) * _elemLen;
            var temp = cache.AllocateRaw(byteToShift);
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
                else if (_isFixedSize)
                {
                    cache.FreeRaw(temp);
                    throw new OverflowException("out range Capacity");
                }
                else Grow();
            }

            int byteCapAfterInsertPoint = (_capacity - index) * _elemLen;
            unsafe
            {
                MemoryCopy(
                    temp.buf.ToPointer(),
                    (_ptrBuf + index).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            cache.FreeRaw(temp);
            _count += elemInserted;
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
            if (idx < 0) { return false; }
            else { RemoveAt(idx); return true; }
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
                    (this._ptrBuf + afterRemove).ToPointer(),
                    (this._ptrBuf + index).ToPointer(),
                    byteCapAfterInsertPoint, byteToShift);
            }
            _count -= count;
        }

        public void Clear()
        {
            if (_isReadOnly) throw new NotSupportedException("Buffer is readonly");
            _count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            if (_ptrBuf == IntPtr.Zero) return "null";
            else
            {
                StringBuilder sb = new StringBuilder($"{typeof(T).Name}[{_count}] ({_capacity}) [");
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

        public bool Shrink(bool keepElement = true)
        {
            var shrinkElemCap = _capacity >> 1;
            if (shrinkElemCap == 0) return false;
            int byteToCopy;
            if (_count > shrinkElemCap)
            {
                if (keepElement) return false;
                else byteToCopy = shrinkElemCap * _elemLen;
            }
            else byteToCopy = _count * _elemLen;

            if (keepElement && _count > shrinkElemCap) return false;

            var bufHalf = cache.AllocateRaw((shrinkElemCap) * _elemLen);
            unsafe
            {
                Buffer.MemoryCopy(_ptrBuf.ToPointer(), bufHalf.buf.ToPointer(), bufHalf.bSize, byteToCopy);
            }
            cache.FreeRaw(new RawBuffer() { buf = _ptrBuf, bSize = _allocByteSize });
            _ptrBuf = bufHalf.buf;
            _allocByteSize = bufHalf.bSize;
            return true;
        }

        public void Grow()
        {
            var growElemCap = _capacity << 1;
            var bufx2 = cache.AllocateRaw((growElemCap) * _elemLen);
            unsafe
            {
                Buffer.MemoryCopy(_ptrBuf.ToPointer(), bufx2.buf.ToPointer(), bufx2.bSize, _count * _elemLen);
            }
            cache.FreeRaw(new RawBuffer() { buf = _ptrBuf, bSize = _allocByteSize });
            _ptrBuf = bufx2.buf;
            _allocByteSize = bufx2.bSize;
        }

        public void Free() { cache.FreeRaw(Recyle()); }

        internal RawBuffer Recyle()
        {
            var rb = new RawBuffer() { buf = _ptrBuf, bSize = _allocByteSize };
            _ptrBuf = IntPtr.Zero;
            _allocByteSize = 0;
            _count = 0;
            _capacity = 0;
            return rb;
        }
    }
}