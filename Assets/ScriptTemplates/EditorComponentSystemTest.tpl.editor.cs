//{"NewClassSystem":"the Test class","NewTest":"the Test Function","NewSystem":"the test System"}

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
    public class NewClassSystemTest
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
        public void NewTest()
        {
            AddToSimGroup<NewSystem>();
            mWorld.Update();
        }

        public class NewSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                Debug.Log("Test System Update");
            }
        }
    }
}