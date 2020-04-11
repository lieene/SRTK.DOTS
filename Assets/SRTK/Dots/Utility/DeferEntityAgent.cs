/************************************************************************************
| File: DeferEntityAgent.cs                                                         |
| Project: lieene.Unsafe                                                            |
| Created Date: Fri Feb 28 2020                                                     |
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

using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;

namespace SRTK
{
    using static Interlocked;
    public struct DeferEntityID : IComponentData
    {
        public readonly static DeferEntityID NULL = new DeferEntityID() { index = -1 };
        internal DeferEntityID(in int index) { this.index = index; }
        public bool IsValid => this.index >= 0;
        public int Index => index;
        internal int index;
        public static implicit operator int(in DeferEntityID idx) => idx.index;
        public static implicit operator DeferEntityID(in int idx) => new DeferEntityID(idx);
    }

    public struct DeferEntity
    {
        public readonly static DeferEntity NULL = new DeferEntity() { DeferID = DeferEntityID.NULL, ecbPlaceHolderEntity = Entity.Null };
        internal DeferEntity(in int index, in Entity entity)
        {
            if (index < 0) throw new ArgumentException($"Native Defer index[{index}] not allowed!");
            this.DeferID = new DeferEntityID(index);
            ecbPlaceHolderEntity = entity;
        }
        internal DeferEntity(in int index) : this(index, Entity.Null) { }

        /// <summary>
        /// a Integer index to track entity that is not created yet, can be used with <see cref="DeferEntityCreator.ParallelAccessor.GetDeferEntity(in DeferEntityID)"/>
        /// </summary>
        public DeferEntityID DeferID;

        /// <summary>
        /// <see cref="EntityCommandBuffer.CreateEntity(EntityArchetype)"/> returned placeholder entity
        /// </summary>
        public Entity ECBPlaceHolderEntity => ecbPlaceHolderEntity;

        /// <summary>
        /// <see cref="EntityCommandBuffer.CreateEntity(EntityArchetype)"/> returned placeholder entity
        /// </summary>
        internal Entity ecbPlaceHolderEntity;
        public static implicit operator int(DeferEntity idx) => idx.DeferID.index;
        public static implicit operator DeferEntity(int idx) => new DeferEntity(idx, Entity.Null);
        public static implicit operator DeferEntityID(DeferEntity idx) => idx.DeferID.index;
        public static implicit operator DeferEntity(DeferEntityID idx) => new DeferEntity(idx, Entity.Null);
        public static explicit operator Entity(DeferEntity idx) => idx.ecbPlaceHolderEntity;
    }

    [BurstCompile]
    internal unsafe struct DeferEntityHolder : IDisposable
    {
        internal DeferEntityHolder(in Allocator allocator)
        {
            m_allocator = allocator;
            m_counter = new IntPtr(UnsafeUtility.Malloc(UnsafeUtility.SizeOf(typeof(int)), 4, allocator));
            //cache = new NativeList<Entity>(allocator);
            cache = default;
            CounterRef = 0;
        }

        internal Allocator m_allocator;
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr m_counter;

        public bool IsCreated => m_counter != IntPtr.Zero;
        internal ref int CounterRef => ref *((int*)m_counter.ToPointer());
        internal int* CounterPtr => (int*)m_counter.ToPointer();

        //internal NativeList<Entity> cache;
        internal NativeArray<Entity> cache;


        public void Dispose()
        {
            var toFree = Exchange(ref m_counter, IntPtr.Zero);
            if (toFree != IntPtr.Zero) UnsafeUtility.Free(toFree.ToPointer(), m_allocator);
            if (cache.IsCreated) cache.Dispose();
        }

        public JobHandle Dispose(JobHandle dep)
        {
            dep.Complete();
            var toFree = Exchange(ref m_counter, IntPtr.Zero);
            if (toFree != IntPtr.Zero) UnsafeUtility.Free(toFree.ToPointer(), m_allocator);
            return cache.Dispose(dep);
        }

        public void AllocateCacheForFilling()
        {
            cache = new NativeArray<Entity>(CounterRef, m_allocator, NativeArrayOptions.UninitializedMemory);
        }

        internal DeferEntity CreateDeferEntity(in Entity rawEntity = default)
        {
            var idx = Increment(ref CounterRef);
            return new DeferEntity(idx - 1, rawEntity);
        }

        internal Entity GetDeferEntity(in int index)
        {
            if (ExistDeferEntity(index)) return cache[index];
            else throw new AccessViolationException($"Defer Entity of index :{index} not exist");
        }

        internal void SetDeferEntity(in int index, in Entity data)
        {
            if (ExistDeferEntity(index)) cache[index] = data;
            else throw new AccessViolationException($"Defer Entity of index :{index} not exist");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ExistDeferEntity(in int index) => index >= 0 && index < cache.Length;

        //internal JobHandle ResetAfter(in JobHandle dep) => (new ResetJob() { m_counter = CounterPtr, cache = cache }).Schedule(dep);
        // internal struct ResetJob : IJob
        // {
        //     [NativeDisableUnsafePtrRestriction] internal int* m_counter;
        //     [DeallocateOnJobCompletion] internal NativeList<Entity> cache;
        //     public void Execute() { *m_counter = 0; }
        // }
        // internal void Reset() { CounterRef = 0; cache.Clear(); }

        //internal void ResizeCacheFroFilling() { cache.ResizeUninitialized(CounterRef); }

        internal ParallelCreator ToParallelCreator() => new ParallelCreator() { m_counter = CounterPtr };
        internal ParallelAccessor ToParallelAccessor() => new ParallelAccessor() { cache = cache };


        internal struct ParallelCreator
        {
            [NativeDisableUnsafePtrRestriction]
            internal int* m_counter;
            internal DeferEntity CreateDeferEntity(in Entity rawEntity = default)
            {
                var idx = Increment(ref *m_counter);
                return new DeferEntity(idx - 1, rawEntity);
            }
        }

        internal struct ParallelAccessor
        {
            [NativeDisableParallelForRestriction] internal NativeArray<Entity> cache;
            internal Entity GetDeferEntity(in int index)
            {
                if (index >= 0 && index < cache.Length) { return cache[index]; }
                else throw new AccessViolationException($"Defer Entity of index :{index} not exist");
            }

            internal void SetDeferEntity(in int index, in Entity e)
            {
                if (index >= 0 && index < cache.Length) { cache[index] = e; }
                else throw new AccessViolationException($"Defer Entity of index :{index} not exist");
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool ExistDeferEntity(in int index) => index >= 0 && index < cache.Length;
        }
    }

    public struct DeferEntityAccessor
    {
        internal DeferEntityAccessor(in DeferEntityHolder entityHolder) { this.entityHolder = entityHolder; }
        internal DeferEntityHolder entityHolder;
        public Entity GetDeferEntity(in DeferEntityID idx) => entityHolder.GetDeferEntity(idx);
        public bool ExistDeferEntity(in DeferEntityID idx) => entityHolder.ExistDeferEntity(idx);
        internal void SetDeferEntity(in DeferEntityID idx, in Entity e) => entityHolder.SetDeferEntity(idx, e);

        internal Parallel ToParallel() => new Parallel() { accessor = entityHolder.ToParallelAccessor() };
        public struct Parallel
        {
            internal DeferEntityHolder.ParallelAccessor accessor;
            public Entity GetDeferEntity(in DeferEntityID idx) => accessor.GetDeferEntity(idx);
            public bool ExistDeferEntity(in DeferEntityID idx) => accessor.ExistDeferEntity(idx);
            internal void SetDeferEntity(in DeferEntityID idx, Entity data) => accessor.SetDeferEntity(idx, data);
        }

    }
    
    public struct DeferEntityCreator
    {
        internal DeferEntityCreator(in EntityCommandBufferSystem ecbs, in DeferEntityHolder entityHolder)
        {
            this.ecbs = ecbs;
            this.ecb = ecbs.CreateCommandBuffer();
            this.entityHolder = entityHolder;
        }

        internal DeferEntityHolder entityHolder;

        public EntityCommandBuffer entityCommandBuffer => ecb;
        internal EntityCommandBuffer ecb;

        public EntityCommandBufferSystem entityCommandBufferSystem => ecbs;
        internal EntityCommandBufferSystem ecbs;

        public DeferEntity CreateDeferEntity(in EntityArchetype archetype = default)
        {
            var de = entityHolder.CreateDeferEntity(ecb.CreateEntity(archetype));
            ecb.AddComponent(de.ecbPlaceHolderEntity, de.DeferID);
            return de;
        }

        public Parallel ToParallel() => new Parallel()
        { creator = entityHolder.ToParallelCreator(), ecbc = ecb.ToConcurrent() };

        public struct Parallel
        {
            internal DeferEntityHolder.ParallelCreator creator;
            internal EntityCommandBuffer.Concurrent ecbc;
            public EntityCommandBuffer.Concurrent entityCommandBufferConcurrent => ecbc;
            public DeferEntity CreateDeferEntity(in int jobIndex, in EntityArchetype archetype = default)
            {
                var de = creator.CreateDeferEntity(ecbc.CreateEntity(jobIndex, archetype));
                ecbc.AddComponent(jobIndex, de.ecbPlaceHolderEntity, de.DeferID);
                return de;
            }
        }

    }

}
