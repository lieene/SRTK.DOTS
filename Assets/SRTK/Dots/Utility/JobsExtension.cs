/************************************************************************************
| File: JobsExtension.cs                                                            |
| Project: lieene.Unsafe                                                            |
| Created Date: Mon Mar 2 2020                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Thu Apr 09 2020                                                    |
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace SRTK
{
    internal static class JobsExtension
    {
        unsafe public static JobHandle Schedule<T>(this T jobData, NativeArray<int> forEachCount, int innerloopBatchCount, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
        { return IJobParallelForDeferExtensions.Schedule(jobData, (int*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(forEachCount), innerloopBatchCount, dependsOn); }

        unsafe public static JobHandle Schedule<T>(this T jobData, NativeRef<int> forEachCount, int innerloopBatchCount, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
        { return IJobParallelForDeferExtensions.Schedule(jobData, forEachCount.Ptr, innerloopBatchCount, dependsOn); }
    }

    public static class JobLogger
    {
        [BurstDiscard] public static void Log<T>(T segment) => Debug.Log(AppendToString(segment));
        [BurstDiscard] public static void Log<T1, T2>(T1 segment1, T2 segment2) => Debug.Log(AppendToString(segment1, segment2));
        [BurstDiscard] public static void Log<T1, T2, T3>(T1 segment1, T2 segment2, T3 segment3) => Debug.Log(AppendToString(segment1, segment2, segment3));
        [BurstDiscard] public static void Log<T1, T2, T3, T4>(T1 segment1, T2 segment2, T3 segment3, T4 segment4) => Debug.Log(AppendToString(segment1, segment2, segment3, segment4));
        [BurstDiscard] public static void Log<T1, T2, T3, T4, T5>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5) => Debug.Log(AppendToString(segment1, segment2, segment3, segment4, segment5));
        [BurstDiscard] public static void Log<T1, T2, T3, T4, T5, T6>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6) => Debug.Log(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6));
        [BurstDiscard] public static void Log<T1, T2, T3, T4, T5, T6, T7>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7) => Debug.Log(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7));
        [BurstDiscard] public static void Log<T1, T2, T3, T4, T5, T6, T7, T8>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7, T8 segment8) => Debug.Log(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7, segment8));

        [BurstDiscard] public static void LogWarning<T>(T segment) => Debug.LogWarning(AppendToString(segment));
        [BurstDiscard] public static void LogWarning<T1, T2>(T1 segment1, T2 segment2) => Debug.LogWarning(AppendToString(segment1, segment2));
        [BurstDiscard] public static void LogWarning<T1, T2, T3>(T1 segment1, T2 segment2, T3 segment3) => Debug.LogWarning(AppendToString(segment1, segment2, segment3));
        [BurstDiscard] public static void LogWarning<T1, T2, T3, T4>(T1 segment1, T2 segment2, T3 segment3, T4 segment4) => Debug.LogWarning(AppendToString(segment1, segment2, segment3, segment4));
        [BurstDiscard] public static void LogWarning<T1, T2, T3, T4, T5>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5) => Debug.LogWarning(AppendToString(segment1, segment2, segment3, segment4, segment5));
        [BurstDiscard] public static void LogWarning<T1, T2, T3, T4, T5, T6>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6) => Debug.LogWarning(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6));
        [BurstDiscard] public static void LogWarning<T1, T2, T3, T4, T5, T6, T7>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7) => Debug.LogWarning(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7));
        [BurstDiscard] public static void LogWarning<T1, T2, T3, T4, T5, T6, T7, T8>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7, T8 segment8) => Debug.LogWarning(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7, segment8));

        [BurstDiscard] public static void LogError<T>(T segment) => Debug.LogError(AppendToString(segment));
        [BurstDiscard] public static void LogError<T1, T2>(T1 segment1, T2 segment2) => Debug.LogError(AppendToString(segment1, segment2));
        [BurstDiscard] public static void LogError<T1, T2, T3>(T1 segment1, T2 segment2, T3 segment3) => Debug.LogError(AppendToString(segment1, segment2, segment3));
        [BurstDiscard] public static void LogError<T1, T2, T3, T4>(T1 segment1, T2 segment2, T3 segment3, T4 segment4) => Debug.LogError(AppendToString(segment1, segment2, segment3, segment4));
        [BurstDiscard] public static void LogError<T1, T2, T3, T4, T5>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5) => Debug.LogError(AppendToString(segment1, segment2, segment3, segment4, segment5));
        [BurstDiscard] public static void LogError<T1, T2, T3, T4, T5, T6>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6) => Debug.LogError(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6));
        [BurstDiscard] public static void LogError<T1, T2, T3, T4, T5, T6, T7>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7) => Debug.LogError(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7));
        [BurstDiscard] public static void LogError<T1, T2, T3, T4, T5, T6, T7, T8>(T1 segment1, T2 segment2, T3 segment3, T4 segment4, T5 segment5, T6 segment6, T7 segment7, T8 segment8) => Debug.LogError(AppendToString(segment1, segment2, segment3, segment4, segment5, segment6, segment7, segment8));

        [BurstDiscard]
        public static string AppendToString(params object[] parts)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Clear();
            for (int i = 0, len = parts.Length; i < len; i++) sb.Append(parts[i].ToString());
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Semaphore
    {
        public static Semaphore Default => default;
        [FieldOffset(0)] int Count;
        public bool IsEmpty => Count == 0;

        public int Produce(int count = 1) => Interlocked.Add(ref Count, count);
        public bool TryProduce(int MaxCount, int count = 1)
        {
            Assert.IsTrue(count >= 0, "Produce count must be none-negative");
            int current, next;
            do
            {
                current = Count;
                next = current + count;
                if (next > MaxCount) return false;
            }
            while (Interlocked.CompareExchange(ref Count, next, current) != current);
            return true;
        }

        public bool TryConsume(int count = 1)
        {
            Assert.IsTrue(count >= 0, "Consume count must be none-negative");
            int current, next;
            do
            {
                current = Count;
                next = current - count;
                if (next < 0) return false;
            }
            while (Interlocked.CompareExchange(ref Count, next, current) != current);
            return true;
        }

        public bool TryLock() => Interlocked.CompareExchange(ref Count, 1, 0) != 0;
        public bool TryFree() => Interlocked.CompareExchange(ref Count, 0, 1) != 0;

        public LockHandle Lock()
        {
            while (!TryLock()) ;
            unsafe { fixed (Semaphore* pThis = &this) return new LockHandle() { mPtr = pThis }; }
        }

        public void Free()
        {
            bool success = TryFree();
            Assert.IsTrue(success, "Semaphore not Locked");
        }

        public RestHandle Reset()
        {
            while (!TryLock()) ;
            unsafe { fixed (Semaphore* pThis = &this) return new RestHandle() { mPtr = pThis }; }
        }

        public struct LockHandle : IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            unsafe internal Semaphore* mPtr;
            unsafe public void Dispose() { if (mPtr != null) { mPtr->Free(); mPtr = null; } }
        }

        public struct RestHandle : IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            unsafe internal Semaphore* mPtr;
            unsafe public void Dispose() { if (mPtr != null) { Interlocked.Exchange(ref mPtr->Count, 0); mPtr = null; } }
        }

    }

    [BurstCompile]
    public struct ListResizeJob<T> : IJob
        where T : unmanaged
    {
        public int ResizeLength;
        public NativeList<T> list;
        public void Execute() { list.ResizeUninitialized(ResizeLength); }
    }

    [BurstCompile]
    public struct DisposeJob<T> : IJob where T : IDisposable
    {
        public DisposeJob(T target) { this.toDispose = target; }
        [DeallocateOnJobCompletion] public T toDispose;
        public void Execute() { /*Nah...*/}
    }
    [BurstCompile]
    public struct DisposeInJob<T> : IJob where T : IDisposable
    {
        public DisposeInJob(T target) { this.toDispose = target; }
        public T toDispose;
        public void Execute() => toDispose.Dispose();
    }

    [BurstCompile]
    public unsafe struct UnsafeDisposeInJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
        public Allocator Allocator;
        public void Execute() { UnsafeUtility.Free(Ptr, Allocator); }
    }
}