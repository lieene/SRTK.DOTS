/************************************************************************************
| File: ArrayCache.cs                                                               |
| Project: SRTK.Pool                                                                |
| Created Date: Sun Sep 8 2019                                                      |
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
using System.Threading;

namespace SRTK.Pool
{
    using static MathX;
    using static CacheEx;

    //----------------------------------------------------------------------------------
    public class FixedSizeArrayCache<T> : Cache<T[]>, IArrayCache<T>
    {
        private readonly int _arraySize;

        public FixedSizeArrayCache(int poolCapacity, int arraySize) :
            base(() => new T[arraySize], poolCapacity)
        {
            if (arraySize < 1) throw new ArgumentOutOfRangeException();
            _arraySize = arraySize;
        }

        public ArraySeg<T> Allocate(int size)
        {
            if (size > _arraySize) throw new ArgumentOutOfRangeException();
            return Allocate().NewSegBefore(size);
        }

        public void Free(ArraySeg<T> seg) => Free(seg.Inner);
        public void Free(ref ArraySeg<T> seg)
        {
            Free(seg.Inner);
            seg = default(ArraySeg<T>);
        }
    };

    public class DynamicSizeArrayCache<T> : IArrayCache<T>
    {
        //----------------------------------------------------------------------------------
        #region Fields/Properties 

        internal struct Element { public T[] Value; }
        private readonly Element[] _items;
        private int _items_capacity;
        private int _poolCapacity;
        private T[] _firstItem;
        public int PoolCapacity => _poolCapacity;

        #endregion Fields/Properties 
        //----------------------------------------------------------------------------------
        #region Ctor 

        public DynamicSizeArrayCache(int capacity)
        {
            _poolCapacity = capacity.MinAt(1);
            _items_capacity = _poolCapacity - 1;
            _firstItem = null;
            if (_items_capacity > 0) _items = new Element[_items_capacity];
            else _items = null;
        }

        #endregion Ctor 
        //----------------------------------------------------------------------------------
        #region Allocate

        public ArraySeg<T> Allocate(int size) => AllocateRaw(size).NewSegBefore(size);
#if DEBUG
        internal T[] AllocateRaw(int size)
        {
            var item = TryAllocate(size);
            if (item == null)
            {
                DebugX.Log($"DynamicSizeArrayPool new Array({size})");
                return new T[size];
            }
            else return item;
        }
#else
        internal T[] AllocateRaw(int size) => TryAllocate(size) ?? new T[size];
#endif

        #endregion Allocate
        //----------------------------------------------------------------------------------
        #region Free  

        public void Free(ArraySeg<T> seg) => FreeRaw(seg.Inner);
        public void Free(ref ArraySeg<T> seg)
        {
            FreeRaw(seg.Inner);
            seg = default(ArraySeg<T>);
        }

        internal void FreeRaw(T[] item)
        {
            if (item == null) return;
            if (_firstItem == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem = item;
            }
            else if (_items != null)
            {
                // searchIndex is used to track max freeing slot index
                for (int i = 0; i < _items_capacity; i++)
                {
                    if (_items[i].Value == null)
                    {
                        // Intentionally not using interlocked here. 
                        // In a worst case scenario two objects may be stored into same slot.
                        // It is very unlikely to happen and will only mean that one of the objects will get collected.
                        _items[i].Value = item;
                        item = null;
                        break;
                    }
                }
            }
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

        internal T[] TryAllocate(int arraySize)
        {
            T[] item = _firstItem;

            short smallest = _nullID;
            short poolCounter = 0;//number of array(not null) in the pool, used to indicate pool's usage
            int smallSize = short.MaxValue;

            if (item != null)//_firstItem not null
            {
                if (item.Length >= arraySize)// first size match
                {
                    if (item == Interlocked.CompareExchange(ref _firstItem, null, item))
                    {//_firstItem allcated
                        TryClearVote(_firstID);
                        return item;
                    }
                    //else _firstItem taken go on with _items
                    poolCounter++;
                }
                else// too small to allocate
                {
                    smallest = _firstID;
                    smallSize = _firstItem.Length;
                    item = null;
                }
            }

            if (_items != null)// array container not noll
            {//first is null or too small or has been taken from other thread

                for (short i = 0; i < _items_capacity; i++)
                {
                    item = _items[i].Value;
                    if (item != null)
                    {//has candidate here
                        int curArraySize = item.Length;
                        if (curArraySize >= arraySize)
                        {//candidate match size
                            if (item == Interlocked.CompareExchange(ref _items[i].Value, null, item))
                            {//candidate allocated
                                TryClearVote(i);
                                return item;
                            }
                        }
                        else if (curArraySize < smallSize)
                        {//candidate too small to allocate and even smaller than previous smallSize
                            smallest = i;
                            smallSize = curArraySize;
                        }

                        //item not null but cannot use
                        poolCounter++;
                    }
                }
            }
            //no matching array found

            // smallest array set
            if (smallest != _nullID) VoteToRemove(smallest, poolCounter);
            //Else smallest array not set
            //Which means pool is all null
            //So no need to Vote/Remove at all

            return null;
        }
        #endregion Try Allocate
        //----------------------------------------------------------------------------------
        #region Voting/Remove Small array 

        /// <summary>
        /// Clear remove vote when a array is allocated
        /// </summary>
        /// <param name="arrayID"></param>
        internal void TryClearVote(short arrayID)
        {
            if (_removeCandidateID == arrayID)
            {
                _removeCandidateID = _nullID;
                _removeVote = 0;
            }
        }

        /// <summary>
        /// Test if samll array should be removed for larger array to be pooled
        /// if this pool is full of samll arrays large array will keep being GC-collected
        /// keeping large array is okay as large array can be allocated for smaller size require
        /// But it will hold large amount of memory. Call RemoveLagest/Clear to free memory if needed
        /// TryClearVote will remove vote when a array is allocated
        /// So if: pool is fully occupied and array are all too small for current allocation
        ///     one small array will be removed for sure
        /// or if: pool is parcially occupied and array are all too small for current allocation
        ///     one small array will be removed when vote add up to pool's capacity
        ///     In this case if: the samll array is allocated out off the pool
        ///         votes will be cleared and array will not be removed and GC-collected
        ///         the vote will star over from 0 when the small array come back to this pool
        ///         it will be removed if it keep being the smallest one
        /// </summary>
        /// <param name="candidateID">small array candidate</param>
        /// <param name="vote">vote to remove this small array (this value is pool occupancy from try allocate)</param>
        internal void VoteToRemove(short candidateID, short vote)
        {
            if (_removeCandidateID == candidateID)
            {
                _removeVote += vote;
                if (_removeVote >= _poolCapacity)// for one iteration this meanns pool is full of small arrays
                {
                    //remove smallest
                    if (_removeCandidateID == _firstID) Interlocked.Exchange(ref _firstItem, null);
                    else Interlocked.Exchange(ref _items[_removeCandidateID].Value, null);
                }
            }
            else
            {
                _removeCandidateID = candidateID;
                _removeVote = 1;
            }
        }
        #endregion Voting to Remove Small array
        //----------------------------------------------------------------------------------
        #region Remove/Clear (Large array) 

        /// <summary>
        /// Remove the largest array
        /// Small array ocuping the pool will be remove automaticlly when allocating
        /// Large array is safe for allocation But it consumes memory
        /// Call this to free memory..
        /// </summary>
        public void RemoveLargest()
        {
            int largeID = int.MaxValue;
            T[] item = null;
            if (_firstItem != null)
            {
                largeID = _firstID;
                item = _firstItem;
            }

            for (int i = 0; i < _items_capacity; i++)
            {
                T[] cur = _items[i].Value;
                if (cur != null && (item == null || item.Length < cur.Length))
                {
                    largeID = i;
                    item = cur;
                }
            }

            if (largeID == _firstID)
                _firstItem = null;
            if (largeID >= 0 && largeID < _items_capacity)
                _items[largeID].Value = null;
        }

        /// <summary>
        /// Clear all array instance
        /// /// Small array ocuping the pool will be remove automaticlly when allocating
        /// Large array is safe for allocation But it consumes memory
        /// Call this to free all memory used by pool..
        /// </summary>
        public void Clear()
        {
            //Interlocked.Exchange(ref _firstItem, null);
            _firstItem = null;
            int tempCap = Interlocked.Exchange(ref _items_capacity, 0);
            for (int i = 0; i < tempCap; i++) _items[i].Value = null;
            _items_capacity = tempCap;

            //Interlocked.Exchange(ref _items[i].Value, null);

        }
        #endregion Remove/Clear (Large array )
        //----------------------------------------------------------------------------------
        #region Pool interface implementations 

        #endregion Pool interface implementations 
    }
    //----------------------------------------------------------------------------------

    public class ArrayCache<T> : IArrayCache<T>
    {
        //----------------------------------------------------------------------------------
        #region PreCalculated pool size params 

        private readonly ushort _minFixSizeLog2;
        private readonly ushort _maxFixSizeLog2;

        private readonly ushort _fixedSizeLevelCount;
        private readonly ushort _maxLevel;

        #endregion PreCalculated pool size params
        //----------------------------------------------------------------------------------
        #region Array pools containers 

        private readonly FixedSizeArrayCache<T>[] _FixedSizePools;
        private readonly DynamicSizeArrayCache<T> _largeArrays;

        #endregion Array pools containers
        //----------------------------------------------------------------------------------
        #region Some Pool param accessors 

        public ushort MinFixedArraySizeLog2 => _minFixSizeLog2;
        public ushort MaxFixedArraySizeLog2 => _maxFixSizeLog2;

        public bool HasFixedSizePool => _maxFixSizeLog2 > 0;
        //public const ushort MaxCapacityPower = ushort.MaxValue;

        public int MinFixedArraySize => _minFixSizeLog2 == 0 ? 0 : 1 << _minFixSizeLog2;
        public int MaxFixedArraySize => _maxFixSizeLog2 == 0 ? 0 : 1 << _maxFixSizeLog2;
        public const int MaxArraySize = int.MaxValue;

        internal int[] _fixedArrayCaps;

        #endregion Some pool param accessors
        //----------------------------------------------------------------------------------
        #region Array Size/Level/Log2(Size) converters 

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

        #endregion Array Size/Level/Log2(Size) converters 
        //----------------------------------------------------------------------------------
        #region Ctor 

        public ArrayCache() : this(DefaultCacheCapacity, DefualArraySizeLog2Max, DefualArraySizeLog2Min) { }
        public ArrayCache(int capacity) : this(capacity, DefualArraySizeLog2Max, DefualArraySizeLog2Min) { }

        /// <summary>
        /// create new Array pool
        /// fixed size array from inner FixedSizeArrayPool of several levels
        /// large dynamic size array from inner DynamicSizeArrayPool
        /// </summary>
        /// <param name="capacity">capacity of pool per level</param>
        /// <param name="maxArraySize">max array size guess </param>
        public ArrayCache(int capacity, int maxArraySize)
            : this(capacity,
                    unchecked((ushort)(GetLog2BySize(maxArraySize)).MinAt(ArraySizeLog2Min)),
                    unchecked((ushort)(GetLog2BySize(maxArraySize) - 2).MinAt(ArraySizeLog2Min)))
        { }

        /// <summary>
        /// create new Array pool
        /// fixed size array from inner FixedSizeArrayPool of several levels
        /// large dynamic size array from inner DynamicSizeArrayPool 
        /// </summary>
        /// /// <param name="capacity">capacity of pool per level</param>
        /// <param name="maxFixArraySizeLog2">fixed sized array min Log2(size)</param>
        /// <param name="minFixArraySizeLog2">fixed sized array max Log2(size)</param>
        /// <param name="hadDynamicSize">should inner DynamicSizeArrayPool be created</param>
        public ArrayCache(int capacity, ushort maxFixArraySizeLog2, ushort minFixArraySizeLog2, bool hadDynamicSize = true)
        {
            capacity = capacity.MinAt(1);

            _minFixSizeLog2 = minFixArraySizeLog2.MinAt(ArraySizeLog2Min);
            _maxFixSizeLog2 = maxFixArraySizeLog2.MinAt(_minFixSizeLog2);

            _fixedSizeLevelCount = unchecked((ushort)(_maxFixSizeLog2 - _minFixSizeLog2 + 1));
            _maxLevel = _fixedSizeLevelCount;

            _fixedArrayCaps = new int[_fixedSizeLevelCount];
            _FixedSizePools = new FixedSizeArrayCache<T>[_fixedSizeLevelCount];
            for (ushort idx = 0, log2 = _minFixSizeLog2; idx < _fixedSizeLevelCount; log2++, idx++)
            {
                var size = 1 << log2;
                _FixedSizePools[idx] = new FixedSizeArrayCache<T>(capacity, size);
                _fixedArrayCaps[idx] = size;
            }
            if (hadDynamicSize) _largeArrays = new DynamicSizeArrayCache<T>(capacity);
            else _largeArrays = null;
        }

        #endregion Ctor
        //----------------------------------------------------------------------------------
        #region Interface Allocate/Free (ArraySeg) 

        public ArraySeg<T> Allocate(int size) => AllocateRaw(size).NewSegBefore(size);

        public void Free(ArraySeg<T> seg)
        {
            var item = seg.Inner;
            if (item == null) return;

            int level = Array.BinarySearch(_fixedArrayCaps, item.Length);
            if (level < 0) _largeArrays?.FreeRaw(item);
            else _FixedSizePools[level].Free(item);
        }

        public void Free(ref ArraySeg<T> seg)
        {
            Free(seg);
            seg = default(ArraySeg<T>);
        }

        #endregion Interface
        //----------------------------------------------------------------------------------
        #region Internal Allocate (Array) 

        internal T[] AllocateRaw(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException();
            int matchLevel = Array.BinarySearch(_fixedArrayCaps, size);
            if (matchLevel < 0) matchLevel = ~matchLevel;

            if (matchLevel >= _fixedSizeLevelCount)
            {
                if (_largeArrays == null) throw new ArgumentOutOfRangeException();
                return _largeArrays.AllocateRaw(size);
            }

            int searchUintil = Min(_fixedSizeLevelCount, matchLevel + ArraySizeLevelSearchCount);
            T[] item = null;
            for (int l = matchLevel; l < searchUintil; l++)
            {
                item = _FixedSizePools[l].TryAlloc();
                if (item != null) return item;
            }
#if DEBUG
            item = _largeArrays?.TryAllocate(size);
            if (item == null)
            {
                DebugX.Log($"ArrayPool new Array({_fixedArrayCaps[matchLevel]})");
                return new T[_fixedArrayCaps[matchLevel]];
            }
            else return item;
#else
            return _largeArrays?.TryAllocate(size) ?? new T[_fixedArrayCaps[matchLevel]];
#endif
        }

        #endregion Internal Allocate (Array) 
        //----------------------------------------------------------------------------------
        public void RemoveLargest() => _largeArrays?.RemoveLargest();
        public void Clear()
        {
            _largeArrays?.Clear();
            for (int i = 0; i < _fixedSizeLevelCount; i++) _FixedSizePools[i].Clear();
        }
        //----------------------------------------------------------------------------------
    }
}