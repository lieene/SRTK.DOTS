using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Burst;
using SRTK;
using System.Runtime.InteropServices;

namespace Tests
{
    public class EventSystemTestTest
    {
        World m_PreviousWorld;
        World mWorld;
        EntityManager mEntityManager;
        SimulationSystemGroup mSimGroup;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            mWorld = World.DefaultGameObjectInjectionWorld = new World("NewClassWorld");
            mSimGroup = mWorld.CreateSystem<SimulationSystemGroup>();
            mEntityManager = mWorld.EntityManager;
        }

        [TearDown]
        public virtual void TearDown()
        {
            World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
            mWorld.Dispose();
        }

        public T AddToSimGroup<T>() where T : ComponentSystemBase
        {
            T newSystem = mWorld.CreateSystem<T>();
            mSimGroup.AddSystemToUpdateList(newSystem);
            return newSystem;
        }

        [Test]
        public void BasicTest()
        {
            var logSys = AddToSimGroup<LogEventSystem>();
            var popSys = AddToSimGroup<PopEventSystem>();
            mWorld.Update();
            logSys.WaiteFroStreamAccess.Complete();
            logSys.WaiteFroEventProcess.Complete();
        }

        //-------------------------------------------------------------------------------------------------------------------
        #region Test
        [UpdateBefore(typeof(LogEventSystem))]
        public class PopEventSystem : SystemBase
        {
            public LogEventSystem eventSystem;
            protected override void OnCreate()
            {
                eventSystem = World.GetOrCreateSystem<LogEventSystem>();
                eventSystem.TypeRegistry.RegisterEventType(10).RegisterNextDataType<int>();
                eventSystem.TypeRegistry.RegisterEventType(11).RegisterNextDataType<Entity>().RegisterNextDataType<long>();
            }
            protected override void OnUpdate()
            {
                Debug.Log("PopEventSystem.OnUpdate");
                var w = eventSystem.GetStreamWriter();
                eventSystem.WaiteFroStreamAccess = Job.WithName("PopEvents").WithCode(() =>
                {
                    JobLogger.Log("Poping Events to Streamer");

                    var handle = w.BeginBatch();
                    handle.WriteEvent(10, 10);
                    handle.WriteEvent(11, new Entity() { Index = 10, Version = 5 }, (long)3);
                    handle.EndBatch();
                }).Schedule(eventSystem.WaiteFroStreamAccess);
            }
        }
        
        public class LogEventSystem : EventSystemBase
        {
            protected override void OnProcessEvents()
            {
                Debug.Log("LogEventSystem.OnProcessEvents");
                if (!mEvents.IsCreated) return;
                var _evts = mEvents;
                var types = TypeRegistry;
                WaiteFroEventProcess = Job.WithName("LogEvents").WithoutBurst().WithCode(() =>
                {
                    for (int i = 0, len = _evts.Length; i < len; i++)
                    {
                        var e = _evts[i];
                        var info = types.GetTypeInfo(e.TypeID);
                        if (e.TypeID == 10)
                        {
                            Debug.Log($"{i} Event[T:{e.TypeID} L:{e.SizeInfo.LocalDataByteSize} X:{e.SizeInfo.ExternalDataByteSize} P:{e.SizeInfo.PackageByteSize}] LD:{ info.Data<int>(0, e)} ");
                            Assert.AreEqual(10, info.Data<int>(0, e));
                        }
                        else if (e.TypeID == 11)
                        {
                            Debug.Log($"{i} Event[T:{e.TypeID} L:{e.SizeInfo.LocalDataByteSize} X:{e.SizeInfo.ExternalDataByteSize} P:{e.SizeInfo.PackageByteSize}] D0:[Entity:{info.Data<Entity>(0, e).Index}|{info.Data<Entity>(0, e).Version}] D1[HP:{info.Data<long>(1, e)}]");
                            Assert.AreEqual(10, info.Data<Entity>(0, e).Index);
                            Assert.AreEqual(5, info.Data<Entity>(0, e).Version);
                            Assert.AreEqual(3, info.Data<long>(1, e));
                        }
                    }
                }).Schedule(WaiteFroEventProcess);
            }
        }
        #endregion Test
        //-------------------------------------------------------------------------------------------------------------------
    }
}