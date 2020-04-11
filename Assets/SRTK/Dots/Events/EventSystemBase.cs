/************************************************************************************
| File: EventSystemBase.cs                                                          |
| Project: lieene.Utility                                                           |
| Created Date: Tue Mar 31 2020                                                     |
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
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;


namespace SRTK
{
    using static EventDataTypes;
    abstract public class EventSystemBase : SystemBase
    {
        JobHandle mWaiteFroStreamAccess;
        JobHandle mWaiteFroEventProcess;
        protected EventStreamer mStreamer = new EventStreamer(Allocator.Persistent);
        protected internal NativeList<GenericEvent> mEvents;
        protected NativeRef<UnsafeAppendBuffer> mExternalDataCacheRef = new NativeRef<UnsafeAppendBuffer>(Allocator.Persistent);

        protected NativeCounter mEventIDCounter = new NativeCounter(Allocator.Persistent, 0);
        protected EventTypeRegistry mTypeRegistry = new EventTypeRegistry(Allocator.Persistent);

        //-------------------------------------------------------------------------------------------------------------------
        #region Public Interface

        public JobHandle WaiteFroStreamAccess
        {
            get => mWaiteFroStreamAccess;
            set => mWaiteFroStreamAccess = JobHandle.CombineDependencies(mWaiteFroStreamAccess, value);
        }

        public JobHandle WaiteFroEventProcess
        {
            get => mWaiteFroEventProcess;
            set => mWaiteFroEventProcess = JobHandle.CombineDependencies(mWaiteFroEventProcess, value);
        }

        /// <summary>
        /// Used to register type ID and data segment length
        /// </summary>
        public ref EventTypeRegistry TypeRegistry => ref mTypeRegistry;

        /// <summary>
        /// Event ID counter, can be used in parallel job or mainthread to get next group ID
        /// only reset on 2 billion actions, safe to use
        /// Event with same ID is allowed to mark event group
        /// </summary>
        public NativeCounter EventIDCounter => mEventIDCounter;

        /// <summary>
        /// Use returned Writer to write events to system
        /// </summary>
        public EventWriter GetStreamWriter() => mStreamer.AsWriter();
        #endregion Public Interface
        //-------------------------------------------------------------------------------------------------------------------

        #region Lifecycle

        protected override void OnCreate() { }

        sealed protected override void OnUpdate()
        {
            //safe to rest event id on one billion ---------------------------------------------
            if (mEventIDCounter.Value > (int.MaxValue >> 1)) mEventIDCounter.Reset(0);

            //release event of last frame
            if (mEvents.IsCreated)
            {
                mEvents.Dispose(mWaiteFroEventProcess);
                mEvents = default;
            }

            if (mStreamer.IsDirty)
            {
                mStreamer.ClearDirty();
                ReadEvents();
                OnProcessEvents();
            }
        }

        unsafe void ReadEvents()
        {
            //Allocate events container--------------------------------------------------
            var streamer = mStreamer;
            var events = mEvents = new NativeList<GenericEvent>(Allocator.TempJob);
            mWaiteFroStreamAccess = JobHandle.CombineDependencies(mWaiteFroStreamAccess, Dependency);
            mWaiteFroStreamAccess = mStreamer.CollectElementsToRead(out var batch2Read, out var elementCount, Allocator.TempJob, mWaiteFroStreamAccess);
            mWaiteFroStreamAccess = Job.WithName("ResizeEventList").WithCode(() => { events.Capacity = elementCount.Value; }).Schedule(mWaiteFroStreamAccess);
            elementCount.Dispose(mWaiteFroStreamAccess);

            //Read events-----------------------------------------------------------------
            mWaiteFroStreamAccess = new ReadEventJob()
            {
                StreamReader = streamer.AsReader(),
                EventsOut = events.AsParallelWriter(),
                Batch2Read = batch2Read,
            }.Schedule<ReadEventJob, int>(batch2Read, 4, mWaiteFroStreamAccess);
            batch2Read.Dispose(mWaiteFroStreamAccess);

            // Before Move external data to cache -----------------------------------------
            OnFilterEvents();
            // last chance to sort or filter event 

            //Calculate size of all External data in events -------------------------------
            var externalDataSizeCounter = new NativeCounter(Allocator.TempJob, 0);
            mWaiteFroStreamAccess = new SumSizeJob()
            {
                SizeCounter = externalDataSizeCounter,
                Events = events,
            }.Schedule(events, 128, mWaiteFroStreamAccess);

            //build external data cache ----------------------------------------------------
            if (!mExternalDataCacheRef.Value.IsCreated)
            {
                //allocate buffer once
                mExternalDataCacheRef.Value = new UnsafeAppendBuffer(128, JobsUtility.CacheLineSize, Allocator.Persistent);
            }

            var cacheRef = mExternalDataCacheRef;

            mWaiteFroStreamAccess = Job.WithName("MoveExtEventToCache").WithCode(() =>
            {
                //Update cache Capacity before Move so the pointer will not get invalidate by relocate
                var cache = cacheRef.AsRef;
                cache.Reset();
                var requiredCap = externalDataSizeCounter.Value;
                if (cache.Capacity < requiredCap) { cache.SetCapacity(requiredCap); }

                //move event external data to cache, so the stream can be released and data will be coherent
                for (int i = 0, len = events.Length; i < len; i++)
                {
                    var evt = events[i];
                    evt.MoveExternalDataTo(ref cache);
                    events[i] = evt;
                }
            }).Schedule(mWaiteFroStreamAccess);
            externalDataSizeCounter.Dispose(mWaiteFroStreamAccess);

            //Safe to release stream and reallocate new one for next frame ---------------------
            Dependency = mWaiteFroEventProcess = mWaiteFroStreamAccess;
            //job after this will not need to change Dependency
            //instead waiteFroEventProcess should be used
        }

        struct ReadEventJob : IJobParallelForDefer
        {
            [ReadOnly] public EventStreamer.EventReader StreamReader;
            [ReadOnly] public NativeList<int> Batch2Read;
            public NativeList<GenericEvent>.ParallelWriter EventsOut;
            public void Execute(int index)
            {
                var h = StreamReader.BeginBatch(Batch2Read[index]);
                for (int i = 0, len = h.ElementCount; i < len; i++)
                { EventsOut.AddNoResize(h.ReadEvent()); }
                h.EndBatch();
            }
        }

        struct SumSizeJob : IJobParallelForDefer
        {
            public NativeCounter SizeCounter;
            [ReadOnly] public NativeList<GenericEvent> Events;
            public void Execute(int index) => SizeCounter.Add(Events[index].SizeInfo.ExternalDataByteSize);
        }

        protected virtual void OnFilterEvents() { }
        protected abstract void OnProcessEvents();

        protected override void OnDestroy()
        {
            if (mEvents.IsCreated) mEvents.Dispose();
            mEvents = default;

            mExternalDataCacheRef.DisposeRefTarget(true);
            mExternalDataCacheRef = default;

            mStreamer.Dispose();
            mStreamer = default;

            mEventIDCounter.Dispose();
            mEventIDCounter = default;

            mTypeRegistry.Dispose();
            mTypeRegistry = default;

        }
        #endregion Lifecycle
        //-------------------------------------------------------------------------------------------------------------------
    }

    abstract public class EventPrcessor<T> : SystemBase
        where T : EventSystemBase
    {
        protected T mEventSystem = null;
        protected NativeArray<GenericEvent> mEvents = default;
        protected override void OnCreate()
        {
            mEventSystem = World.GetOrCreateSystem<T>();
        }

        sealed protected override void OnUpdate()
        {
            if (mEventSystem.mEvents.IsCreated)
            {
                mEvents = mEventSystem.mEvents.AsArray();
                OnProcessEvents();
            }
        }
        protected abstract void OnProcessEvents();
    }
}