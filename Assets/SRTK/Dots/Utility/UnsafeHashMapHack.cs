/************************************************************************************
| File: UnsafeHashMapHack.cs                                                        |
| Project: lieene.Utility                                                           |
| Created Date: Tue Apr 7 2020                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Sat Apr 11 2020                                                    |
| Modified By: Lieene Guo                                                           |
| -----                                                                             |
| MIT License                                                                       |
|                                                                                   |
| Copyright (c) 2020 Lieene@ShadeRealm                                              |
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace SRTK.Hack
{
    //-------------------------------------------------------------------------------------------------------------------
    #region SRTK Hack
    public struct UnsafeHashMapEntryIterator
    {
        internal int maskedHash;//Hash Bucket Iterator
        internal int entryIndex;//Entry & Next iterator
    }
    #endregion SRTK Hack
    //-------------------------------------------------------------------------------------------------------------------

    public struct NativeMultiHashMapIterator<TKey>
        where TKey : struct
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe partial struct UnsafeHashMapData
    {
        [FieldOffset(0)]
        internal byte* values;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        internal byte* keys;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(16)]
        internal byte* next;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(24)]
        internal byte* buckets;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(32)]
        internal int keyCapacity;

        [FieldOffset(36)]
        internal int bucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int allocatedIndexLength;

        [FieldOffset(JobsUtility.CacheLineSize < 64 ? 64 : JobsUtility.CacheLineSize)]
        internal fixed int firstFreeTLS[JobsUtility.MaxJobThreadCount * IntPerCacheLine];

        // 64 is the cache line size on x86, arm usually has 32 - so it is possible to save some memory there
        internal const int IntPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void IsBlittableAndThrow<TKey, TValue>()
            where TKey : struct
            where TValue : struct
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();
        }

        internal static void AllocateHashMap<TKey, TValue>(int length, int bucketLength, Allocator label, out UnsafeHashMapData* outBuf)
            where TKey : struct
            where TValue : struct
        {
            IsBlittableAndThrow<TKey, TValue>();

            UnsafeHashMapData* data = (UnsafeHashMapData*)UnsafeUtility.Malloc(sizeof(UnsafeHashMapData), UnsafeUtility.AlignOf<UnsafeHashMapData>(), label);

            bucketLength = math.ceilpow2(bucketLength);

            data->keyCapacity = length;
            data->bucketCapacityMask = bucketLength - 1;

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out keyOffset, out nextOffset, out bucketOffset);

            data->values = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            data->keys = data->values + keyOffset;
            data->next = data->values + nextOffset;
            data->buckets = data->values + bucketOffset;

            outBuf = data;
        }

        internal static void ReallocateHashMap<TKey, TValue>(UnsafeHashMapData* data, int newCapacity, int newBucketCapacity, Allocator label)
            where TKey : struct
            where TValue : struct
        {
            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            if (data->keyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a hash map is not supported");
            }

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out keyOffset, out nextOffset, out bucketOffset);

            byte* newData = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            byte* newKeys = newData + keyOffset;
            byte* newNext = newData + nextOffset;
            byte* newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->values, data->keyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->keys, data->keyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, data->next, data->keyCapacity * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            for (int bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
            {
                int* buckets = (int*)data->buckets;
                int* pNextIndexChain = (int*)newNext;
                while (buckets[bucket] >= 0)
                {
                    int curEntry = buckets[bucket];
                    buckets[bucket] = pNextIndexChain[curEntry];
                    int newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() & (newBucketCapacity - 1);
                    pNextIndexChain[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            UnsafeUtility.Free(data->values, label);
            if (data->allocatedIndexLength > data->keyCapacity)
            {
                data->allocatedIndexLength = data->keyCapacity;
            }

            data->values = newData;
            data->keys = newKeys;
            data->next = newNext;
            data->buckets = newBuckets;
            data->keyCapacity = newCapacity;
            data->bucketCapacityMask = newBucketCapacity - 1;
        }

        internal static void DeallocateHashMap(UnsafeHashMapData* data, Allocator allocator)
        {
            UnsafeUtility.Free(data->values, allocator);
            UnsafeUtility.Free(data, allocator);
        }

        internal static int CalculateDataSize<TKey, TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TKey : struct
            where TValue : struct
        {
            int elementSize = UnsafeUtility.SizeOf<TValue>();
            int keySize = UnsafeUtility.SizeOf<TKey>();

            // Offset is rounded up to be an even cacheLineSize
            keyOffset = (elementSize * length + JobsUtility.CacheLineSize - 1);
            keyOffset -= keyOffset % JobsUtility.CacheLineSize;

            nextOffset = (keyOffset + keySize * length + JobsUtility.CacheLineSize - 1);
            nextOffset -= nextOffset % JobsUtility.CacheLineSize;

            bucketOffset = (nextOffset + UnsafeUtility.SizeOf<int>() * length + JobsUtility.CacheLineSize - 1);
            bucketOffset -= bucketOffset % JobsUtility.CacheLineSize;

            int totalSize = bucketOffset + UnsafeUtility.SizeOf<int>() * bucketLength;
            return totalSize;
        }

        internal static void GetKeyArray<TKey>(UnsafeHashMapData* data, NativeArray<TKey> result)
            where TKey : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result[o++] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, b);
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Length, o);
        }

        internal static void GetValueArray<TValue>(UnsafeHashMapData* data, NativeArray<TValue> result)
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result[o++] = UnsafeUtility.ReadArrayElement<TValue>(data->values, b);
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Length, o);
        }

        internal static void GetKeyValueArrays<TKey, TValue>(UnsafeHashMapData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result.Keys[o] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, b);
                    result.Values[o] = UnsafeUtility.ReadArrayElement<TValue>(data->values, b);
                    o++;
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Keys.Length, o);
            Assert.AreEqual(result.Values.Length, o);
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    #region SRTK Hack
    internal unsafe partial struct UnsafeHashMapData
    {
        internal static unsafe TKey KeyAtEntry<TKey>(UnsafeHashMapEntryIterator itr, UnsafeHashMapData* data)
        {
            Assert.IsTrue(itr.maskedHash >= 0 && itr.maskedHash <= data->bucketCapacityMask, "Masked Hash Code out of range!");
            return UnsafeUtility.ReadArrayElement<TKey>(data->keys, itr.entryIndex);
        }

        internal static unsafe TValue ValueAtEntry<TValue>(UnsafeHashMapEntryIterator itr, UnsafeHashMapData* data)
        {
            Assert.IsTrue(itr.maskedHash >= 0 && itr.maskedHash <= data->bucketCapacityMask, "Masked Hash Code out of range!");
            return UnsafeUtility.ReadArrayElement<TValue>(data->values, itr.entryIndex);
        }

        internal static unsafe bool FirstEntryIndex(UnsafeHashMapData* data, out UnsafeHashMapEntryIterator itr)
        {
            itr = new UnsafeHashMapEntryIterator() { maskedHash = 0, entryIndex = -1 };
            var buckets = (int*)data->buckets;
            while (itr.entryIndex < 0 && itr.maskedHash <= data->bucketCapacityMask)
                itr.entryIndex = buckets[itr.maskedHash++];
            return itr.entryIndex >= 0;
        }

        internal static unsafe bool NextEntryIndex(UnsafeHashMapData* data, ref UnsafeHashMapEntryIterator itr)
        {
            var buckets = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            itr.entryIndex = bucketNext[itr.entryIndex];
            while (itr.entryIndex < 0 && itr.maskedHash <= data->bucketCapacityMask)
                itr.entryIndex = buckets[itr.maskedHash++];
            return itr.entryIndex >= 0;
        }
    }
    #endregion SRTK Hack
    //-------------------------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct UnsafeHashMapBase<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal static unsafe void Clear(UnsafeHashMapData* data)
        {
            UnsafeUtility.MemSet(data->buckets, 0xff, (data->bucketCapacityMask + 1) * 4);
            UnsafeUtility.MemSet(data->next, 0xff, (data->keyCapacity) * 4);

            for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
            {
                data->firstFreeTLS[tls * UnsafeHashMapData.IntPerCacheLine] = -1;
            }

            data->allocatedIndexLength = 0;
        }

        internal static unsafe int AllocEntry(UnsafeHashMapData* data, int threadIndex)
        {
            int idx;
            int* pNextIndexChain = (int*)data->next;
            do
            {
                idx = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine];
                if (idx < 0)
                {
                    // Try to refill local cache
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], -2);

                    // If it failed try to get one from the never-allocated array
                    if (data->allocatedIndexLength < data->keyCapacity)
                    {
                        idx = Interlocked.Add(ref data->allocatedIndexLength, 16) - 16;

                        if (idx < data->keyCapacity - 1)
                        {
                            int count = math.min(16, data->keyCapacity - idx);
                            for (int i = 1; i < count; ++i) pNextIndexChain[idx + i] = idx + i + 1;
                            pNextIndexChain[idx + count - 1] = -1;
                            pNextIndexChain[idx] = -1;
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], idx + 1);
                            return idx;
                        }
                        if (idx == data->keyCapacity - 1)
                        {
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], -1);
                            return idx;
                        }
                    }
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], -1);
                    // Failed to get any, try to get one from another free list
                    bool again = true;
                    while (again)
                    {
                        again = false;
                        for (int other = (threadIndex + 1) % JobsUtility.MaxJobThreadCount; other != threadIndex; other = (other + 1) % JobsUtility.MaxJobThreadCount)
                        {
                            do
                            {
                                idx = data->firstFreeTLS[other * UnsafeHashMapData.IntPerCacheLine];
                                if (idx < 0) break;
                            }
                            while (Interlocked.CompareExchange(ref data->firstFreeTLS[other * UnsafeHashMapData.IntPerCacheLine], pNextIndexChain[idx], idx) != idx);

                            if (idx == -2) again = true;
                            else if (idx >= 0)
                            {
                                pNextIndexChain[idx] = -1;
                                return idx;
                            }
                        }
                    }
                    throw new InvalidOperationException("HashMap is full");
                }
                if (idx >= data->keyCapacity) throw new InvalidOperationException(string.Format("nextPtr idx {0} beyond capacity {1}", idx, data->keyCapacity));
            }
            while (Interlocked.CompareExchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], pNextIndexChain[idx], idx) != idx);
            pNextIndexChain[idx] = -1;
            return idx;
        }

        internal static unsafe bool TryAddAtomic(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            if (TryGetFirstValueAtomic(data, key, out _, out _)) return false;

            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                int* pNextIndexChain = (int*)data->next;
                do
                {
                    pNextIndexChain[idx] = buckets[bucket];
                    if (TryGetFirstValueAtomic(data, key, out _, out _))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        do pNextIndexChain[idx] = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine];
                        while (Interlocked.CompareExchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], idx, pNextIndexChain[idx]) != pNextIndexChain[idx]);
                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, pNextIndexChain[idx]) != pNextIndexChain[idx]);
            }

            return true;
        }

        internal static unsafe void AddAtomicMulti(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            int nextPtr;
            int* pNextIndexChain = (int*)data->next;
            do
            {
                nextPtr = buckets[bucket];
                pNextIndexChain[idx] = nextPtr;
            }
            while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtr) != nextPtr);
        }

        internal static unsafe bool TryAdd(UnsafeHashMapData* data, TKey key, TValue item, bool isMultiHashMap, Allocator allocation)
        {
            if (!isMultiHashMap && TryGetFirstValueAtomic(data, key, out _, out _)) return false;

            // Allocate an entry from the free list
            int idx;
            int* pNextIndexChain;

            if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
            {
                for (int tls = 1; tls < JobsUtility.MaxJobThreadCount; ++tls)
                {
                    if (data->firstFreeTLS[tls * UnsafeHashMapData.IntPerCacheLine] >= 0)
                    {
                        idx = data->firstFreeTLS[tls * UnsafeHashMapData.IntPerCacheLine];
                        pNextIndexChain = (int*)data->next;
                        data->firstFreeTLS[tls * UnsafeHashMapData.IntPerCacheLine] = pNextIndexChain[idx];
                        pNextIndexChain[idx] = -1;
                        data->firstFreeTLS[0] = idx;
                        break;
                    }
                }

                if (data->firstFreeTLS[0] < 0)
                {
                    int newCap = UnsafeHashMapData.GrowCapacity(data->keyCapacity);
                    UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeHashMapData.GetBucketSize(newCap), allocation);
                }
            }

            idx = data->firstFreeTLS[0];

            if (idx >= 0)
            {
                data->firstFreeTLS[0] = ((int*)data->next)[idx];
            }
            else
            {
                idx = data->allocatedIndexLength++;
            }

            if (idx < 0 || idx >= data->keyCapacity)
            {
                throw new InvalidOperationException("Internal HashMap error");
            }

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;
            pNextIndexChain = (int*)data->next;

            pNextIndexChain[idx] = buckets[bucket];
            buckets[bucket] = idx;

            return true;
        }

        internal static unsafe int Remove(UnsafeHashMapData* data, TKey key, bool isMultiHashMap)
        {
            var removed = 0;

            // First find the slot based on the hash
            var buckets = (int*)data->buckets;
            var pNextIndexChain = (int*)data->next;
            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            var prevEntry = -1;
            var entryIdx = buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data->keyCapacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        buckets[bucket] = pNextIndexChain[entryIdx];
                    }
                    else
                    {
                        pNextIndexChain[prevEntry] = pNextIndexChain[entryIdx];
                    }

                    // And free the index
                    int nextIdx = pNextIndexChain[entryIdx];
                    pNextIndexChain[entryIdx] = data->firstFreeTLS[0];
                    data->firstFreeTLS[0] = entryIdx;
                    entryIdx = nextIdx;

                    // Can only be one hit in regular hashmaps, so return
                    if (!isMultiHashMap)
                    {
                        break;
                    }
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = pNextIndexChain[entryIdx];
                }
            }

            return removed;
        }

        internal static unsafe void Remove(UnsafeHashMapData* data, NativeMultiHashMapIterator<TKey> it)
        {
            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int* pNextIndexChain = (int*)data->next;
            int bucket = it.key.GetHashCode() & data->bucketCapacityMask;

            int entryIdx = buckets[bucket];

            if (entryIdx == it.EntryIndex)
            {
                buckets[bucket] = pNextIndexChain[entryIdx];
            }
            else
            {
                while (entryIdx >= 0 && pNextIndexChain[entryIdx] != it.EntryIndex)
                {
                    entryIdx = pNextIndexChain[entryIdx];
                }

                if (entryIdx < 0)
                {
                    throw new InvalidOperationException("Invalid iterator passed to HashMap remove");
                }

                pNextIndexChain[entryIdx] = pNextIndexChain[it.EntryIndex];
            }

            // And free the index
            pNextIndexChain[it.EntryIndex] = data->firstFreeTLS[0];
            data->firstFreeTLS[0] = it.EntryIndex;
        }

        internal static unsafe void RemoveKeyValue<TValueEQ>(UnsafeHashMapData* data, TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            var buckets = (int*)data->buckets;
            var keyCapacity = (uint)data->keyCapacity;
            var prevNextPtr = buckets + (key.GetHashCode() & data->bucketCapacityMask);
            var entryIdx = *prevNextPtr;

            if ((uint)entryIdx >= keyCapacity)
            {
                return;
            }

            var pNextIndexChain = (int*)data->next;
            var keys = data->keys;
            var values = data->values;
            var firstFreeTLS = data->firstFreeTLS;

            do
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key)
                && UnsafeUtility.ReadArrayElement<TValueEQ>(values, entryIdx).Equals(value))
                {
                    int nextIdx = pNextIndexChain[entryIdx];
                    pNextIndexChain[entryIdx] = firstFreeTLS[0];
                    firstFreeTLS[0] = entryIdx;
                    *prevNextPtr = entryIdx = nextIdx;
                }
                else
                {
                    prevNextPtr = pNextIndexChain + entryIdx;
                    entryIdx = *prevNextPtr;
                }
            }
            while ((uint)entryIdx < keyCapacity);
        }

        internal static unsafe bool TryGetFirstValueAtomic(UnsafeHashMapData* data, TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            it.key = key;
            if (data->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryGetNextValueAtomic(data, out item, ref it);
        }

        internal static unsafe bool TryGetNextValueAtomic(UnsafeHashMapData* data, out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            item = default;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            int* pNextIndexChain = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(it.key))
            {
                entryIdx = pNextIndexChain[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = pNextIndexChain[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(data->values, entryIdx);

            return true;
        }

        internal static unsafe bool SetValue(UnsafeHashMapData* data, ref NativeMultiHashMapIterator<TKey> it, ref TValue item)
        {
            int entryIdx = it.EntryIndex;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity) return false;
            UnsafeUtility.WriteArrayElement(data->values, entryIdx, item);
            return true;
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeHashMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeHashMapData* Data;
        public Allocator Allocator;

        public void Execute()
        {
            UnsafeHashMapData.DeallocateHashMap(Data, Allocator);
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    #region SRTK Hack 
    internal partial struct UnsafeHashMapBase<TKey, TValue>
    {
        internal static unsafe bool TryGetValueAtomicPtr(UnsafeHashMapData* data, TKey key, out TValue* pItem)
        {
            if (data->allocatedIndexLength <= 0)
            {
                pItem = null;
                return false;
            }
            
            int* buckets = (int*)data->buckets;
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            int entryIdx = buckets[bucket];

            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                pItem = null;
                return false;
            }

            int* pNextIndexChain = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
            {
                entryIdx = pNextIndexChain[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    pItem = null;
                    return false;
                }
            }
            // Read the value
            pItem = (TValue*)(data->values + (UnsafeUtility.SizeOf<TValue>() * entryIdx));
            return true;
        }

        internal static unsafe int AllocateFreeIndexAtomicPtr(UnsafeHashMapData* data, TKey key, out TValue* pItem, int threadIndex)
        {
            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);
            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            pItem = (TValue*)(data->values + (UnsafeUtility.SizeOf<TValue>() * idx));
            return idx;
        }

        internal static unsafe bool TryAddOrGetValueAtomicPtr(UnsafeHashMapData* data, TKey key, int idx, ref TValue* pItem, int threadIndex)
        {
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                int* pNextIndexChain = (int*)data->next;
                do
                {
                    pNextIndexChain[idx] = buckets[bucket];
                    if (TryGetValueAtomicPtr(data, key, out pItem))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        do pNextIndexChain[idx] = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine];
                        while (Interlocked.CompareExchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntPerCacheLine], idx, pNextIndexChain[idx]) != pNextIndexChain[idx]);
                        return true;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, pNextIndexChain[idx]) != pNextIndexChain[idx]);
            }
            return false;
        }
    }
    #endregion SRTK Hack
    //-------------------------------------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------------------------------------
    #region SRTK Hack 
    //TODO: WIP
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct AtomicArrayIndexAccessor
    {
        internal const int FreeIndexNone = -1;
        internal const int FreeIndexAllocating = -2;

        internal const int IntPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);
        internal const int FirstFreeIndexPreThreadSize = JobsUtility.MaxJobThreadCount * IntPerCacheLine;

        [FieldOffset(0)] internal fixed int FirstFreeIndexPreThread[FirstFreeIndexPreThreadSize];
        [FieldOffset(FirstFreeIndexPreThreadSize)] internal int* pNextIndexChain;
        // 4-byte padding on 32-bit architectures here
        [FieldOffset(FirstFreeIndexPreThreadSize + 8)] internal int Capacity;
        [FieldOffset(FirstFreeIndexPreThreadSize + 12)] internal int allocatedIndexLength;

        //write only
        internal static unsafe int AllocEntry(int Capacity, ref int allocatedIndexLength, int* FirstFreeIndexPreThread, int* pNextIndexChain, int threadIndex)
        {
            int idx;
            int thisThreadOffset = threadIndex * IntPerCacheLine;
            do
            {
                //Get first index from free index chain of this thread
                idx = FirstFreeIndexPreThread[thisThreadOffset];
                if (idx <= FreeIndexNone)// Free Index Not found here
                {
                    // If it failed try to get one from the never-allocated array, meanwhile cache up to 16 index in next chain

                    //Mark allocation started
                    Interlocked.Exchange(ref FirstFreeIndexPreThread[thisThreadOffset], FreeIndexAllocating);

                    //Capacity test
                    if (allocatedIndexLength < Capacity)//Has unallocated index
                    {
                        //allocate 16 indexes
                        idx = Interlocked.Add(ref allocatedIndexLength, 16) - 16;

                        if (idx < Capacity - 1)//has more than one unallocated indexes
                        {//store allocated indexes in chain, without the first one, witch is returned

                            int count = math.min(16, Capacity - idx);//Clamp to Capacity
                            for (int i = 1; i < count; ++i) pNextIndexChain[idx + i] = idx + i + 1;//chain extra allocated indexes (not the first as it will be returned)
                            pNextIndexChain[idx + count - 1] = FreeIndexNone;//break chain at last allocated index
                            pNextIndexChain[idx] = FreeIndexNone;//break chain for returned index
                            Interlocked.Exchange(ref FirstFreeIndexPreThread[thisThreadOffset], idx + 1);//put free chain first into FirstFreeIndexPreThread for this thread
                            return idx;
                        }
                        else if (idx == Capacity - 1)// just one index allocated
                        {
                            Interlocked.Exchange(ref FirstFreeIndexPreThread[thisThreadOffset], FreeIndexNone);//no free index
                            return idx;
                        }
                        //else no index allocated index are all allocated, there might be some in other thread cache
                    }

                    //Failed to allocate Index, Mark None free and allocated end.
                    Interlocked.Exchange(ref FirstFreeIndexPreThread[thisThreadOffset], FreeIndexNone);

                    // Try with other thread's Free Chain
                    bool again = true;
                    while (again)
                    {
                        again = false;

                        //for each thread
                        int nextThread = (threadIndex + 1) % JobsUtility.MaxJobThreadCount;
                        while (nextThread != threadIndex)
                        {
                            int nextThreadOffset = nextThread * IntPerCacheLine;
                            do
                            {
                                idx = FirstFreeIndexPreThread[nextThreadOffset];//get first free index
                                if (idx < 0) break;//No luck, go on to next thread
                            }
                            while (Interlocked.CompareExchange(ref FirstFreeIndexPreThread[nextThreadOffset], pNextIndexChain[idx], idx) != idx);
                            //There are Free indexes here try to compete for index, go back if failed

                            if (idx == FreeIndexAllocating) again = true;//allocating there are still free indexes out there, should try again
                            else if (idx >= 0)//index pinned!
                            {
                                pNextIndexChain[idx] = FreeIndexNone;//break chain at returned index
                                return idx;
                            }
                            nextThread = (nextThread + 1) % JobsUtility.MaxJobThreadCount;//step to next thread
                        }
                    }
                    throw new InvalidOperationException("All index is in use");
                }
                if (idx >= Capacity) throw new InvalidOperationException(string.Format("nextChain idx {0} beyond capacity {1}", idx, Capacity));
            }
            while (Interlocked.CompareExchange(ref FirstFreeIndexPreThread[thisThreadOffset], pNextIndexChain[idx], idx) != idx);
            //There are Free indexes here try to compete for index, go back if failed

            //Index found, must be index found as if there no index to use InvalidOperationException would been thrown
            pNextIndexChain[idx] = -1;//break chain at returned index
            return idx;
        }

        //TODO: still have problem---------------------------------------------------------------------------------------
        //write only, entryIdx must be allocated
        private static unsafe void FreeNextIndex(int prevIndex, int* FirstFreeIndexPreThread, int* pNextIndexChain, int threadIndex)
        {
            Assert.IsTrue(prevIndex >= 0, "invalid previous index");
            int thisThreadOffset = threadIndex * IntPerCacheLine;
            int index2Free = pNextIndexChain[prevIndex];
            if (index2Free >= 0)
            {
                int nextIndex = pNextIndexChain[index2Free];
                if (nextIndex > 0)
                {
                    while (Interlocked.CompareExchange(ref pNextIndexChain[index2Free], -1, nextIndex) != nextIndex)
                    { nextIndex = pNextIndexChain[index2Free]; }//someone else is freeing nextnextIndex
                }

                if (Interlocked.CompareExchange(ref pNextIndexChain[prevIndex], nextIndex, index2Free) == index2Free)
                {
                    pNextIndexChain[index2Free] = FirstFreeIndexPreThread[thisThreadOffset];
                    FirstFreeIndexPreThread[thisThreadOffset] = index2Free;
                }
                //else someone else has freed target index
            }
            //else someone else has freed target index
        }
        //TODO: still have problem---------------------------------------------------------------------------------------


    }

    #endregion SRTK Hack
    //-------------------------------------------------------------------------------------------------------------------

}