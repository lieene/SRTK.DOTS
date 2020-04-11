/************************************************************************************
| File: NativeAggregator.cs                                                         |
| Project: lieene.Utility                                                           |
| Created Date: Fri Apr 3 2020                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Fri Apr 10 2020                                                    |
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
| Date      	By	Comments                                                          |
| ----------	---	----------------------------------------------------------        |
************************************************************************************/


using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Assertions;
using SRTK.Hack;

namespace SRTK
{
    //-------------------------------------------------------------------------------------------------------------------
    unsafe public struct NativeMappedAggregator<TKey, TValue, TResult, TAggregator, TData> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TResult : unmanaged
        where TAggregator : unmanaged, IAggregator<TValue, TResult>, IAggregatorShell<TValue, TResult>
        where TData : unmanaged, IAggregatorData<TValue, TResult>
    {
        [NativeDisableUnsafePtrRestriction] internal UnsafeHashMapData* mBuffer;
        internal Allocator mAllocator;
        internal TAggregator mShell;

        internal NativeMappedAggregator(int capacity, Allocator allocator, TAggregator shell = default)
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TData>();
            mAllocator = allocator;
            // Bucket size is bigger to reduce collisions
            UnsafeHashMapData.AllocateHashMap<TKey, TData>(capacity, capacity << 1, allocator, out mBuffer);
            UnsafeHashMapBase<TKey, TData>.Clear(mBuffer);
            mShell = shell;
        }
        //---------------------------------------------------------------------------------------------------------
        #region iterator Access

        public bool TryGetFirst(out UnsafeHashMapEntryIterator itr) => UnsafeHashMapData.FirstEntryIndex(mBuffer, out itr);

        public bool TryGetNext(ref UnsafeHashMapEntryIterator itr) => UnsafeHashMapData.NextEntryIndex(mBuffer, ref itr);

        public bool TryRead(ref UnsafeHashMapEntryIterator itr, out TKey key, out TValue value, out TResult result)
        {
            if (itr.entryIndex != -1)
            {
                key = UnsafeHashMapData.KeyAtEntry<TKey>(itr, mBuffer);
                var data = UnsafeHashMapData.ValueAtEntry<TData>(itr, mBuffer);
                value = data.Value;
                result = data.Result;
                return true;
            }
            else
            {
                key = default;
                value = default;
                result = default;
                return false;
            }
        }

        #endregion iterator Access
        //---------------------------------------------------------------------------------------------------------
        #region To Array

        public NativeArray<TKey> ToKeyArray(Allocator allocator)
        {
            var keys = new NativeArray<TKey>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(mBuffer, keys);
            return keys;
        }

        public NativeArray<TData> ToDataArray(Allocator allocator)
        {
            var result = new NativeArray<TData>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetValueArray(mBuffer, result);
            return result;
        }

        public NativeKeyValueArrays<TKey, TData> ToArray(Allocator allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TData>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyValueArrays(mBuffer, result);
            return result;
        }

        #endregion To Array
        //---------------------------------------------------------------------------------------------------------
        #region Evaluate

        public void Evaluate()
        {
            var hashCap = mBuffer->bucketCapacityMask;
            for (int h = 0; h < hashCap; h++) EvaluateBucketImp(h, mBuffer);
        }

        public JobHandle Evaluate(JobHandle dependsOn)
        {
            var hashCap = new NativeRef<int>(Allocator.TempJob, 0);
            dependsOn = new GetHashCapJob() { HashCap = hashCap, data = mBuffer }.Schedule(dependsOn);
            return new EvaluateJob() { data = mBuffer }.Schedule(hashCap, 8, dependsOn);
        }

        static void EvaluateBucketImp(int maskedHash, UnsafeHashMapData* data)
        {
            Assert.IsTrue(maskedHash >= 0 && maskedHash <= data->bucketCapacityMask, "Masked Hash Code out of range!");
            int entryIndex = data->buckets[maskedHash];
            var pNextIndexChain = data->next;
            while (entryIndex != -1)
            {
                UnsafeUtility.ReadArrayElement<TAggregator>(data->values, entryIndex).Evaluate();
                entryIndex = pNextIndexChain[entryIndex];
            }
        }

        #endregion Evaluate
        //---------------------------------------------------------------------------------------------------------
        #region Count And Capacity

        public int Count() => CountImp(mBuffer);

        public JobHandle Count(JobHandle dependsOn, out NativeRef<int> count, Allocator allocator)
        {
            count = new NativeRef<int>(allocator, 0);
            return new CountJob() { count = count, data = mBuffer }.Schedule(dependsOn);
        }

        internal static int CountImp(UnsafeHashMapData* data)
        {
            int* pNextIndexes = (int*)data->next;
            int freeListSize = 0;
            for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
            {
                int freeIdx = data->firstFreeTLS[tls * UnsafeHashMapData.IntPerCacheLine];
                for (; freeIdx >= 0; freeIdx = pNextIndexes[freeIdx]) ++freeListSize;
            }
            return math.min(data->keyCapacity, data->allocatedIndexLength) - freeListSize;
        }

        public int Capacity
        {
            get => mBuffer->keyCapacity;
            set => UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(mBuffer, value, UnsafeHashMapData.GetBucketSize(value), mAllocator);
        }

        #endregion Count And Capacity
        //---------------------------------------------------------------------------------------------------------
        #region Access by key
        public TResult this[TKey key] => ResultOf(key);

        public TResult ResultOf(TKey key)
        {
            bool found = UnsafeHashMapBase<TKey, TData>.TryGetValueAtomicPtr(mBuffer, key, out var pItem);
            Assert.IsTrue(found, "Key does not exist!");
            return pItem->Result;
        }

        public TValue ValueOf(TKey key)
        {
            bool found = UnsafeHashMapBase<TKey, TData>.TryGetValueAtomicPtr(mBuffer, key, out var pItem);
            Assert.IsTrue(found, "Key does not exist!");
            return pItem->Value;
        }

        public TAggregator AggregatorOf(TKey key)
        {
            var copy = mShell;
            bool found = UnsafeHashMapBase<TKey, TData>.TryGetValueAtomicPtr(mBuffer, key, out var pItem);
            Assert.IsTrue(found, "Key does not exist!");
            copy.LoadData(pItem->pValue, pItem->pResult, pItem->pCount, pItem->pExtra);
            return copy;
        }

        #endregion Access by key
        //---------------------------------------------------------------------------------------------------------
        #region Dispose

        public void Dispose()
        {
            if (mAllocator.ShouldDeallocate()) UnsafeHashMapData.DeallocateHashMap(mBuffer, mAllocator);
            mAllocator = Allocator.Invalid;
            mBuffer = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (mAllocator.ShouldDeallocate()) dependsOn = new UnsafeHashMapDisposeJob { Data = mBuffer, Allocator = mAllocator }.Schedule(dependsOn);
            mAllocator = Allocator.Invalid;
            mBuffer = null;
            return dependsOn;
        }

        #endregion Dispose
        //---------------------------------------------------------------------------------------------------------
        #region  Jobs
        [BurstCompile]
        internal struct CountJob : IJob
        {
            public NativeRef<int> count;
            [NativeDisableUnsafePtrRestriction] public UnsafeHashMapData* data;
            public void Execute() => count.Value = CountImp(data);
        }

        [BurstCompile]
        struct GetHashCapJob : IJob
        {
            public NativeRef<int> HashCap;
            [NativeDisableUnsafePtrRestriction] public UnsafeHashMapData* data;
            public void Execute() => HashCap.Value = data->bucketCapacityMask;
        }

        [BurstCompile]
        internal struct EvaluateJob : IJobParallelForDefer
        {
            [NativeDisableUnsafePtrRestriction] public UnsafeHashMapData* data;
            public void Execute(int maskedHash) => EvaluateBucketImp(maskedHash, data);
        }

        #endregion  Jobs
        //---------------------------------------------------------------------------------------------------------
        #region Parallel Aggregator aka. Parallel Writer
        public ParallelAggregator AsParallelAggregator => new ParallelAggregator()
        {
            mBuffer = mBuffer,
            ThreadID = -1,
            mAllocator = mAllocator,
            mShell = mShell,
        };

        public struct ParallelAggregator
        {
            [NativeDisableUnsafePtrRestriction] internal UnsafeHashMapData* mBuffer;
            [NativeSetThreadIndex] internal int ThreadID;
            internal Allocator mAllocator;
            internal TAggregator mShell;

            public TValue Aggregate(TKey key, TValue next, out TValue replaced)
            {
                TData* pData;
                var copy = mShell;
                if (UnsafeHashMapBase<TKey, TData>.TryGetValueAtomicPtr(mBuffer, key, out pData))
                {//key found use existing data
                    copy.LoadData(pData->pValue, pData->pResult, pData->pCount, pData->pExtra);
                }
                else//not found
                {
                    //allocate new entry
                    int index = UnsafeHashMapBase<TKey, TData>.AllocateFreeIndexAtomicPtr(mBuffer, key, out pData, ThreadID);
                    copy.LoadData(pData->pValue, pData->pResult, pData->pCount, pData->pExtra);
                    copy.Reset();//Reset data

                    //try add allocated entry to chain
                    if (UnsafeHashMapBase<TKey, TData>.TryAddOrGetValueAtomicPtr(mBuffer, key, index, ref pData, ThreadID))
                    {
                        //some other thread added item first
                        copy.LoadData(pData->pValue, pData->pResult, pData->pCount, pData->pExtra);
                    }
                    //else new key is valid
                }
                return copy.Aggregate(next, out replaced);
            }
        }
        #endregion Parallel Aggregator aka. Parallel Writer
        //---------------------------------------------------------------------------------------------------------
    }
    //-------------------------------------------------------------------------------------------------------------------
    #region Interfaces & Delegates

    /// <summary>
    /// agg each new value to one value, for example, Sum: aggregated = aggregated + next
    /// </summary>
    /// <param name="agg">aggregated value</param>
    /// <param name="next">next value to aggregated</param>
    /// <param name="replaced">aggregated value right before this aggregate action take place
    /// which could be different from ref agg value before this function call, in multithreaded case </param>
    /// <returns>aggregated value right after this aggregate action take place
    /// which could be different from ref agg value after this function returns, in multithreaded case</returns>
    public delegate TValue AggregateFunc<TValue>(ref TValue agg, TValue next, out TValue replaced);

    /// <summary>
    /// Calculate aggregation result base on aggregated value,for example : Average. aggregated/count
    /// </summary>
    /// <param name="agg">aggregated value</param>
    /// <param name="count">count of values aggregated</param>
    /// <returns>calculated result</returns>
    public delegate TResult EvaluateFunc<TValue, TResult>(TValue agg, int count);

    /// <summary>
    /// Reset aggregation value to initial value
    /// </summary>
    /// <param name="agg">aggregated value</param>
    /// <returns>initial value</returns>
    public delegate TValue ResetFunc<TValue>(ref TValue agg);

    public interface IAggregateFunc<TValue>
    {
        /// <summary>
        /// Add new value to current aggregation. for example, Sum: aggregated = aggregated + next
        /// </summary>
        /// <param name="agg">aggregated value</param>
        /// <param name="next">next value to aggregated</param>
        /// <param name="replaced">aggregated value right before this aggregate action take place
        /// which could be different from ref agg value before this function call, in multithreaded case </param>
        /// <returns>aggregated value right after this aggregate action take place
        /// which could be different from ref agg value after this function returns, in multithreaded case</returns>
        TValue Aggregate(ref TValue agg, TValue next, out TValue replaced);
    }

    public interface IEvaluateFunc<TValue> : IEvaluateFunc<TValue, TValue> { };

    public interface IEvaluateFunc<TValue, TResult>
    {
        /// <summary>
        /// Calculate aggregation result base on aggregated value. for example, Average: result = aggregated/count
        /// </summary>
        /// <param name="agg">aggregated value</param>
        /// <param name="count">count of values aggregated</param>
        /// <returns>calculated result</returns>
        TResult Evaluate(TValue agg, int count);
    }

    public interface IResetFunc<TValue>
    {
        /// <summary>
        /// Reset aggregation value to initial value
        /// </summary>
        /// <param name="agg">aggregated value</param>
        /// <returns>initial value</returns>
        TValue Reset(ref TValue agg);
    }


    /// <summary>
    /// aggregate functions
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public interface IAggregation<TValue, TResult> : IAggregateFunc<TValue>, IEvaluateFunc<TValue, TResult>, IResetFunc<TValue> { }

    public interface IAggregation<TValue> : IAggregation<TValue, TValue> { }

    public interface IAggregatorData<TValue> : IAggregatorData<TValue, TValue> where TValue : unmanaged { };

    public interface IAggregatorData<TValue, TResult>
        where TValue : unmanaged
        where TResult : unmanaged
    {
        TValue Value { get; }
        TResult Result { get; }
        unsafe TValue* pValue { get; }
        unsafe TResult* pResult { get; }
        unsafe int* pCount { get; }
        unsafe void* pExtra { get; }
    }

    public interface IAggregatorShell<TValue> : IAggregatorShell<TValue, TValue> where TValue : unmanaged { };

    public interface IAggregatorShell<TValue, TResult> : IDisposable
        where TValue : unmanaged
        where TResult : unmanaged
    {
        unsafe void LoadData(TValue* pValue, TResult* pResult, int* pCount, void* pExtra);
        unsafe bool IsShell { get; }
        bool HasData { get; }
    }

    public interface IAggregator<TValue> : IDisposable, IAggregator<TValue, TValue> where TValue : unmanaged { };

    public interface IAggregator<TValue, TResult>
        where TValue : unmanaged
        where TResult : unmanaged
    {
        /// <summary>
        /// IsCreated
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Count of current aggregated values
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Current aggregated value
        /// </summary>
        TValue Value { get; }

        /// <summary>
        /// aggregation result, if evaluated or else default value of TResult
        /// </summary>
        TResult Result { get; }

        /// <summary>
        /// Add new value to current aggregation. for example, Sum: aggregated = aggregated + next
        /// </summary>
        /// <param name="next">next value to aggregated</param>
        /// <param name="replaced">aggregated value right before this aggregate action take place
        /// which could be different from ref agg value before this function call, in multithreaded case </param>
        /// <param name="aggregation">custom Aggregation to use</param>
        /// <typeparam name="T">type of the custom Aggregation</typeparam>
        /// <returns>aggregated value right after this aggregate action take place
        /// which could be different from ref agg value after this function returns, in multithreaded case</returns>
        TValue Aggregate<T>(TValue next, out TValue replaced, T aggregation) where T : struct, IAggregateFunc<TValue>;

        /// <summary>
        /// Calculate aggregation result base on aggregated value,for example, Average: result = aggregated/count
        /// </summary>
        /// <param name="aggregation">custom Aggregation to use</param>
        /// <typeparam name="T">type of a custom Aggregation</typeparam>
        /// <returns>calculated result</returns>
        TResult Evaluate<T>(T aggregation) where T : struct, IEvaluateFunc<TValue, TResult>;

        /// <summary>
        /// Clear aggregation records, set value back to default value of TValue
        /// </summary>
        void Reset<T>(T aggregation) where T : struct, IResetFunc<TValue>;

        /// <summary>
        /// Add new value to current aggregation. for example, Sum: aggregated = aggregated + next
        /// </summary>
        /// <param name="next">next value to aggregated</param>
        /// <param name="replaced">aggregated value right before this aggregate action take place
        /// which could be different from ref agg value before this function call, in multithreaded case </param>
        /// <returns>aggregated value right after this aggregate action take place
        /// which could be different from ref agg value after this function returns, in multithreaded case</returns>
        TValue Aggregate(TValue next, out TValue replaced);

        /// <summary>
        /// Calculate aggregation result base on aggregated value. for example, Average: result = aggregated/count
        /// </summary>
        /// <returns>calculated result</returns>
        TResult Evaluate();

        /// <summary>
        /// Clear aggregation records, set value back to default value of TValue
        /// </summary>
        void Reset();

        /// <summary>
        /// Deferreed Dispose
        /// </summary>
        JobHandle Dispose(JobHandle dependsOn);
    }
    #endregion Interfaces & Delegates
    //-------------------------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeAggregatorData<TValue, TResult> : IAggregatorData<TValue, TResult>
        where TValue : unmanaged
        where TResult : unmanaged
    {
        public int mCount;
        public Semaphore mSemaphore;
        public TValue mValue;
        public TResult mResult;

        public TValue Value => mValue;
        public TResult Result => mResult;

        unsafe public int* pCount { get { fixed (int* pCount = &mCount) return pCount; } }
        unsafe public void* pExtra { get { fixed (void* pSemaphore = &mSemaphore) return pSemaphore; } }
        unsafe public TValue* pValue { get { fixed (TValue* pValue = &mValue) return pValue; } }
        unsafe public TResult* pResult { get { fixed (TResult* pResult = &mResult) return pResult; } }

        unsafe public static void* GetDataPointerFromCountPointer(int* pCount) => (void*)pCount;
    }

    //-------------------------------------------------------------------------------------------------------------------
    unsafe public struct NativeAggregator<TValue, TResult> : IDisposable, IAggregator<TValue, TResult>, IAggregatorShell<TValue, TResult>
        where TValue : unmanaged
        where TResult : unmanaged
    {
        internal Allocator mAllocator;
        public NativeDelegate<AggregateFunc<TValue>> DefaultAgg;
        public NativeDelegate<EvaluateFunc<TValue, TResult>> DefaultEvaluate;
        public NativeDelegate<ResetFunc<TValue>> DefaultRest;

        [NativeDisableUnsafePtrRestriction] internal void* mDataPtr;
        internal ref NativeAggregatorData<TValue, TResult> mData => ref Unsafe.AsRef<NativeAggregatorData<TValue, TResult>>(mDataPtr);
        public unsafe void LoadData(TValue* pValue, TResult* pResult, int* pCount, void* pExtra) => this.mDataPtr = NativeAggregatorData<TValue, TResult>.GetDataPointerFromCountPointer(pCount);
        public bool IsShell => mAllocator == Allocator.None;
        public bool HasData => mDataPtr != null;

        public NativeAggregator(Allocator allocator, TValue initialVal) : this(allocator)
        {
            mAllocator = allocator;
            mDataPtr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<NativeAggregatorData<TValue, TResult>>(), UnsafeUtility.AlignOf<NativeAggregatorData<TValue, TResult>>(), mAllocator);
            mData.mValue = initialVal;
        }

        public NativeAggregator(Allocator allocator)
        {
            mAllocator = allocator;
            mDataPtr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<NativeAggregatorData<TValue, TResult>>(), UnsafeUtility.AlignOf<NativeAggregatorData<TValue, TResult>>(), mAllocator);
            DefaultAgg = default;
            DefaultEvaluate = default;
            DefaultRest = default;
            mData.mCount = 0;
            mData.mValue = default;
            mData.mSemaphore = Semaphore.Default;
        }

        public void SetAggregationMethod<T>(T aggregation) where T : IAggregation<TValue, TResult>
        {
            DefaultAgg = NativeDelegate<AggregateFunc<TValue>>.Compile(aggregation.Aggregate);
            DefaultEvaluate = NativeDelegate<EvaluateFunc<TValue, TResult>>.Compile(aggregation.Evaluate);
            DefaultRest = NativeDelegate<ResetFunc<TValue>>.Compile(aggregation.Reset);
        }

        public bool IsCreated => mDataPtr != null;
        public int Count => mData.mCount;
        public TValue Value => mData.mValue;
        public TResult Result => mData.mResult;

        public TValue Aggregate<T>(TValue next, out TValue replaced, T func) where T : struct, IAggregateFunc<TValue>
        {
            Assert.IsTrue(IsCreated);
            TValue val;
            var handel = mData.mSemaphore.Lock();
            mData.mCount++;
            val = func.Aggregate(ref mData.mValue, next, out replaced);
            handel.Dispose();
            return val;
        }

        public TResult Evaluate<T>(T aggregation) where T : struct, IEvaluateFunc<TValue, TResult>
        {
            Assert.IsTrue(IsCreated);
            return mData.mResult = aggregation.Evaluate(mData.mValue, mData.mCount);
        }

        public void Reset<T>(T aggregation) where T : struct, IResetFunc<TValue>
        {
            if (mDataPtr == null) return;
            var handel = mData.mSemaphore.Reset();
            mData.mValue = default;
            mData.mResult = default;
            mData.mCount = 0;
            handel.Dispose();
        }
        
        public TValue Aggregate(TValue next, out TValue replaced)
        {
            Assert.IsTrue(DefaultAgg.IsCreated && DefaultEvaluate.IsCreated && DefaultRest.IsCreated, "Aggregation Not Set");
            TValue val;
            var handle = mData.mSemaphore.Lock();
            mData.mCount++;
            val = DefaultAgg.AsFunctionPointer().Invoke(ref mData.mValue, next, out replaced);
            handle.Dispose();
            return val;
        }

        public TResult Evaluate()
        {
            Assert.IsTrue(DefaultAgg.IsCreated && DefaultEvaluate.IsCreated && DefaultRest.IsCreated, "Aggregation Not Set");
            return mData.mResult = DefaultEvaluate.AsFunctionPointer().Invoke(mData.mValue, mData.mCount);
        }

        public void Reset()
        {
            if (mDataPtr == null) return;
            Assert.IsTrue(DefaultAgg.IsCreated && DefaultEvaluate.IsCreated && DefaultRest.IsCreated, "Aggregation Not Set");
            var handle = mData.mSemaphore.Reset();
            mData.mResult = default;
            mData.mCount = 0;
            DefaultRest.AsFunctionPointer().Invoke(ref mData.mValue);
            handle.Dispose();
        }

        public void Dispose()
        {
            if (mAllocator.ShouldDeallocate())
            {
                UnsafeUtility.Free(mDataPtr, mAllocator);
                mAllocator = Allocator.Invalid;
            }
            mDataPtr = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (mAllocator.ShouldDeallocate())
            {
                dependsOn = new UnsafeDisposeInJob { Ptr = mDataPtr, Allocator = mAllocator }.Schedule(dependsOn);
                mAllocator = Allocator.Invalid;
            }
            mDataPtr = null;
            return dependsOn;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------
}