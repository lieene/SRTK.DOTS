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
    public class NativeDelegateTest
    {
        World m_PreviousWorld;
        World mWorld;
        EntityManager mEntityManager;
        SimulationSystemGroup mSimGroup;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            mWorld = World.DefaultGameObjectInjectionWorld = new World("NativeDelegateTestWorld");
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


        //----------------------------------------------------------------------------------------------------------
        #region Delegate Types
        public delegate T TFunc<T>(T a, T b) where T : unmanaged;

        public delegate int IntFunc(int input);

        [BurstCompile]
        public struct StaticFunctions
        {
            [BurstCompile] public static int Times2(int input) => input * 2;
            [BurstCompile] public static int Times3(int input) => input * 3;
        }

        public struct StructFunction
        {
            public int value;
            public int AddTo(int value) => this.value + value;
        }

        public class ClassFunction
        {
            public ClassFunction(int times) => this.times = times;
            public int times;
            public int Times(int value) => value * times;
        }

        public interface ITimes<T>
        {
            T Mul(T value);
            void FromInt(int value);
        }

        public struct IntWithMul : ITimes<IntWithMul>
        {
            public int value;

            public void FromInt(int value) => this.value = value;

            public IntWithMul Mul(IntWithMul value) => this.value * value;
            public static implicit operator int(IntWithMul v) => v.value;
            public static implicit operator IntWithMul(int v) => new IntWithMul() { value = v };
        }

        public class ClassGenericFunction
        {
            public T Times<T>(T a, T b) where T : ITimes<T> => a.Mul(b);
        }

        #endregion Delegate Types
        //----------------------------------------------------------------------------------------------------------
        #region In LambdaJob of SystemBase
        [Test]
        public void InLambdaJob_SystemBase()
        {
            AddToSimGroup<CallNativeDelegateSystem>();
            mWorld.Update();
        }
        
        public class CallNativeDelegateSystem : SystemBase
        {
            public NativeDelegate<IntFunc> mStaticDelegate = default;
            public NativeDelegate<IntFunc> mStructDelegate = default;
            public NativeDelegate<IntFunc> mClassDelegate = default;

            protected override void OnCreate()
            {
                mStaticDelegate = NativeDelegate<IntFunc>.Compile(StaticFunctions.Times2);
                mStructDelegate = NativeDelegate<IntFunc>.Compile(new StructFunction() { value = 2 }.AddTo);
                mClassDelegate = NativeDelegate<IntFunc>.Compile(new ClassFunction(4).Times);
            }
            protected override void OnUpdate()
            {
                NativeRef<int> intRef = new NativeRef<int>(Allocator.TempJob);

                Debug.Log($"[StaticDelegate] compile:({mStaticDelegate.IsCompiled})");
                var staticDelegate = mStaticDelegate;
                Job.WithCode(() =>
                {
                    JobLogger.Log("Static Delegate: 2 * 3 = ?");
                    intRef.Value = staticDelegate.AsFunctionPointer().Invoke(3);
                    JobLogger.Log("Static Delegate: result is ", intRef.Value);
                }).Schedule(Dependency).Complete();
                Assert.AreEqual(6, intRef.Value);

                Debug.Log($"[StructDelegate] compile:({mStructDelegate.IsCompiled})");
                var structDelegate = mStructDelegate;
                Job.WithCode(() =>
                {
                    JobLogger.Log("Static Delegate: 2 + 3 = ?");
                    intRef.Value = structDelegate.AsFunctionPointer().Invoke(3);
                    JobLogger.Log("Static Delegate: result is ", intRef.Value);
                }).Schedule(Dependency).Complete();
                Assert.AreEqual(5, intRef.Value);

                Debug.Log($"[ClassDelegate] compile:({mClassDelegate.IsCompiled})");
                var classDelegate = mClassDelegate;
                Job.WithCode(() =>
                {
                    JobLogger.Log("Static Delegate: 4 * 3 = ?");
                    intRef.Value = classDelegate.AsFunctionPointer().Invoke(3);
                    JobLogger.Log("Static Delegate: result is ", intRef.Value);
                }).Schedule(Dependency).Complete();
                Assert.AreEqual(12, intRef.Value);


                intRef.Dispose();
            }
        }
        #endregion In LambdaJob of SystemBase

        //----------------------------------------------------------------------------------------------------------
        #region InIJobParallelFor

        [Test]
        public void InIJobParallelFor()
        {
            //static ---------------------------------------------------------
            //static type-less
            RunTypeLessDelegate(1, StaticFunctions.Times2);
            RunTypeLessDelegate(1, StaticFunctions.Times3);

            //static typed
            RunDelegate(1, StaticFunctions.Times2);
            RunDelegate(1, StaticFunctions.Times3);

            //struct ---------------------------------------------------------
            var struct2 = new StructFunction() { value = 2 };
            var struct3 = new StructFunction() { value = 3 };
            //struct type-less
            RunTypeLessDelegate(1, struct2.AddTo);
            RunTypeLessDelegate(1, struct2.AddTo);

            //struct typed
            RunDelegate(1, struct2.AddTo);
            RunDelegate(1, struct2.AddTo);

            //class ---------------------------------------------------------
            var cf2 = new ClassFunction(2);
            var cf3 = new ClassFunction(3);
            //class type-less
            RunTypeLessDelegate(1, cf2.Times);
            RunTypeLessDelegate(1, cf3.Times);

            //class typed
            RunDelegate(1, cf2.Times);
            RunDelegate(1, cf3.Times);

            //lambda ---------------------------------------------------------
            //lambda type-less
            RunTypeLessDelegate(1, v => v + 20);
            RunTypeLessDelegate(1, v => v + 30);

            //lambda typed
            RunDelegate(1, v => v + 20);
            RunDelegate(1, v => v + 30);

            //class ---------------------------------------------------------
            //var cfg1 = new ClassGenericFunction();
            // var cfg2 = new ClassGenericFunction();
            // TFunc<IntWithMul> d1 = cfg1.Times;
            // TFunc<IntWithMul> d2 = cfg2.Times;
            // //class type-less
            // RunTypeLessGenericDelegate<IntWithMul>(1, d1);
            // RunTypeLessGenericDelegate<IntWithMul>(1, d2);
        }

        void RunTypeLessDelegate(int iteration, IntFunc action)
        {
            var ntd = NativeTypeLessDelegate.Compile<IntFunc>(action);
            Debug.Log($"[Typeless Delegate] static:{action.Method.IsStatic} generic:{action.Method.IsGenericMethod} compile:({ntd.IsCompiled})");
            var values = new NativeArray<int>(iteration, Allocator.TempJob);
            for (int i = 0, len = values.Length; i < len; i++) values[i] = i;
            var mJob = new RunDelegateJob() { values = values, ntd = ntd, }.Schedule(iteration, 4, default);
            mJob.Complete();
            Debug.Log(values.Join());
            values.Dispose();
            for (int i = 0, len = values.Length; i < len; i++) Assert.AreEqual(action(i), values[i]);
        }

        void RunTypeLessGenericDelegate<T>(int iteration, TFunc<T> action) where T : unmanaged, ITimes<T>
        {
            var ntd = NativeTypeLessDelegate.Compile<TFunc<T>>(action);
            Debug.Log($"[Typeless Delegate] static:{action.Method.IsStatic} generic:{action.Method.IsGenericMethod} compile:({ntd.IsCompiled})");
            var values = new NativeArray<T>(iteration, Allocator.TempJob);
            for (int i = 0, len = values.Length; i < len; i++)
            {
                T val = default; val.FromInt(i);
                values[i] = val;
            }
            var mJob = new RunGenericDelegateJob<T>() { values = values, ntd = ntd, }.Schedule(iteration, 4, default);
            mJob.Complete();
            Debug.Log(values.Join());
            values.Dispose();
            for (int i = 0, len = values.Length; i < len; i++)
            {
                T val = default; val.FromInt(i);
                Assert.AreEqual(action(val, val), values[i]);
            }
        }

        void RunDelegate(int iteration, IntFunc action)
        {
            var nd = NativeDelegate<IntFunc>.Compile(action);
            Debug.Log($"[Typed Delegate] static:{action.Method.IsStatic} generic:{action.Method.IsGenericMethod} Compile:({nd.IsCompiled})");
            var values = new NativeArray<int>(iteration, Allocator.TempJob);
            for (int i = 0, len = values.Length; i < len; i++) values[i] = i;
            var mJob = new RunTypedDelegateJob() { values = values, nd = nd, }.Schedule(iteration, 4, default);
            mJob.Complete();
            Debug.Log(values.Join());
            values.Dispose();
            for (int i = 0, len = values.Length; i < len; i++) Assert.AreEqual(action(i), values[i]);
        }


        [BurstCompile]
        struct RunDelegateJob : IJobParallelFor
        {
            public NativeArray<int> values;
            public NativeTypeLessDelegate ntd;
            public void Execute(int index)
            {
                //var input = values[index];
                //var call = ntd.GetInvoker<IntFunc>();
                //var outPut = call(input);
                values[index] = ntd.AsFunctionPointer<IntFunc>().Invoke(values[index]);
                //values[index] = ntd.GetInvoker<IntFunc>().Invoke(values[index]);
            }
        }
        [BurstCompile]
        struct RunGenericDelegateJob<T> : IJobParallelFor where T : unmanaged, ITimes<T>
        {
            public NativeArray<T> values;
            public NativeTypeLessDelegate ntd;
            public void Execute(int index)
            {
                //var input = values[index];
                //var call = ntd.GetInvoker<IntFunc>();
                //var outPut = call(input);
                values[index] = ntd.AsFunctionPointer<TFunc<T>>().Invoke(values[index], values[index]);
                //values[index] = ntd.GetInvoker<IntFunc>().Invoke(values[index]);
            }
        }

        [BurstCompile]
        struct RunTypedDelegateJob : IJobParallelFor
        {
            public NativeArray<int> values;
            public NativeDelegate<IntFunc> nd;
            public void Execute(int index)
            {
                //var input = values[index];
                //var call = ntd.GetInvoker<IntFunc>();
                //var outPut = call(input);
                values[index] = nd.AsFunctionPointer().Invoke(values[index]);
                //values[index] = ntd.GetInvoker<IntFunc>().Invoke(values[index]);
            }
        }
        #endregion InIJobParallelFor
        //----------------------------------------------------------------------------------------------------------

    }
}