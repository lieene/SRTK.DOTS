/************************************************************************************
| File: CompositeSystemBase.cs                                                      |
| Project: lieene.Utility                                                           |
| Created Date: Tue Mar 31 2020                                                     |
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
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities.CodeGeneratedJobForEach;

namespace SRTK
{
    public abstract class CompositeSystemBase : SystemBase
    //-------------------------------------------------------------------------------------------------------------------
    {
        #region Sort by Rank
        internal bool isSorted = false;
        public bool IsSorted => isSorted && SubSystems.TrueForAll(sub => sub.IsSorted);
        public virtual void Sort(bool recursive = true)
        {
            if (!isSorted) SubSystems.Sort((a, b) => a.rank - b.rank);
            isSorted = true;
            if (recursive)
            { for (int i = 0, len = SubSystems.Count; i < len; i++) SubSystems[i].Sort(recursive); }
        }
        #endregion Sort by Rank
        //-------------------------------------------------------------------------------------------------------------------
        #region Hierarchy
        internal List<SubSystemBase> SubSystems = new List<SubSystemBase>();

        public virtual void AddSubSystem(SubSystemBase sub)
        {
            Assert.IsFalse(SubSystems.Contains(sub));
            sub.RemoveFromSystem();
            sub.AddToSystem(this);
        }

        public T GetSubSystem<T>(bool recursive = true) where T : SubSystemBase
        {
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            {
                var sub = SubSystems[i];
                if (sub is T) return sub as T;
                else if (recursive)
                {
                    T subT = sub.GetSubSystem<T>(recursive);
                    if (subT != null) return subT;
                }
            }
            return null;
        }

        public T GetSubSystem<T>(string name, bool recursive = true) where T : SubSystemBase
        {
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            {
                var sub = SubSystems[i];
                if (sub is T && sub.Name == name) return sub as T;
                else if (recursive)
                {
                    T subT = sub.GetSubSystem<T>(name, recursive);
                    if (subT != null) return subT;
                }
            }
            return null;
        }

        public SubSystemBase GetSubSystemBase(string name, bool recursive = true)
        {
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            {
                var sub = SubSystems[i];
                if (sub.Name == name) return sub;
                else if (recursive)
                {
                    sub = sub.GetSubSystemBase(name, recursive);
                    if (sub != null) return sub;
                }
            }
            return null;
        }

        #endregion Hierarchy
        //-------------------------------------------------------------------------------------------------------------------
        #region Lifecycle
        protected virtual void OnInitFrame() { }
        protected virtual void OnUpdateFrame() { }
        protected virtual void OnFinalizeFrame() { }
        protected virtual void OnDispose() { }

        protected sealed override void OnUpdate()
        {
            if (!isSorted) Sort(true);
            //Initialize------------------------------------------
            OnInitFrame();
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            { SubSystems[i].InitFrame(); }

            //Update---------------------------------------------
            OnUpdateFrame();
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            { SubSystems[i].UpdateFrame(); }

            //Finalize-------------------------------------------
            for (int i = 0, len = SubSystems.Count; i < len; i++)
            { SubSystems[i].FinalizeFrame(); }
            OnFinalizeFrame();
        }

        protected sealed override void OnDestroy()
        {
            for (int i = 0, len = SubSystems.Count; i < len; i++) { SubSystems[i].Destroy(); }
        }

        #endregion Lifecycle
        //-------------------------------------------------------------------------------------------------------------------
        public abstract class SubSystemBase
        {
            public SubSystemBase(string name, int rank = 0)
            {
                this.name = name;
                this.rank = rank;
            }

            public string Name => name;
            protected string name;
            //-------------------------------------------------------------------------------------------------------------------
            #region Sort by Rank
            internal bool isSorted = false;
            public bool IsSorted => isSorted && SubSystems.TrueForAll(sub => sub.IsSorted);
            protected void DelayedSort(bool includedSelf = false)
            {
                isSorted = includedSelf ? false : isSorted;
                if (mGroup != null) mGroup.isSorted = false;
                if (mSystem != null) mSystem.isSorted = false;
            }

            public virtual void Sort(bool recursive = true)
            {
                if (!isSorted) SubSystems.Sort((a, b) => a.rank - b.rank);
                isSorted = true;
                if (recursive)
                {
                    for (int i = 0, len = SubSystems.Count; i < len; i++)
                        SubSystems[i].Sort(recursive);
                }
            }

            internal int rank = 0;
            public int Rank
            {
                get => rank;
                set
                {
                    if (value != rank)
                    {
                        DelayedSort(false);
                        value = rank;
                    }
                }
            }
            #endregion Sort by Rank
            //-------------------------------------------------------------------------------------------------------------------
            #region Hierarchy
            internal CompositeSystemBase mSystem = null;
            internal SubSystemBase mGroup = null;
            internal List<SubSystemBase> SubSystems = new List<SubSystemBase>();

            public bool IsAttachedToSystem => mGroup != null && mSystem != null;

            public T GetSubSystem<T>(bool recursive = true) where T : SubSystemBase
            {
                for (int i = 0, len = SubSystems.Count; i < len; i++)
                {
                    var sub = SubSystems[i];
                    if (sub.Name == name && sub is T) return sub as T;
                    else if (recursive)
                    {
                        T subT = sub.GetSubSystem<T>();
                        if (subT != null) return subT;
                    }
                }
                return null;
            }

            public T GetSubSystem<T>(string name, bool recursive = true) where T : SubSystemBase
            {
                for (int i = 0, len = SubSystems.Count; i < len; i++)
                {
                    var sub = SubSystems[i];
                    if (sub is T && sub.Name == name) return sub as T;
                    else if (recursive)
                    {
                        T subT = sub.GetSubSystem<T>(name, recursive);
                        if (subT != null) return subT;
                    }
                }
                return null;
            }

            public SubSystemBase GetSubSystemBase(string name, bool recursive = true)
            {
                for (int i = 0, len = SubSystems.Count; i < len; i++)
                {
                    var sub = SubSystems[i];
                    if (sub.Name == name) return sub;
                    else if (recursive)
                    {
                        sub = sub.GetSubSystemBase(name, recursive);
                        if (sub != null) return sub;
                    }
                }
                return null;
            }

            public void AddSubSystem(SubSystemBase sub)
            {
                Assert.IsFalse(SubSystems.Contains(sub));
                sub.RemoveFromSystem();
                sub.AddToSystem(mSystem, this);
            }

            public void RemoveFromSystem()
            {
                if (mSystem != null)
                {
                    this.OnDestroy();
                    mSystem.SubSystems.Remove(this);
                }
                if (mGroup != null) mGroup.SubSystems.Remove(this);
                mGroup = null;
                mSystem = null;
            }

            internal void AddToSystem(CompositeSystemBase newSystem, SubSystemBase newGroup = null)
            {
                mSystem = newSystem;
                mGroup = newGroup;
                if (mGroup == null)
                {
                    mSystem.SubSystems.Add(this);
                    mSystem.isSorted = false;
                }
                else
                {
                    mGroup.SubSystems.Add(this);
                    mGroup.DelayedSort(true);
                }
                OnCreate();
            }

            #endregion Hierarchy
            //-------------------------------------------------------------------------------------------------------------------
            #region SystemBase API, So SubSystemBase will work like SystemBase
            //-------------------------------------------------------------------------------------------------------------------
            public JobHandle Dependency
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.Dependency;
                }
                set
                {
                    Assert.IsNotNull(mSystem);
                    mSystem.Dependency = value;
                }
            }
            //-------------------------------------------------------------------------------------------------------------------
            #region World

            public World World
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.World;
                }
            }

            public EntityManager EntityManager
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.EntityManager;
                }
            }

            public uint GlobalSystemVersion
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.GlobalSystemVersion;
                }
            }

            public uint LastSystemVersion
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.LastSystemVersion;
                }
            }

            public ref readonly Unity.Core.TimeData WorldTime
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return ref mSystem.Time;
                }
            }
            #endregion World
            //-------------------------------------------------------------------------------------------------------------------
            #region Lambda 
            //TODO: Lambda Is not working out side of SystemBase

            // public ForEachLambdaJobDescription Entities
            // {
            //     get
            //     {
            //         Assert.IsNotNull(mSystem);
            //         return mSystem.Entities;
            //     }
            // }

            // public LambdaSingleJobDescription Job
            // {
            //     get
            //     {
            //         Assert.IsNotNull(mSystem);
            //         return mSystem.Job;
            //     }
            // }
            #endregion Lambda
            //-------------------------------------------------------------------------------------------------------------------
            #region Entity Query

            public EntityQuery[] EntityQueries
            {
                get
                {
                    Assert.IsNotNull(mSystem);
                    return mSystem.EntityQueries;
                }
            }

            public EntityQuery GetEntityQuery(params ComponentType[] types)
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetEntityQuery(types);
            }

            public EntityQuery GetEntityQuery(params EntityQueryDesc[] descriptions)
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetEntityQuery(descriptions);
            }

            public EntityQuery GetEntityQuery(NativeArray<ComponentType> types)
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetEntityQuery(types);
            }

            public void RequireForUpdate(EntityQuery q)
            {
                Assert.IsNotNull(mSystem);
                mSystem.RequireForUpdate(q);
            }

            #endregion Entity Query
            //-------------------------------------------------------------------------------------------------------------------
            #region Component Access
            public T GetComponent<T>(Entity e) where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetComponent<T>(e);
            }

            public T SetComponent<T>(Entity e) where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetComponent<T>(e);
            }

            public ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(bool isReadonly) where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetComponentDataFromEntity<T>(isReadonly);
            }

            public BufferFromEntity<T> GetBufferFromEntity<T>(bool isReadonly) where T : struct, IBufferElementData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetBufferFromEntity<T>(isReadonly);
            }
            #endregion Component Access
            //-------------------------------------------------------------------------------------------------------------------
            #region Archetype Chunk Types
            public ArchetypeChunkEntityType GetArchetypeChunkEntityType()
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetArchetypeChunkEntityType();
            }

            public ArchetypeChunkComponentType<T> GetArchetypeChunkComponentType<T>(bool isReadonly) where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetArchetypeChunkComponentType<T>(isReadonly);
            }

            public ArchetypeChunkComponentTypeDynamic GetArchetypeChunkComponentTypeDynamic(ComponentType t)
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetArchetypeChunkComponentTypeDynamic(t);
            }

            public ArchetypeChunkBufferType<T> GetArchetypeChunkBufferType<T>(bool isReadonly) where T : struct, IBufferElementData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetArchetypeChunkBufferType<T>(isReadonly);
            }

            public ArchetypeChunkSharedComponentType<T> GetArchetypeChunkSharedComponentType<T>() where T : struct, ISharedComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetArchetypeChunkSharedComponentType<T>();
            }
            #endregion Archetype Chunk Types
            //-------------------------------------------------------------------------------------------------------------------
            #region Singleton
            public void RequireSingletonForUpdate<T>()
            {
                Assert.IsNotNull(mSystem);
                mSystem.RequireSingletonForUpdate<T>();
            }

            public bool HasSingleton<T>() where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.HasSingleton<T>();
            }

            public Entity GetSingletonEntity<T>() where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetSingletonEntity<T>();
            }

            public T GetSingleton<T>() where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                return mSystem.GetSingleton<T>();
            }

            public void SetSingleton<T>(T value) where T : struct, IComponentData
            {
                Assert.IsNotNull(mSystem);
                mSystem.SetSingleton<T>(value);
            }
            #endregion Singleton
            //-------------------------------------------------------------------------------------------------------------------
            #endregion SystemBase API
            //-------------------------------------------------------------------------------------------------------------------
            #region Lifecycle

            protected virtual void OnCreate() { }
            protected virtual bool OnInitFrame() => true;
            protected abstract void OnUpdateFrame();
            protected virtual void OnFinalizeFrame() { }
            protected virtual void OnDestroy() { }

            public bool Enabled = true;
            internal bool frameInitialized = false;
            public void InitFrame()
            {
                if (Enabled)
                {
                    frameInitialized = OnInitFrame();
                    for (int i = 0, len = SubSystems.Count; i < len; i++) { SubSystems[i].InitFrame(); }
                }
            }

            public void UpdateFrame()
            {
                if (Enabled && frameInitialized)
                {
                    OnUpdateFrame();
                    for (int i = 0, len = SubSystems.Count; i < len; i++)
                    {
                        SubSystems[i].UpdateFrame();
                    }
                }
            }

            public void FinalizeFrame()
            {
                if (Enabled && frameInitialized)
                {
                    OnFinalizeFrame();
                    for (int i = 0, len = SubSystems.Count; i < len; i++) { SubSystems[i].FinalizeFrame(); }
                }
            }

            public void Destroy()
            {
                OnDestroy();
                for (int i = 0, len = SubSystems.Count; i < len; i++) { SubSystems[i].Destroy(); }
            }

            #endregion Lifecycle
            //-------------------------------------------------------------------------------------------------------------------
        }
    }

    /*
        public struct TestCounter : IComponentData { public int value; }
        public struct TestRandomValue : IComponentData { public int value; }
        public struct TestBaseValue : IComponentData { public int value; }
        public class TestCompositeSystem : CompositeSystemBase
        {
            Unity.Mathematics.Random rand;
            protected override void OnCreate()
            {
                AddSubSystem(new TestSubSystem("SubTest", 0));
                var e = EntityManager.CreateEntity();
                EntityManager.AddComponentData(e, new TestBaseValue() { value = 1 });
                rand = new Unity.Mathematics.Random(31);

            }
            protected override void OnUpdateFrame()
            {
            }

            public void SomeJob()
            {
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.L))
                {
                    var sub = GetSubSystemBase("SubTest");
                    if (sub == null)
                    {
                        AddSubSystem(new TestSubSystem("SubTest", 0));
                    }
                    else
                    {
                        sub.RemoveFromSystem();
                    }
                }
                var seed = rand.NextUInt();
                Entities.ForEach((int entityInQueryIndex, Entity e, ref TestBaseValue v) =>
                {
                    v.value = new Unity.Mathematics.Random(seed >> entityInQueryIndex).NextInt(10);
                }).ScheduleParallel();

            }

            public static void FFF(int entityInQueryIndex, Entity e, ref TestBaseValue v)
            {
                v.value = new Unity.Mathematics.Random((uint)entityInQueryIndex).NextInt(10);
            }

            public class TestSubSystem : CompositeSystemBase.SubSystemBase
            {
                public TestSubSystem(string name, int rank = 0) : base(name, rank) { }

                Entity counter;
                EntityQuery withRandQuery;
                Unity.Mathematics.Random r;
                NativeArray<int> values;
                protected override void OnCreate()
                {
                    counter = EntityManager.CreateEntity();
                    EntityManager.SetName(counter, "Singleton Counter");
                    EntityManager.AddComponentData(counter, new TestCounter() { value = 0 });

                    var rand = EntityManager.CreateEntity();
                    EntityManager.SetName(rand, "Named Rand");
                    EntityManager.AddComponentData(rand, new TestRandomValue() { value = 0 });

                    EntityManager.Instantiate(rand);
                    EntityManager.Instantiate(rand);
                    EntityManager.Instantiate(rand);

                    withRandQuery = GetEntityQuery(typeof(TestRandomValue));
                    r = new Unity.Mathematics.Random(1);
                }

                protected override bool OnInitFrame()
                {
                    var ctr = GetSingleton<TestCounter>();
                    ctr.value++;
                    SetSingleton(ctr);
                    values = new NativeArray<int>(withRandQuery.CalculateEntityCount(), Allocator.TempJob);
                    for (int i = 0, len = values.Length; i < len; i++) values[i] = r.NextInt(11);
                    Dependency = new RendGenJob() { values = values, seed = r.NextUInt() }.Schedule(values.Length, 2, Dependency);
                    Dependency.Complete();

                    var sb = new System.Text.StringBuilder("|");
                    for (int i = 0, len = values.Length; i < len; i++)
                    { sb.Append(values[i].ToString() + "|"); }
                    UnityEngine.Debug.Log(sb.ToString());

                    return true;
                }

                struct RendGenJob : IJobParallelFor
                {
                    public NativeArray<int> values;
                    public uint seed;
                    public void Execute(int i) { values[i] = new Unity.Mathematics.Random((uint)(seed + i * 575)).NextInt(11); }
                }

                protected override void OnUpdateFrame()
                {
                    (mSystem as TestCompositeSystem).SomeJob();
                    Dependency = new ApplyValueJob()
                    {
                        vtp = GetArchetypeChunkComponentType<TestRandomValue>(false),
                        rands = values,
                    }.ScheduleParallel(withRandQuery, Dependency);
                }

                struct ApplyValueJob : IJobChunk
                {
                    public ArchetypeChunkComponentType<TestRandomValue> vtp;
                    public NativeArray<int> rands;
                    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
                    {
                        var chunkV = chunk.GetNativeArray(vtp);
                        for (int i = 0, len = chunk.Count; i < len; i++)
                        {
                            chunkV[i] = new TestRandomValue() { value = rands[firstEntityIndex + i] };
                        }
                    }
                }

                protected override void OnFinalizeFrame()
                {
                    values.Dispose(Dependency);
                }

                protected override void OnDestroy()
                {
                    EntityManager.DestroyEntity(counter);
                    EntityManager.DestroyEntity(withRandQuery);
                }
            }
        }
        */


}