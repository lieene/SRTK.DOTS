/************************************************************************************
| File: NativeTypes.cs                                                              |
| Project: lieene.Utility                                                           |
| Created Date: Wed Mar 11 2020                                                     |
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
    using static Interlocked;
    //-------------------------------------------------------------------------------------------------------------------
    public static class LowLevelUnSafeExt
    {
        public static bool ShouldDeallocate(this Allocator allocator) => allocator > Allocator.None;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckAlignment(this int alignment)
        {
            var zeroAlignment = alignment == 0;
            var powTwoAlignment = ((alignment - 1) & alignment) == 0;
            var validAlignment = (!zeroAlignment) && powTwoAlignment;
            if (!validAlignment)
            {
                throw new ArgumentException($"Specified alignment must be non-zero positive power of two. Requested: {alignment}");
            }
        }

        public static int Align(this int offset, int alignment)
        {
            Assert.IsTrue(IsValidAlignment(alignment));
            //padding = (alignment - (offset mod alignment)) mod alignment //alignment does not need to be power of 2
            //        = (alignment - (offset & (alignment - 1))) & (alignment - 1); //alignment must be power of 2
            //        = (-offset & (alignment - 1))
            //aligned = offset + padding
            //        = offset + ((alignment - (offset mod alignment)) mod alignment) //alignment does not need to be power of 2
            //        = (offset + (alignment - 1)) & ~(alignment - 1);//alignment must be power of 2, add one less than alignment to offset, and remove remainder over alignment
            //        = (offset + (alignment - 1)) & -alignment;// ~(alignment - 1) = -alignment
            //        = offset + (-offset & (alignment - 1))
            return (offset + (alignment - 1)) & -alignment;
        }

        public static int PaddingOf(this int offset, int alignment)
        {
            Assert.IsTrue(IsValidAlignment(alignment));
            //padding = (alignment - (offset mod alignment)) mod alignment
            //        = (alignment - (offset & (alignment - 1))) & (alignment - 1);
            //        = (-offset & (alignment - 1))
            return -offset & (alignment - 1);

        }

        public static bool IsValidAlignment(this int alignment)
        {
            //alignment must be 
            //    non-zero positive               power of two
            return (alignment > 0) && (((alignment - 1) & alignment) == 0);
        }
    }

    public static class NaticeTypeExtension
    {
        public static void DisposeRefTarget<T>(this NativeRef<T> nativeRef, bool DisposeSelf = true) where T : unmanaged, IDisposable
        {
            nativeRef.AsRef.Dispose();
            nativeRef.AsRef=default;
            if (DisposeSelf) { nativeRef.Dispose(); }
        }

        public static JobHandle DisposeRefTarget<T>(this NativeRef<T> nativeRef, JobHandle dependsOn, bool DisposeSelf = true) where T : unmanaged, IDisposable
        {
            var targetJob = new DisposeInJob<T>(nativeRef.AsRef).Schedule(dependsOn);
            nativeRef.AsRef=default;
            if (DisposeSelf) { dependsOn = nativeRef.Dispose(dependsOn); }
            return JobHandle.CombineDependencies(targetJob,dependsOn);
        }

    }

    //-------------------------------------------------------------------------------------------------------------------
    [BurstCompile]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    unsafe public struct NativeCounter : IDisposable
    {
        Allocator Allocator;
        [NativeDisableUnsafePtrRestriction]
        internal int* pCounter;
        public NativeCounter(in Allocator allocator, in int startCount = 0)
        {
            this.Allocator = allocator;
            pCounter = (int*)UnsafeUtility.Malloc(sizeof(int), 4, allocator);
            *pCounter = startCount;
        }
        public bool IsCreated => pCounter != null;
        /// <summary>
        /// Count++
        /// </summary>
        public int Count => Increment(ref *pCounter) - 1;

        /// <summary>
        /// ++Count
        /// </summary>
        public int Next => Increment(ref *pCounter);
        public int Add(int value) => Interlocked.Add(ref *pCounter, value);

        public int Value => *pCounter;

        public void Reset(int startCount = 0) => *pCounter = startCount;

        public void Dispose()
        {
            if (Allocator.ShouldDeallocate())
            {
                UnsafeUtility.Free(pCounter, Allocator);
                Allocator = Allocator.Invalid;
            }
            pCounter = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (Allocator.ShouldDeallocate())
            {
                dependsOn = new UnsafeDisposeInJob { Ptr = pCounter, Allocator = Allocator }.Schedule(dependsOn);
                Allocator = Allocator.Invalid;
            }
            pCounter = null;
            return dependsOn;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    [BurstCompile]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct NativeMaxCounter : IDisposable
    {
        /// <summary>
        /// Generate a counter that can count up to upperLimit
        /// </summary>
        /// <param name="allocator">allocation type</param>
        /// <param name="max">upper limit value, when counter reach this value false is returned by <see cref="TryNext"/></param>
        /// <param name="startCount"></param>
        public NativeMaxCounter(in Allocator allocator, in int max, in int startCount = 0)
        {
            this.max = new NativeRef<int>(allocator, max);
            this.counter = new NativeCounter(allocator, startCount);
        }
        internal NativeCounter counter;

        public int Max
        {
            get => max.Value;
            set => max.Value = value;
        }
        internal NativeRef<int> max;

        public bool IsCreated => counter.IsCreated;

        public void Reset(int startCount = 0) => counter.Reset(startCount);

        /// <summary>
        /// ++Count
        /// </summary>
        public bool TryNext(out int next)
        {
            next = counter.Next;
            var _max = max.Value;
            //trying not to use if
            return next >= _max ? (next = _max) < int.MinValue : true;
            //if (next >= MaxCount) { next = MaxCount; return false; } else { return true; }
        }

        /// <summary>
        /// Count++
        /// </summary>
        public bool TryCount(out int current)
        {
            current = counter.Count;
            var _max = max.Value;
            //trying not to use if
            return current >= _max ? (current = _max) < int.MinValue : true;
            //if (current >= MaxCount) { current = MaxCount; return false; } else { return true; }
        }


        public bool TryAdd(int value, out int added)
        {
            added = counter.Add(value);
            var _max = max.Value;
            return added >= _max ? (added = _max) < int.MinValue : true;
        }

        public int Value
        {
            get
            {
                var value = counter.Value;
                var _max = max.Value;
                return value > _max ? _max : value;
            }
        }


        public void Dispose()
        {
            if (counter.IsCreated)
            {
                counter.Dispose();
                max.Dispose();
            }
            counter = default;
            max = default;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (counter.IsCreated) { dependsOn = JobHandle.CombineDependencies(counter.Dispose(dependsOn), max.Dispose(dependsOn)); }
            counter = default;
            max = default;
            return dependsOn;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    [BurstCompile]
    unsafe public struct NativeRef<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal T* Ptr;
        Allocator Allocator;

        public NativeRef(in Allocator allocator, T value = default)
        {
            this.Allocator = allocator;
            Ptr = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), JobsUtility.CacheLineSize, Allocator);
            UnsafeUtility.CopyStructureToPtr(ref value, Ptr);
        }

        public bool IsCreated => Ptr != null;

        public T Value
        {
            get
            {
                Assert.IsTrue(Ptr != null);
                UnsafeUtility.CopyPtrToStructure<T>(Ptr, out var value);
                return value;
            }
            set
            {
                Assert.IsTrue(Ptr != null);
                UnsafeUtility.CopyStructureToPtr(ref value, Ptr);
            }
        }

        public ref T AsRef
        {
            get
            {
                Assert.IsTrue(Ptr != null);
                return ref *Ptr;
            }
        }

        public void Dispose()
        {
            if (Allocator.ShouldDeallocate())
            {
                UnsafeUtility.Free(Ptr, Allocator);
                Allocator = Allocator.Invalid;
            }
            Ptr = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (Allocator.ShouldDeallocate())
            {
                dependsOn = new UnsafeDisposeInJob { Ptr = Ptr, Allocator = Allocator }.Schedule(dependsOn);
                Allocator = Allocator.Invalid;
            }
            Ptr = null;
            return dependsOn;
        }
    }
    [BurstCompile]
    unsafe public struct NativeTypeLessRef : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* Ptr;
        Allocator Allocator;

        public static NativeTypeLessRef AsReference<T>(in Allocator allocator, T value = default) where T : unmanaged
        {
            var byteSize = UnsafeUtility.SizeOf<T>();
            var r = new NativeTypeLessRef()
            {
                Allocator = allocator,
                Ptr = UnsafeUtility.Malloc(byteSize, math.ceilpow2(byteSize), allocator)
            };
            UnsafeUtility.CopyStructureToPtr(ref value, r.Ptr);
            return r;
        }

        public T GetValueAs<T>() where T : unmanaged
        {
            Assert.IsTrue(Ptr != null);
            UnsafeUtility.CopyPtrToStructure<T>(Ptr, out var value);
            return value;
        }

        public void SetValueAs<T>(T value) where T : unmanaged
        {
            Assert.IsTrue(Ptr != null);
            UnsafeUtility.CopyStructureToPtr(ref value, Ptr);
        }

        public ref T AsRef<T>() where T : unmanaged
        {
            Assert.IsTrue(Ptr != null);
            return ref *(T*)Ptr;
        }

        public bool IsCreated => Ptr != null;

        public void Dispose()
        {
            if (Allocator.ShouldDeallocate())
            {
                UnsafeUtility.Free(Ptr, Allocator);
                Allocator = Allocator.Invalid;
            }
            Ptr = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (Allocator.ShouldDeallocate())
            {
                dependsOn = new UnsafeDisposeInJob { Ptr = Ptr, Allocator = Allocator }.Schedule(dependsOn);
                Allocator = Allocator.Invalid;
            }
            Ptr = null;
            return dependsOn;
        }
    }
    [BurstCompile]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct NativeBool
    {
        public const byte False = 0;
        public const byte True = 1;
        byte value;
        public bool Value
        {
            get => value == True;
            set => this.value = value ? True : False;
        }

        public static implicit operator bool(NativeBool b) => b.Value;
        public static implicit operator NativeBool(bool b) => new NativeBool() { value = b ? True : False };
    }

    [BurstCompile]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    unsafe public struct NativeAppendBuffer : IDisposable
    {
        public NativeAppendBuffer(int initialCapacity, int alignment, Allocator allocator)
        {
            mBuffer = (UnsafeAppendBuffer*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeAppendBuffer>(), 16, allocator);
            *mBuffer = new UnsafeAppendBuffer(initialCapacity, alignment, allocator);
        }
        public bool IsCreated => mBuffer != null;

        UnsafeAppendBuffer* mBuffer;
        public ref UnsafeAppendBuffer Buffer => ref *mBuffer;

        public void Dispose()
        {
            if (mBuffer->Allocator.ShouldDeallocate())
            {
                mBuffer->Dispose();
                UnsafeUtility.Free(mBuffer, mBuffer->Allocator);
            }
            mBuffer = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (mBuffer->Allocator.ShouldDeallocate())
            {
                var allocator = mBuffer->Allocator;
                dependsOn = mBuffer->Dispose(dependsOn);
                dependsOn = new UnsafeDisposeInJob { Ptr = mBuffer, Allocator = allocator }.Schedule(mBuffer->Dispose(dependsOn));
            }
            mBuffer = null;
            return dependsOn;
        }
    }
}
