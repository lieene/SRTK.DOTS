/************************************************************************************
| File: AtomicAggregators.cs                                                        |
| Project: lieene.Utility                                                           |
| Created Date: Thu Apr 9 2020                                                      |
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
| Date      	By	Comments                                                        |
| ----------	---	----------------------------------------------------------      |
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

namespace SRTK
{
    using static AtomicAgg;
    public enum AggregationType
    {
        None = 0,
        Sum = 1,
        Avg = 2,
        Min = 3,
        Max = 4,
    };
    //-------------------------------------------------------------------------------------------------------------------
    #region Int Aggregator
    unsafe public struct AtomicIntAggregator : IDisposable, IAggregator<int, int>, IAggregatorShell<int, int>
    {
        public static AtomicIntAggregator Shell(AggregationType type = AggregationType.Sum)
        {
            Assert.IsTrue(type != AggregationType.None, "Can not assign AggregationType.None");
            return new AtomicIntAggregator()
            {
                mAllocator = Allocator.None,
                mAggType = (byte)(type),
                mDataPtr = null,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void LoadData(int* pValue, int* pResult, int* pCount, void* pExtra) => this.mDataPtr = Data.GetDataPointerFromCountPointer(pCount);

        public bool IsShell => mAllocator == Allocator.None;
        public bool HasData => mDataPtr != null;

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct Data : IAggregatorData<int, int>
        {
            [FieldOffset(0)] public int mCount;
            [FieldOffset(8)] public int mValue;
            [FieldOffset(4)] public int mResult;
            public int Value => mValue;
            public int Result => mResult;

            public int* pCount { get { fixed (int* pCount = &mCount) return pCount; } }
            public int* pValue { get { fixed (int* pValue = &mValue) return pValue; } }
            public int* pResult { get { fixed (int* pResult = &mResult) return pResult; } }
            public void* pExtra => null;
            unsafe public static Data* GetDataPointerFromCountPointer(int* pCount) => (Data*)pCount;
        }

        public AtomicIntAggregator(Allocator allocator, AggregationType type, int initialVal)
        {
            Assert.IsTrue(type != AggregationType.None, "Can not assign AggregationType.None");
            mAllocator = allocator;
            mAggType = (byte)(type);
            mDataPtr = (Data*)UnsafeUtility.Malloc(12, 16, allocator);
            mDataPtr->mValue = initialVal;
            mDataPtr->mCount = 0;
            mDataPtr->mResult = 0;
        }

        public AtomicIntAggregator(Allocator allocator, AggregationType type = AggregationType.Sum)
        {
            mAllocator = allocator;
            mAggType = (byte)(type);
            mDataPtr = (Data*)UnsafeUtility.Malloc(12, 16, allocator);
            Reset();
        }

        public AggregationType AggregationType
        {
            get => (AggregationType)mAggType;
            set => mAggType = (byte)value;
        }

        internal Allocator mAllocator;
        internal byte mAggType;
        [NativeDisableUnsafePtrRestriction] internal Data* mDataPtr;


        public bool IsCreated => mDataPtr != null;

        public int Value => *(mDataPtr->pValue);
        public int Count => *(mDataPtr->pCount);
        public int Result => *(mDataPtr->pResult);

        public void SetValue(int value)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mValue = value;
        }

        public void SetResult(int result)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mResult = result;
        }

        public void SetCount(int count)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mCount = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Aggregate<T>(int next, out int replaced, T aggregation) where T : struct, IAggregateFunc<int>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            return aggregation.Aggregate(ref mDataPtr->mValue, next, out replaced);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate<T>(T aggregation) where T : struct, IEvaluateFunc<int, int>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            return mDataPtr->mResult = aggregation.Evaluate(mDataPtr->mValue, mDataPtr->mCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset<T>(T aggregation) where T : struct, IResetFunc<int>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            aggregation.Reset(ref mDataPtr->mValue);
            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Aggregate(int next, out int replaced)
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            if (mAggType == (byte)AggregationType.Max) return Max(ref mDataPtr->mValue, next, out replaced);
            else if (mAggType == (byte)AggregationType.Min) return Min(ref mDataPtr->mValue, next, out replaced);
            else return Sum(ref mDataPtr->mValue, next, out replaced);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate()
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            return mDataPtr->mResult = mAggType == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue, mDataPtr->mCount) : mDataPtr->mValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            if (mAggType == (byte)AggregationType.Min) ResetMin(ref mDataPtr->mValue);
            else if (mAggType == (byte)AggregationType.Max) ResetMax(ref mDataPtr->mValue);
            else mDataPtr->mValue = 0;
            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
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

    unsafe public struct Atomic4IntAggregator : IDisposable, IAggregator<int4, int4>, IAggregatorShell<int4, int4>
    {
        //-------------------------------------------------------------------------------------------------------------------
        #region Shell
        public static Atomic4IntAggregator Shell(AggregationType type0, AggregationType type1, AggregationType type2 = AggregationType.None, AggregationType type3 = AggregationType.None)
        {
            var a4i = new Atomic4IntAggregator()
            {
                mAllocator = Allocator.None,
                mDataPtr = null,
            };
            a4i.mAggType[1] = (byte)type0;
            a4i.mAggType[2] = (byte)type1;
            a4i.mAggType[3] = (byte)type2;
            a4i.mAggType[4] = (byte)type3;
            return a4i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void LoadData(int4* pValue, int4* pResult, int* pCount, void* pExtra) => this.mDataPtr = Data.GetDataPointerFromCountPointer(pCount);
        public bool IsShell => mAllocator == Allocator.None;
        public bool HasData => mDataPtr != null;
        #endregion Shell
        //-------------------------------------------------------------------------------------------------------------------
        #region Fields Ctor
        [StructLayout(LayoutKind.Sequential)]
        public struct Data : IAggregatorData<int4, int4>
        {
            public static Data* Allocate(Allocator allocator) => (Data*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Data>(), 64, allocator);
            public int mCount;
            public int4 mValue;
            public int4 mResult;

            public int4 Value => mValue;
            public int4 Result => mResult;

            public int4* pValue { get { fixed (int4* pValue = &mValue) return pValue; } }
            public int4* pResult { get { fixed (int4* pResult = &mResult) return pResult; } }
            public int* pCount { get { fixed (int* pCount = &mCount) return pCount; } }
            public void* pExtra => null;
            unsafe public static Data* GetDataPointerFromCountPointer(int* pCount) => (Data*)pCount;

        }

        public Atomic4IntAggregator(Allocator allocator, AggregationType type0, AggregationType type1, AggregationType type2 = AggregationType.None, AggregationType type3 = AggregationType.None)
        {
            mDataPtr = Data.Allocate(allocator);
            mAllocator = allocator;
            mAggType[1] = (byte)type0;
            mAggType[2] = (byte)type1;
            mAggType[3] = (byte)type2;
            mAggType[4] = (byte)type3;
            Reset();
        }

        internal Allocator mAllocator;
        internal fixed byte mAggType[4];
        [NativeDisableUnsafePtrRestriction] internal Data* mDataPtr;
        #endregion Fields Ctor
        //-------------------------------------------------------------------------------------------------------------------
        public bool IsCreated => mDataPtr != null;

        public AggregationType this[int index]
        {
            get
            {
                Assert.IsTrue(index > 0 && index < 4, $"index[{index}] must be in range [0-3]");
                return (AggregationType)mAggType[index];
            }
            set
            {
                Assert.IsTrue(index > 0 && index < 4, $"index[{index}] must be in range [0-3]");
                mAggType[index] = (byte)value;
            }
        }

        public ref int4 mValue
        {
            get
            {
                Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
                return ref mDataPtr->mValue;
            }
        }

        internal ref int4 mResult
        {
            get
            {
                Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
                return ref mDataPtr->mResult;
            }
        }

        internal ref int mCount
        {
            get
            {
                Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
                return ref mDataPtr->mCount;
            }
        }

        public int Count => mCount;
        public int4 Value => mValue;
        public int4 Result => mResult;
        public void SetCount(int count) => mCount = count;
        public void SetResult(int4 result) => mResult = result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Aggregate<T>(int4 next, out int4 replaced, T aggregation) where T : struct, IAggregateFunc<int4>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            return aggregation.Aggregate(ref mDataPtr->mValue, next, out replaced);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Evaluate<T>(T aggregation) where T : struct, IEvaluateFunc<int4, int4>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            return mDataPtr->mResult = aggregation.Evaluate(mDataPtr->mValue, mDataPtr->mCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset<T>(T aggregation) where T : struct, IResetFunc<int4>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            aggregation.Reset(ref mDataPtr->mValue);
            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Aggregate(int4 next, out int4 replaced)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            int4 agg;
            AggregationType aggTp = (AggregationType)mAggType[0];
            if (aggTp == AggregationType.None) { replaced.x = agg.x = mDataPtr->mValue.x; }
            else if (aggTp == AggregationType.Max) { agg.x = Max(ref mDataPtr->mValue.x, next.x, out replaced.x); }
            else if (aggTp == AggregationType.Min) { agg.x = Min(ref mDataPtr->mValue.x, next.x, out replaced.x); }
            else { agg.x = Sum(ref mDataPtr->mValue.x, next.x, out replaced.x); }

            aggTp = (AggregationType)mAggType[1];
            if (aggTp == AggregationType.None) { replaced.y = agg.y = mDataPtr->mValue.y; }
            else if (aggTp == AggregationType.Max) { agg.y = Max(ref mDataPtr->mValue.y, next.y, out replaced.y); }
            else if (aggTp == AggregationType.Min) { agg.y = Min(ref mDataPtr->mValue.y, next.y, out replaced.y); }
            else { agg.y = Sum(ref mDataPtr->mValue.y, next.y, out replaced.y); }

            aggTp = (AggregationType)mAggType[2];
            if (aggTp == AggregationType.None) { replaced.z = agg.z = mDataPtr->mValue.z; }
            else if (aggTp == AggregationType.Max) { agg.z = Max(ref mDataPtr->mValue.z, next.z, out replaced.z); }
            else if (aggTp == AggregationType.Min) { agg.z = Min(ref mDataPtr->mValue.z, next.z, out replaced.z); }
            else { agg.z = Sum(ref mDataPtr->mValue.z, next.z, out replaced.z); }

            aggTp = (AggregationType)mAggType[3];
            if (aggTp == AggregationType.None) { replaced.w = agg.w = mDataPtr->mValue.w; }
            else if (aggTp == AggregationType.Max) { agg.w = Max(ref mDataPtr->mValue.w, next.w, out replaced.w); }
            else if (aggTp == AggregationType.Min) { agg.w = Min(ref mDataPtr->mValue.w, next.w, out replaced.w); }
            else { agg.w = Sum(ref mDataPtr->mValue.w, next.w, out replaced.w); }

            return agg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Evaluate()
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mResult.x = mAggType[0] == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue.x, mDataPtr->mCount) : mDataPtr->mValue.x;
            mDataPtr->mResult.y = mAggType[1] == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue.y, mDataPtr->mCount) : mDataPtr->mValue.y;
            mDataPtr->mResult.z = mAggType[2] == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue.z, mDataPtr->mCount) : mDataPtr->mValue.z;
            mDataPtr->mResult.w = mAggType[3] == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue.w, mDataPtr->mCount) : mDataPtr->mValue.w;
            return mResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");

            AggregationType aggTp = (AggregationType)mAggType[0];
            if (aggTp == AggregationType.Min) ResetMin(ref mDataPtr->mValue.x);
            else if (aggTp == AggregationType.Max) ResetMax(ref mDataPtr->mValue.x);
            else mDataPtr->mValue.x = 0;

            aggTp = (AggregationType)mAggType[1];
            if (aggTp == AggregationType.Min) ResetMin(ref mDataPtr->mValue.y);
            else if (aggTp == AggregationType.Max) ResetMax(ref mDataPtr->mValue.y);
            else mDataPtr->mValue.y = 0;

            aggTp = (AggregationType)mAggType[2];
            if (aggTp == AggregationType.Min) ResetMin(ref mDataPtr->mValue.z);
            else if (aggTp == AggregationType.Max) ResetMax(ref mDataPtr->mValue.z);
            else mDataPtr->mValue.z = 0;

            aggTp = (AggregationType)mAggType[3];
            if (aggTp == AggregationType.Min) ResetMin(ref mDataPtr->mValue.w);
            else if (aggTp == AggregationType.Max) ResetMax(ref mDataPtr->mValue.w);
            else mDataPtr->mValue.w = 0;

            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
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


    #endregion Int Aggregator
    //-------------------------------------------------------------------------------------------------------------------
    #region Float Aggregator

    unsafe public struct AtomicFloatAggregator : IDisposable, IAggregator<float, float>, IAggregatorShell<float, float>
    {
        public static AtomicIntAggregator Shell(AggregationType type = AggregationType.Sum)
        {
            Assert.IsTrue(type != AggregationType.None, "Can not assign AggregationType.None");
            return new AtomicIntAggregator()
            {
                mAllocator = Allocator.None,
                mAggType = (byte)(type),
                mDataPtr = null,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void LoadData(float* pValue, float* pResult, int* pCount, void* pExtra) => this.mDataPtr = (Data*)pValue;
        public bool IsShell => mAllocator == Allocator.None;
        public bool HasData => mDataPtr != null;

        public AtomicFloatAggregator(Allocator allocator, AggregationType type, float initialVal)
        {
            Assert.IsTrue(type != AggregationType.None, "Can not assign AggregationType.None");
            mAllocator = allocator;
            mAggType = (byte)(type);
            mDataPtr = (Data*)UnsafeUtility.Malloc(12, 16, allocator);
            mDataPtr->mValue = initialVal;
            mDataPtr->mCount = 0;
            mDataPtr->mResult = 0;
        }

        public AtomicFloatAggregator(Allocator allocator, AggregationType type = AggregationType.Sum)
        {
            mAllocator = allocator;
            mAggType = (byte)(type);
            mDataPtr = (Data*)UnsafeUtility.Malloc(12, 16, allocator);
            Reset();
        }

        public AggregationType AggregationType
        {
            get => (AggregationType)mAggType;
            set => mAggType = (byte)value;
        }

        internal Allocator mAllocator;
        internal byte mAggType;
        [NativeDisableUnsafePtrRestriction] internal Data* mDataPtr;

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct Data : IAggregatorData<float, float>
        {
            [FieldOffset(0)] public int mCount;
            [FieldOffset(4)] public float mResult;
            [FieldOffset(8)] public float mValue;
            public float Value => mValue;
            public float Result => mResult;

            public int* pCount { get { fixed (int* pCount = &mCount) return pCount; } }
            public float* pResult { get { fixed (float* pResult = &mResult) return pResult; } }
            public float* pValue { get { fixed (float* pValue = &mValue) return pValue; } }
            public void* pExtra => null;
        }

        public bool IsCreated => mDataPtr != null;

        public float Value => *(mDataPtr->pValue);
        public int Count => *(mDataPtr->pCount);
        public float Result => *(mDataPtr->pResult);

        public void SetValue(float value)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mValue = value;
        }

        public void SetResult(float result)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mResult = result;
        }

        public void SetCount(int count)
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            mDataPtr->mCount = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Aggregate<T>(float next, out float replaced, T aggregation) where T : struct, IAggregateFunc<float>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            return aggregation.Aggregate(ref mDataPtr->mValue, next, out replaced);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate<T>(T aggregation) where T : struct, IEvaluateFunc<float, float>
        {
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            return mDataPtr->mResult = aggregation.Evaluate(mDataPtr->mValue, mDataPtr->mCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset<T>(T aggregation) where T : struct, IResetFunc<float>
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            aggregation.Reset(ref mDataPtr->mValue);
            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Aggregate(float next, out float replaced)
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            Interlocked.Increment(ref mDataPtr->mCount);
            if (mAggType == (byte)AggregationType.Max) return Max(ref mDataPtr->mValue, next, out replaced);
            else if (mAggType == (byte)AggregationType.Min) return Min(ref mDataPtr->mValue, next, out replaced);
            else return Sum(ref mDataPtr->mValue, next, out replaced);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate()
        {
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            Assert.IsFalse(mDataPtr == null, "Atomic Aggregator not Allocated!");
            return mDataPtr->mResult = mAggType == (byte)AggregationType.Avg ? Avg(mDataPtr->mValue, mDataPtr->mCount) : mDataPtr->mValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (mDataPtr == null) return;
            Assert.IsFalse(mAggType == (byte)AggregationType.None, "None Aggregation Type");
            if (mAggType == (byte)AggregationType.Min) ResetMin(ref mDataPtr->mValue);
            else if (mAggType == (byte)AggregationType.Max) ResetMax(ref mDataPtr->mValue);
            else mDataPtr->mValue = 0;
            mDataPtr->mResult = 0;
            mDataPtr->mCount = 0;
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

    #endregion Float Aggregator
    //-------------------------------------------------------------------------------------------------------------------

    public static class AtomicAgg
    {
        //-------------------------------------------------------------------------------------------------------------------
        #region Any Aggregate Function
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AtomicAggregate<TAgg>(this TAgg aggregation, ref int agg, int next, out int replaced) where TAgg : IAggregateFunc<int>
        {
            int cur, result;
            do
            {
                result = cur = agg;
                aggregation.Aggregate(ref result, next, out replaced);
            }
            while (Interlocked.CompareExchange(ref agg, result, cur) != cur);
            return agg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AtomicAggregate<TAgg>(this TAgg aggregation, ref float agg, float next, out float replaced) where TAgg : IAggregateFunc<float>
        {
            float cur, result;
            do
            {
                result = cur = agg;
                aggregation.Aggregate(ref result, next, out replaced);
            }
            while (Interlocked.CompareExchange(ref agg, result, cur) != cur);
            return agg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AtomicAggregate<TAgg>(this TAgg aggregation, ref double agg, double next, out double replaced) where TAgg : IAggregateFunc<double>
        {
            double cur, result;
            do
            {
                result = cur = agg;
                aggregation.Aggregate(ref result, next, out replaced);
            }
            while (Interlocked.CompareExchange(ref agg, result, cur) != cur);
            return agg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AtomicAggregate<TAgg>(this TAgg aggregation, ref long agg, long next, out long replaced) where TAgg : IAggregateFunc<long>
        {
            long cur, result;
            do
            {
                result = cur = agg;
                aggregation.Aggregate(ref result, next, out replaced);
            }
            while (Interlocked.CompareExchange(ref agg, result, cur) != cur);
            return agg;
        }

        #endregion Any Aggregate Function
        //-------------------------------------------------------------------------------------------------------------------
        #region Sum
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(ref int sum, int next, out int replaced)
        {
            var after = Interlocked.Add(ref sum, next);
            replaced = after - next;
            return after;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Sum(ref long sum, long next, out long replaced)
        {
            var after = Interlocked.Add(ref sum, next);
            replaced = after - next;
            return after;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(ref float sum, float next, out float replaced)
        {
            float nextSum;
            do { replaced = sum; nextSum = replaced + next; }
            while (Interlocked.CompareExchange(ref sum, nextSum, replaced) != replaced);
            return nextSum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum(ref double sum, double next, out double replaced)
        {
            double nextSum;
            do { replaced = sum; nextSum = replaced + next; }
            while (Interlocked.CompareExchange(ref sum, nextSum, replaced) != replaced);
            return nextSum;
        }
        #endregion Sum
        //-------------------------------------------------------------------------------------------------------------------
        #region Max With Reset
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(ref int prevMax, int next, out int smaller)
        {
            int greater;
            do
            {
                if ((greater = prevMax) >= next)
                {
                    smaller = next;
                    return greater;
                }
                else
                {
                    smaller = greater;
                    greater = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMax, greater, smaller) != smaller);
            return greater;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ResetMax(ref int max) => Interlocked.Exchange(ref max, int.MinValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Max(ref long prevMax, long next, out long smaller)
        {
            long greater;
            do
            {
                if ((greater = prevMax) >= next)
                {
                    smaller = next;
                    return greater;
                }
                else
                {
                    smaller = greater;
                    greater = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMax, greater, smaller) != smaller);
            return greater;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ResetMax(ref long max) => Interlocked.Exchange(ref max, long.MinValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(ref float prevMax, float next, out float smaller)
        {
            float greater;
            do
            {
                if ((greater = prevMax) >= next)
                {
                    smaller = next;
                    return greater;
                }
                else
                {
                    smaller = greater;
                    greater = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMax, greater, smaller) != smaller);
            return greater;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ResetMax(ref float max) => Interlocked.Exchange(ref max, float.MinValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(ref double prevMax, double next, out double smaller)
        {
            double greater;
            do
            {
                if ((greater = prevMax) >= next)
                {
                    smaller = next;
                    return greater;
                }
                else
                {
                    smaller = greater;
                    greater = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMax, greater, smaller) != smaller);
            return greater;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ResetMax(ref double max) => Interlocked.Exchange(ref max, double.MinValue);
        #endregion Max With Reset
        //-------------------------------------------------------------------------------------------------------------------
        #region Min With Rest
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(ref int prevMin, int next, out int greater)
        {
            int smaller;
            do
            {
                if ((smaller = prevMin) <= next)
                {
                    greater = next;
                    return smaller;
                }
                else
                {
                    greater = smaller;
                    smaller = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMin, smaller, greater) != greater);
            return smaller;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ResetMin(ref int min) => Interlocked.Exchange(ref min, int.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Min(ref long prevMin, long next, out long greater)
        {
            long smaller;
            do
            {
                if ((smaller = prevMin) <= next)
                {
                    greater = next;
                    return smaller;
                }
                else
                {
                    greater = smaller;
                    smaller = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMin, smaller, greater) != greater);
            return smaller;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ResetMin(ref long min) => Interlocked.Exchange(ref min, long.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(ref float prevMin, float next, out float greater)
        {
            float smaller;
            do
            {
                if ((smaller = prevMin) <= next)
                {
                    greater = next;
                    return smaller;
                }
                else
                {
                    greater = smaller;
                    smaller = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMin, smaller, greater) != greater);
            return smaller;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ResetMin(ref float min) => Interlocked.Exchange(ref min, float.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(ref double prevMin, double next, out double greater)
        {
            double smaller;
            do
            {
                if ((smaller = prevMin) <= next)
                {
                    greater = next;
                    return smaller;
                }
                else
                {
                    greater = smaller;
                    smaller = next;
                }
            }
            while (Interlocked.CompareExchange(ref prevMin, smaller, greater) != greater);
            return smaller;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ResetMin(ref double min) => Interlocked.Exchange(ref min, double.MaxValue);
        #endregion Min With Rest
        //-------------------------------------------------------------------------------------------------------------------
        #region Average
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Avg(float sum, int count) => count == 0 ? (sum == 0 ? 0 : (sum > 0 ? float.PositiveInfinity : float.NegativeInfinity)) : (sum / count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Avg(double sum, int count) => count == 0 ? (sum == 0 ? 0 : (sum > 0 ? double.PositiveInfinity : double.NegativeInfinity)) : (sum / count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Avg(int sum, int count) => count == 0 ? (sum == 0 ? 0 : (sum > 0 ? int.MaxValue : int.MinValue)) : (sum / count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Avg(long sum, int count) => count == 0 ? (sum == 0 ? 0 : (sum > 0 ? long.MaxValue : long.MinValue)) : (sum / count);
        #endregion Average
        //-------------------------------------------------------------------------------------------------------------------
    }
}