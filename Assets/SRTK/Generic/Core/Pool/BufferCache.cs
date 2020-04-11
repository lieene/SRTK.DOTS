using System.IO;
/************************************************************************************
| File: BufferCache.cs                                                              |
| Project: SRTK.Pool                                                                |
| Created Date: Sun Sep 8 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 22 2019                                                    |
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
using System.Threading;

namespace SRTK.Pool
{
    using static MathX;
    using static CacheEx;
    using static System.Runtime.InteropServices.Marshal;
    //using static RawBuffer;

    //----------------------------------------------------------------------------------
    public class FixedSizeBufferCache : IBufferCache
    {
        internal readonly int _byteSize;

        public FixedSizeBufferCache(int cacheCapacity, int byteSize)
        {
            if (byteSize < 1) throw new ArgumentOutOfRangeException();
            cacheCapacity = cacheCapacity.MinAt(1);
            int itemLen = cacheCapacity - 1;
            _items = new Element[itemLen];
            _firstItem = IntPtr.Zero;
            for (int i = 0; i < itemLen; i++) _items[i] = Element.Null;
            _byteSize = byteSize;
        }

        private struct Element
        {
            public static readonly Element Null = new Element { Value = IntPtr.Zero };
            public IntPtr Value;
        }

        private IntPtr _firstItem;
        private readonly Element[] _items;

        internal IntPtr TryAlloc()
        {
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            IntPtr inst = _firstItem;
            if (inst == IntPtr.Zero || inst != Interlocked.CompareExchange(ref _firstItem, IntPtr.Zero, inst))
            {
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    // Note that the initial read is optimistically not synchronized. That is intentional. 
                    // We will interlock only when we have a candidate. in a worst case we may miss some
                    // recently returned objects. Not a big deal.
                    inst = _items[i].Value;
                    if (inst != IntPtr.Zero && inst == Interlocked.CompareExchange(ref _items[i].Value, IntPtr.Zero, inst))
                        break;
                }
            }
            return inst;
        }

        public BufferX<T> Allocate<T>(int size) where T : unmanaged
        {
            int byteSize = size * BufferX<T>._elemLen;
            if (byteSize > _byteSize) throw new OverflowException();

            IntPtr inst = TryAlloc();
            if (inst == IntPtr.Zero) inst = AllocHGlobal(_byteSize);
            if (inst == IntPtr.Zero) throw new NullReferenceException("Not Enough Memory");
            return new BufferX<T>(inst, size, _byteSize);
        }

        public RawBuffer AllocateRaw(int byteSize)
        {
            RawBuffer buf;
            if (byteSize > _byteSize) throw new OverflowException();
            buf.buf = TryAlloc();
            buf.bSize = _byteSize;
            return buf;
        }

        public void FreeRaw(RawBuffer buf)
        {
            IntPtr Slot = IntPtr.Zero;
            if (buf.buf == IntPtr.Zero) return;
            if (buf.bSize != _byteSize) throw new ArgumentException("Buffer size miss match");
            //buf.Clear();
            if (IntPtr.Zero != (Slot = Interlocked.CompareExchange(ref _firstItem, buf.buf, IntPtr.Zero)))
            {
                // searchIndex is used to track max freeing slot index
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    Slot = Interlocked.CompareExchange(ref _items[i].Value, buf.buf, IntPtr.Zero);
                    if (Slot == IntPtr.Zero) return;
                }
            }
            else return;
            //Slot != IntPtr.Zero, Empty cache slot not found
            FreeHGlobal(buf.buf);
        }

        public void FreeRaw(ref RawBuffer buf)
        {
            FreeRaw(buf);
            buf = RawBuffer.Null;
        }

        public void Free<T>(BufferX<T> buf) where T : unmanaged
        {
            IntPtr Slot = IntPtr.Zero;
            if (buf._ptrBuf == IntPtr.Zero) return;
            if (buf._allocByteSize != _byteSize) throw new ArgumentException("Buffer size miss match");
            //buf.Clear();
            if (IntPtr.Zero != (Slot = Interlocked.CompareExchange(ref _firstItem, buf._ptrBuf, IntPtr.Zero)))
            {
                // searchIndex is used to track max freeing slot index
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    Slot = Interlocked.CompareExchange(ref _items[i].Value, buf._ptrBuf, IntPtr.Zero);
                    if (Slot == IntPtr.Zero) return;
                }
            }
            else return;
            //Slot != IntPtr.Zero, Empty cache slot not found
            FreeHGlobal(buf._ptrBuf);
        }

        public void Free<T>(ref BufferX<T> buf) where T : unmanaged
        {
            Free(buf);
            buf = BufferX<T>.Null;
        }


        /// <summary>
        /// number of cached objects in cache
        /// </summary>
        public int Useage
        {
            get
            {
                int counter = 0;
                if (_firstItem != IntPtr.Zero) counter++;
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    if (_items[i].Value != IntPtr.Zero) counter++;
                }
                return counter;
            }
        }

        /// <summary>
        /// [0-1] ratio of used cache solts vs. cache Capacity
        /// </summary>
        public float UseageRate { get { return Useage / (float)(_items.Length + 1); } }

        public void Release()
        {
            IntPtr inst = Interlocked.Exchange(ref _firstItem, IntPtr.Zero);
            if (inst != IntPtr.Zero) FreeHGlobal(inst);
            int length = _items.Length;
            for (int i = 0; i < length; i++)
            {
                inst = Interlocked.Exchange(ref _items[i].Value, IntPtr.Zero);
                if (inst != IntPtr.Zero) FreeHGlobal(inst);
            }
        }
    };

    public class DynamicSizeBufferCache : IBufferCache
    {
        //----------------------------------------------------------------------------------
        #region Fields/Properties 

        private readonly RawBuffer[] _items;
        private int _items_capacity;
        private int _cacheCapacity;
        private RawBuffer _firstItem;
        public int PoolCapacity => _cacheCapacity;

        #endregion Fields/Properties 
        //----------------------------------------------------------------------------------
        #region Ctor 

        public DynamicSizeBufferCache(int capacity)
        {
            _cacheCapacity = capacity.MinAt(1);
            _items_capacity = _cacheCapacity - 1;
            _firstItem = RawBuffer.Null;
            if (_items_capacity > 0)
            {
                _items = new RawBuffer[_items_capacity];
                for (int i = 0; i < _items_capacity; i++) _items[i] = RawBuffer.Null;
            }
            else _items = null;
        }

        #endregion Ctor 
        //----------------------------------------------------------------------------------
        #region Allocate

        public BufferX<T> Allocate<T>(int aSize) where T : unmanaged
        {
            var buf = AllocateRaw(aSize * BufferX<T>._elemLen);
            return new BufferX<T>(buf.buf, aSize, buf.bSize);
        }

        public RawBuffer AllocateRaw(int byteSize)
        {
            var buf = TryAllocate(byteSize);
            if (buf.buf == IntPtr.Zero)
            {
#if DEBUG
                DebugX.Log($"DynamicSizeBufferPool allocate ({byteSize})");
#endif
                buf.buf = AllocHGlobal(byteSize);
                buf.bSize = byteSize;
                return buf;
            }
            else
            {
                if (buf.bSize < byteSize) throw new InternalBufferOverflowException();
                return buf;
            }
        }

        #endregion Allocate
        //----------------------------------------------------------------------------------
        #region Free  

        public void Free<T>(BufferX<T> buf) where T : unmanaged
        {
            //buf.Clear();
            FreeRaw(buf.Recyle());
        }
        public void Free<T>(ref BufferX<T> buf) where T : unmanaged
        {
            //buf.Clear();
            FreeRaw(buf.Recyle());
            buf = BufferX<T>.Null;
        }

        public void FreeRaw(RawBuffer buf)
        {
            IntPtr Slot = IntPtr.Zero;
            if (buf.buf == IntPtr.Zero) return;
            if (IntPtr.Zero == (Slot = Interlocked.CompareExchange(ref _firstItem.buf, buf.buf, IntPtr.Zero)))
            {
                _firstItem.bSize = buf.bSize;
                return;
            }
            else
            {
                // searchIndex is used to track max freeing slot index
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    Slot = Interlocked.CompareExchange(ref _items[i].buf, buf.buf, IntPtr.Zero);
                    if (Slot == IntPtr.Zero)
                    {
                        _items[i].bSize = buf.bSize;
                        return;
                    }
                }
            }
            //Slot != IntPtr.Zero, Empty cache slot not found
            FreeHGlobal(buf.buf);
        }

        public void FreeRaw(ref RawBuffer buf)
        {
            FreeRaw(buf);
            buf = RawBuffer.Null;
        }


        #endregion Free  
        //----------------------------------------------------------------------------------
        #region Try Allocate
        //----------------------------------------------------------------------------------
        short _removeCandidateID = short.MaxValue;
        short _removeVote = 0;
        const short _firstID = -1;
        const short _nullID = short.MaxValue;
        //----------------------------------------------------------------------------------

        internal RawBuffer TryAllocate(int bSize)
        {
            RawBuffer pBuf = _firstItem;

            short smallest = _nullID;
            short cacheCounter = 0;//number of buffer(not null) in the cache, used to indicate cache's usage
            int smallSize = short.MaxValue;

            if (pBuf.buf != IntPtr.Zero)//_firstItem not null
            {
                if (pBuf.bSize >= bSize)// first size match
                {
                    if (pBuf.buf == Interlocked.CompareExchange(ref _firstItem.buf, IntPtr.Zero, pBuf.buf))
                    {//_firstItem allcated
                        _firstItem.bSize = 0;
                        TryClearVote(_firstID);
                        return pBuf;
                    }
                    //else _firstItem taken go on with _items
                    cacheCounter++;
                }
                else// too small to allocate
                {
                    smallest = _firstID;
                    smallSize = _firstItem.bSize;
                    pBuf = RawBuffer.Null;
                }
            }

            if (_items != null)// buffer container not noll
            {//first is null or too small or has been taken from other thread

                for (short i = 0; i < _items_capacity; i++)
                {
                    pBuf = _items[i];
                    if (pBuf.buf != IntPtr.Zero)
                    {//has candidate here
                        if (pBuf.bSize >= bSize)
                        {//candidate match size
                            if (pBuf.buf == Interlocked.CompareExchange(ref _items[i].buf, IntPtr.Zero, pBuf.buf))
                            {//candidate allocated
                                _items[i].bSize = 0;
                                TryClearVote(i);
                                return pBuf;
                            }
                        }
                        else if (pBuf.bSize < smallSize)
                        {//candidate too small to allocate and even smaller than previous smallSize
                            smallest = i;
                            smallSize = pBuf.bSize;
                        }

                        //item not null but cannot use
                        cacheCounter++;
                    }
                }
            }
            //no matching buffer found

            // smallest buffer set
            if (smallest != _nullID) VoteToRemove(smallest, cacheCounter);
            //Else smallest buffer not set
            //Which means cache is all null
            //So no need to Vote/Remove at all

            return RawBuffer.Null;
        }
        #endregion Try Allocate
        //----------------------------------------------------------------------------------
        #region Voting/Remove Small buffer 

        /// <summary>
        /// Clear remove vote when a buffer is allocated
        /// </summary>
        /// <param name="bufferID"></param>
        internal void TryClearVote(short bufferID)
        {
            if (_removeCandidateID == bufferID)
            {
                _removeCandidateID = _nullID;
                _removeVote = 0;
            }
        }

        /// <summary>
        /// Test if samll buffer should be removed for larger buffer to be cacheed
        /// if this cache is full of samll buffers large buffer will keep being GC-collected
        /// keeping large buffer is okay as large buffer can be allocated for smaller size require
        /// But it will hold large amount of memory. Call RemoveLagest/Clear to free memory if needed
        /// TryClearVote will remove vote when a buffer is allocated
        /// So if: cache is fully occupied and buffer are all too small for current allocation
        ///     one small buffer will be removed for sure
        /// or if: cache is parcially occupied and buffer are all too small for current allocation
        ///     one small buffer will be removed when vote add up to cache's capacity
        ///     In this case if: the samll buffer is allocated out off the cache
        ///         votes will be cleared and buffer will not be removed and GC-collected
        ///         the vote will star over from 0 when the small buffer come back to this cache
        ///         it will be removed if it keep being the smallest one
        /// </summary>
        /// <param name="candidateID">small buffer candidate</param>
        /// <param name="vote">vote to remove this small buffer (this value is cache occupancy from try allocate)</param>
        internal void VoteToRemove(short candidateID, short vote)
        {
            if (_removeCandidateID == candidateID)
            {
                _removeVote += vote;
                if (_removeVote >= _cacheCapacity)// for one iteration this meanns cache is full of small buffers
                {
                    //remove smallest
                    if (_removeCandidateID == _firstID)
                    {
                        var ptr = Interlocked.Exchange(ref _firstItem.buf, IntPtr.Zero);
                        if (ptr != IntPtr.Zero)
                        {
                            _firstItem.bSize = 0;
                            FreeHGlobal(ptr);
                        }
                    }
                    else
                    {
                        var ptr = Interlocked.Exchange(ref _items[_removeCandidateID].buf, IntPtr.Zero);
                        if (ptr != IntPtr.Zero)
                        {
                            _items[_removeCandidateID].bSize = 0;
                            FreeHGlobal(ptr);
                        }
                    }
                }
            }
            else
            {
                _removeCandidateID = candidateID;
                _removeVote = 1;
            }
        }
        #endregion Voting to Remove Small buffer
        //----------------------------------------------------------------------------------
        #region Remove/Clear (Large buffer) 

        /// <summary>
        /// Remove the largest buffer
        /// Small buffer ocuping the cache will be remove automaticlly when allocating
        /// Large buffer is safe for allocation But it consumes memory
        /// Call this to free memory..
        /// </summary>
        public void RemoveLargest()
        {
            int largeID = int.MaxValue;
            RawBuffer largBuf = RawBuffer.Null;
            if (_firstItem.buf != IntPtr.Zero)
            {
                largeID = _firstID;
                largBuf = _firstItem;
            }

            for (int i = 0; i < _items_capacity; i++)
            {
                RawBuffer cur = _items[i];
                if (cur.buf != IntPtr.Zero && (largBuf.buf == IntPtr.Zero || largBuf.bSize < cur.bSize))
                {
                    largeID = i;
                    largBuf = cur;
                }
            }
            //remove largest
            if (largeID == _firstID)
            {
                var ptr = Interlocked.Exchange(ref _firstItem.buf, IntPtr.Zero);
                if (ptr != IntPtr.Zero)
                {
                    _firstItem.bSize = 0;
                    FreeHGlobal(ptr);
                }
            }
            else
            {
                var ptr = Interlocked.Exchange(ref _items[largeID].buf, IntPtr.Zero);
                if (ptr != IntPtr.Zero)
                {
                    _items[largeID].bSize = 0;
                    FreeHGlobal(ptr);
                }
            }
        }

        /// <summary>
        /// Clear all buffer instance
        /// /// Small buffer ocuping the cache will be remove automaticlly when allocating
        /// Large buffer is safe for allocation But it consumes memory
        /// Call this to free all memory used by cache..
        /// </summary>
        public void Release()
        {
            //Interlocked.Exchange(ref _firstItem, null);

            IntPtr inst = Interlocked.Exchange(ref _firstItem.buf, IntPtr.Zero);
            //intentional not setting buffer size, as IntPtr.Zero buffer
            //can be used by free, setting buffer size will protentially mass up buffer just freed
            //meanwhile IntPtr.Zero buffer cannot be allocated so it is not to set size to zero
            if (inst != IntPtr.Zero) FreeHGlobal(inst);

            int tempCap = Interlocked.Exchange(ref _items_capacity, 0);
            for (int i = 0; i < tempCap; i++)
            {
                //set size to zero is okay here as _items_capacity is
                //set to zero while clearing
                if (_items[i].buf != IntPtr.Zero) FreeHGlobal(_items[i].buf);
                _items[i].buf = IntPtr.Zero;
                _items[i].bSize = 0;
            }
            _items_capacity = tempCap;

            //Interlocked.Exchange(ref _items[i].Value, null);

        }
        #endregion Remove/Clear (Large buffer )
        //----------------------------------------------------------------------------------
        #region Pool interface implementations 

        #endregion Pool interface implementations 
    }
    //----------------------------------------------------------------------------------

    public class BufferCache : IBufferCache
    {
        //----------------------------------------------------------------------------------
        #region PreCalculated cache size params 

        private readonly ushort _minFixSizeLog2;
        private readonly ushort _maxFixSizeLog2;

        private readonly ushort _fixedSizeLevelCount;
        private readonly ushort _maxLevel;

        #endregion PreCalculated cache size params
        //----------------------------------------------------------------------------------
        #region buffer cache containers 

        private readonly FixedSizeBufferCache[] _FixedSizePools;
        private readonly DynamicSizeBufferCache _largeBuffers;

        #endregion buffer caches containers
        //----------------------------------------------------------------------------------
        #region Some Pool param accessors 

        public ushort MinFixedBufferSizeLog2 => _minFixSizeLog2;
        public ushort MaxFixedBufferSizeLog2 => _maxFixSizeLog2;

        public bool HasFixedSizePool => _maxFixSizeLog2 > 0;
        //public const ushort MaxCapacityPower = ushort.MaxValue;

        public int MinFixedBufferSize => _minFixSizeLog2 == 0 ? 0 : 1 << _minFixSizeLog2;
        public int MaxFixedBufferSize => _maxFixSizeLog2 == 0 ? 0 : 1 << _maxFixSizeLog2;
        public const int MaxBufferSize = int.MaxValue;

        internal int[] _fixedBufferBSize;

        #endregion Some cache param accessors
        //----------------------------------------------------------------------------------
        #region buffer Size/Level/Log2(Size) converters 

        public static int GetSizeByLog2(ushort log2) => 1 << log2;

        public static ushort GetLog2BySize(int Size) => unchecked((ushort)unchecked((uint)Size.ClampToPositive()).CeilingLog2());

        public ushort GetLog2ByLevel(ushort level) => unchecked((ushort)(level.MaxAt(_maxLevel) + _minFixSizeLog2));

        public ushort GetLevelByLog2(ushort log2) => unchecked((ushort)(log2 - _minFixSizeLog2).Clamp(0, _maxLevel));

        public ushort GetLevelBySize(int Size) => GetLevelByLog2(GetLog2BySize(Size));

        public int GetSizeByLevel(ushort level)
        {
            ushort pow = GetLog2ByLevel(level);
            if (pow > _maxFixSizeLog2) return int.MaxValue;
            else return GetSizeByLog2(pow);
        }

        #endregion buffer Size/Level/Log2(Size) converters 
        //----------------------------------------------------------------------------------
        #region Ctor 

        public BufferCache() : this(DefaultCacheCapacity, DefualBufferSizeLog2Max, DefualBufferSizeLog2Min) { }
        public BufferCache(int capacity) : this(capacity, DefualBufferSizeLog2Max, DefualBufferSizeLog2Min) { }

        /// <summary>
        /// create new buffer cache
        /// fixed size buffer from inner FixedSizeBufferPool of several levels
        /// large dynamic size buffer from inner DynamicSizeBufferPool
        /// </summary>
        /// <param name="capacity">capacity of cache per level</param>
        /// <param name="maxBufferSize">max buffer size guess </param>
        public BufferCache(int capacity, int maxBufferSize)
            : this(capacity,
                    unchecked((ushort)(GetLog2BySize(maxBufferSize)).MinAt(BufferSizeLog2Min)),
                    unchecked((ushort)(GetLog2BySize(maxBufferSize) - 2).MinAt(BufferSizeLog2Min)))
        { }

        /// <summary>
        /// create new buffer cache
        /// fixed size buffer from inner FixedSizeBufferPool of several levels
        /// large dynamic size buffer from inner DynamicSizeBufferPool 
        /// </summary>
        /// /// <param name="capacity">capacity of cache per level</param>
        /// <param name="maxFixbufferSizeLog2">fixed sized buffer min Log2(size)</param>
        /// <param name="minFixbufferSizeLog2">fixed sized buffer max Log2(size)</param>
        /// <param name="hadDynamicSize">should inner DynamicSizeBufferPool be created</param>
        public BufferCache(int capacity, ushort maxFixbufferSizeLog2, ushort minFixbufferSizeLog2, bool hadDynamicSize = true)
        {
            capacity = capacity.MinAt(1);

            _minFixSizeLog2 = minFixbufferSizeLog2.MinAt(BufferSizeLog2Min);
            _maxFixSizeLog2 = maxFixbufferSizeLog2.MinAt(_minFixSizeLog2);

            _fixedSizeLevelCount = unchecked((ushort)(_maxFixSizeLog2 - _minFixSizeLog2 + 1));
            _maxLevel = _fixedSizeLevelCount;

            _fixedBufferBSize = new int[_fixedSizeLevelCount];
            _FixedSizePools = new FixedSizeBufferCache[_fixedSizeLevelCount];
            for (ushort idx = 0, log2 = _minFixSizeLog2; idx < _fixedSizeLevelCount; log2++, idx++)
            {
                var size = 1 << log2;
                _FixedSizePools[idx] = new FixedSizeBufferCache(capacity, size);
                _fixedBufferBSize[idx] = size;
            }
            if (hadDynamicSize) _largeBuffers = new DynamicSizeBufferCache(capacity);
            else _largeBuffers = null;
        }

        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region Interface Allocate/Free (bufferSeg) 

        public BufferX<T> Allocate<T>(int aSize) where T : unmanaged
        {
            var buf = AllocateRaw(aSize * BufferX<T>._elemLen);
            return new BufferX<T>(buf.buf, aSize, buf.bSize);

        }

        public void Free<T>(BufferX<T> buf) where T : unmanaged
        {
            if (buf._ptrBuf == IntPtr.Zero) return;

            int level = Array.BinarySearch(_fixedBufferBSize, buf._allocByteSize);
            if (level < 0)
            {
                if (_largeBuffers != null) _largeBuffers.Free<T>(buf);
                else FreeHGlobal(buf._ptrBuf);
            }
            else _FixedSizePools[level].Free<T>(buf);
        }

        public void Free<T>(ref BufferX<T> buf) where T : unmanaged
        {
            Free<T>(buf);
            buf = BufferX<T>.Null;
        }

        public void FreeRaw(RawBuffer buf)
        {
            if (buf.buf == IntPtr.Zero) return;

            int level = Array.BinarySearch(_fixedBufferBSize, buf.bSize);
            if (level < 0)
            {
                if (_largeBuffers != null) _largeBuffers.FreeRaw(buf);
                else FreeHGlobal(buf.buf);
            }
            else _FixedSizePools[level].FreeRaw(buf);
        }

        public void FreeRaw(ref RawBuffer buf)
        {
            FreeRaw(buf);
            buf = RawBuffer.Null;
        }

        #endregion Interface
        //----------------------------------------------------------------------------------
        #region Internal Allocate (buffer) 

        public RawBuffer AllocateRaw(int byteSize)
        {
            if (byteSize < 0) throw new ArgumentOutOfRangeException();
            int matchLevel = Array.BinarySearch(_fixedBufferBSize, byteSize);
            if (matchLevel < 0) matchLevel = ~matchLevel;

            RawBuffer buf = RawBuffer.Null;
            if (matchLevel >= _fixedSizeLevelCount)
            {
                if (_largeBuffers == null)
                {
#if DEBUG
                    DebugX.Log($"BufferPool large buffer({byteSize})");
#endif

                    buf.buf = AllocHGlobal(byteSize);
                    buf.bSize = byteSize;
                    return buf;
                }
                return _largeBuffers.AllocateRaw(byteSize);
            }

            int searchUintil = Min(_fixedSizeLevelCount, matchLevel + BufferSizeLevelSearchCount);
            for (int l = matchLevel; l < searchUintil; l++)
            {
                var fsp = _FixedSizePools[l];
                buf.buf = fsp.TryAlloc();

                if (buf.buf != IntPtr.Zero)
                {
                    buf.bSize = fsp._byteSize;
                    return buf;
                }
            }
            if (_largeBuffers != null) buf = _largeBuffers.TryAllocate(byteSize);
            if (buf.buf != IntPtr.Zero) return buf;
            else
            {
                int fSize = _fixedBufferBSize[matchLevel];
#if DEBUG
                DebugX.Log($"BufferPool new buffer({fSize})");
#endif

                buf.buf = AllocHGlobal(fSize);
                buf.bSize = fSize;
                return buf;
            }
        }

        #endregion Internal Allocate (buffer) 
        //----------------------------------------------------------------------------------
        public void RemoveLargest() => _largeBuffers?.RemoveLargest();
        public void Release()
        {
            _largeBuffers?.Release();
            for (int i = 0; i < _fixedSizeLevelCount; i++) _FixedSizePools[i].Release();
        }
        //----------------------------------------------------------------------------------
    }
}