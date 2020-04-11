/************************************************************************************
| File: TimeEditor.cs                                                               |
| Project: lieene.TimeSystem                                                        |
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
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SRTK
{
    public struct TimeEditor
    {
        internal EntityCommandBufferSystem ecbs;
        internal EntityCommandBuffer ecb;
        public EntityCommandBuffer ECB => ecb;
        internal EntityQueryMask hasLocaltimeScale;
        internal EntityQueryMask hasTimeScale;

        internal TimeEditor(EntityCommandBufferSystem ecbs)
        {
            this.ecbs = ecbs;
            this.ecb = ecbs.CreateCommandBuffer();
            var em = ecbs.EntityManager;
            hasLocaltimeScale = em.GetEntityQueryMask(em.CreateEntityQuery(typeof(LocalTimeScale)));
            hasTimeScale = em.GetEntityQueryMask(em.CreateEntityQuery(typeof(TimeScale)));
        }

        public void ClearParentNow(in Entity child)
        {
            var em = ecbs.EntityManager;
            if (child.Index < 0 || !em.Exists(child))
            {
                Debug.LogAssertion($"child({child.Index}) is not valid!");
                return;
            }
            if (!em.HasComponent<TimeScale>(child))
            {
                Debug.LogAssertion($"child({child.Index}) dose not has TimeScale component!");
                return;
            }

            if (em.HasComponent<ParentTime>(child))
            {//remove child from previous parent's children
                var parent = em.GetComponentData<ParentTime>(child).Value;
                var children = em.GetBuffer<ChildTime>(parent);
                var idx = children.FindFirstElement(new ChildTime() { Value = child });

                Assert.IsTrue(idx >= 0);//Failed: Child Time not found in parent's Children buffer

                children.RemoveAt(idx);
                em.RemoveComponent<ParentTime>(child);

                //if (!keepTimeScale)
                //{
                //    if (em.HasComponent<LocalTimeScale>(child))
                //    {
                //        var lts = em.GetComponentData<LocalTimeScale>(child);
                //        em.SetComponentData(child, (TimeScale)lts);
                //    }
                //    else em.SetComponentData(child, TimeScale.Default);
                //}
            }
            else Debug.LogAssertion($"child({child.Index}) dose not has Parent,no need to clear parent!");
        }

        public void SetParentNow(in Entity child, in Entity newParent)
        {
            var em = ecbs.EntityManager;
            if (child == newParent)
            {
                Debug.LogAssertion($"child and parent are the same entity ({child.Index})!");
                return;
            }
            if (child.Index < 0 || !em.Exists(child))
            {
                Debug.LogAssertion($"child({child.Index}) is not valid!");
                return;
            }
            if (newParent.Index < 0 || !em.Exists(newParent))
            {
                Debug.LogAssertion($"parent({newParent.Index}) is not valid!");
                return;
            }
            if (!em.HasComponent<TimeScale>(child))
            {
                Debug.LogAssertion($"child({child.Index}) dose not has TimeScale component!");
                return;
            }

            if (!em.HasComponent<TimeScale>(newParent))
            {
                Debug.LogAssertion($"parent({newParent.Index}) dose not has TimeScale component!");
                return;
            }

            if (em.HasComponent<ParentTime>(child) && (em.GetComponentData<ParentTime>(child).Value == newParent))
            {
                Debug.LogAssertion($"child({child.Index}) is already parent({newParent.Index}) dose not need to change!");
                return;
            }

            var up2Root = newParent;
            while (em.HasComponent<ParentTime>(up2Root))
            {
                up2Root = em.GetComponentData<ParentTime>(up2Root).Value;
                if (up2Root == child)
                {
                    Debug.LogAssertion($"Loop Reference! child({child.Index}) is deep parent of parent({newParent.Index})!");
                    return;
                }
            }

            if (em.HasComponent<ParentTime>(child))
            {//remove child from previous parent's children
                var prev_parent = em.GetComponentData<ParentTime>(child).Value;
                var prev_children = em.GetBuffer<ChildTime>(prev_parent);
                var prev_idx = prev_children.FindFirstElement(new ChildTime() { Value = child });
                Assert.IsTrue(prev_idx >= 0); //Failed: Child Time not found in parent's Children buffer;
                prev_children.RemoveAt(prev_idx);
            }
            else em.AddComponent<ParentTime>(child);

            //if (keepTimeScale)
            //{
            //    var timeScale = em.GetComponentData<TimeScale>(child).value;
            //    var newParentTimeScale = em.GetComponentData<TimeScale>(newParent).value;
            //    timeScale = newParentTimeScale == 0 ? timeScale : timeScale / newParentTimeScale;
            //    if (em.HasComponent<LocalTimeScale>(child)) em.SetComponentData(child, (LocalTimeScale)timeScale);
            //    else em.AddComponentData(child, (LocalTimeScale)timeScale);
            //}
            //else keep localTimeScale
            //if (!em.HasComponent<LocalTimeScale>(child)) em.AddComponentData(child, LocalTimeScale.Default);

            DynamicBuffer<ChildTime> children;
            if (em.HasComponent<ChildTime>(newParent)) children = em.GetBuffer<ChildTime>(newParent);
            else children = em.AddBuffer<ChildTime>(newParent);

            Assert.IsTrue(children.FindFirstElement(new ChildTime() { Value = child }) < 0);//Parent has duplicated children

            //apply change------------------------------------------------------------
            children.Add(new ChildTime() { Value = child });
            em.SetComponentData(child, new ParentTime() { Value = newParent });
        }

        public void ClearParent(in Entity child)
        {
            var em = ecbs.EntityManager;
            if (child.Index >= 0 && !em.Exists(child))
            {
                Debug.LogAssertion($"child({child.Index}) is not valid!");
                return;
            }
            if (!em.HasComponent<TimeScale>(child))
            {
                Debug.LogAssertion($"child({child.Index}) dose not has TimeScale component!");
                return;
            }
            if (em.HasComponent<ParentTime>(child))
            {
                ecb.RemoveComponent<ParentTime>(child);
            }
            else Debug.LogAssertion($"child({child.Index}) dose not has Parent,no need to clear parent!");
        }

        public void SetParent(in Entity child, in Entity newParent)
        {
            var em = ecbs.EntityManager;
            if (child == newParent)
            {
                Debug.LogAssertion($"child and parent are the same entity ({child.Index})!");
                return;
            }
            bool childNotDefer = child.Index >= 0;

            if (childNotDefer && !em.Exists(child))
            {
                Debug.LogAssertion($"child({child.Index}) is not valid!");
                return;
            }
            bool parentNotDefer = newParent.Index >= 0;
            if (parentNotDefer && !em.Exists(newParent))
            {
                Debug.LogAssertion($"parent({newParent.Index}) is not valid!");
                return;
            }
            if (childNotDefer && em.HasComponent<ParentTime>(child) && (em.GetComponentData<ParentTime>(child).Value == newParent))
            {
                Debug.LogAssertion($"child({child.Index}) is already parent({newParent.Index}) dose not need to change!");
                return;
            }

            if (childNotDefer && parentNotDefer)//both exists
            {// loop check
                var up2Root = newParent;
                while (em.HasComponent<ParentTime>(up2Root))
                {
                    up2Root = em.GetComponentData<ParentTime>(up2Root).Value;
                    if (up2Root == child)
                    {
                        Debug.LogAssertion($"Loop Reference! child({child.Index}) is deep parent of parent({newParent.Index})!");
                        return;
                    }
                }
                if (em.HasComponent<ParentTime>(child))
                {
                    em.SetComponentData(child, new ParentTime() { Value = newParent });
                }
            }
            //When child or parent or both is created by ECB this loop check will not happen
            //in this case loop can only be checked in TimeHierarchySystem where this checking could be too costly
            //It would be the user's job to keep not doing so as this kind of loop would be done in one place and easy to notice
            else ecb.AddComponent(child, new ParentTime { Value = newParent });

        }

        public TimeEdit EditTime(in Entity target) => TimeEdit.BeginNew.WithEntity(target);
        public TimeEdit CreateTime() => TimeEdit.BeginNew;

        public Concurrent AsConcurrent(ComponentSystemBase usrSystem) => new Concurrent(ecb, usrSystem, hasLocaltimeScale, hasTimeScale);

        public struct Concurrent
        {
            internal EntityCommandBuffer.Concurrent ecbc;
            internal ComponentDataFromEntity<ParentTime> parentAccess;
            internal EntityQueryMask hasLocaltimeScale;
            internal EntityQueryMask hasTimeScale;

            internal Concurrent(EntityCommandBuffer ecb, ComponentSystemBase componentSystem, EntityQueryMask hasLocaltimeScale, EntityQueryMask hasTimeScale)
            {
                parentAccess = componentSystem.GetComponentDataFromEntity<ParentTime>(false);
                this.hasLocaltimeScale = hasLocaltimeScale;
                this.hasTimeScale = hasTimeScale;
                ecbc = ecb.ToConcurrent();
            }

            public void ClearParent(int jobIndex, in Entity child)
            {
                if (parentAccess.HasComponent(child))
                {
                    ecbc.RemoveComponent<ParentTime>(jobIndex++, child);
                }
                else Debug.LogAssertion($"child({child.Index}) dose not has Parent,no need to clear parent!");
            }

            public void SetParent(int jobIndex, in Entity child, in Entity newParent)
            {
                if (child == newParent)
                {
                    Debug.LogAssertion($"child and parent are the same entity ({child.Index})!");
                    return;
                }

                bool childNotDefer = child.Index >= 0;
                bool parentNotDefer = newParent.Index >= 0;

                if (childNotDefer && parentAccess.HasComponent(child) && (parentAccess[child].Value == newParent))
                {
                    Debug.LogAssertion($"child({child.Index}) is already parent({newParent.Index}) dose not need to change!");
                    return;
                }


                if (childNotDefer && parentNotDefer)//both exists
                {// loop check
                    var up2Root = newParent;
                    while (parentAccess.HasComponent(up2Root))
                    {
                        up2Root = parentAccess[up2Root].Value;
                        if (up2Root == child)
                        {
                            Debug.LogAssertion($"Loop Reference! child({child.Index}) is deep parent of parent({newParent.Index})!");
                            return;
                        }
                    }

                    if (parentAccess.HasComponent(child))
                    {
                        parentAccess[child] = new ParentTime() {Value = newParent};
                    }
                }
                //When child or parent or both is created by ECB this loop check will not happen
                //in this case loop can only be checked in TimeHierarchySystem where this checking could be too costly
                //It would be the user's job to keep not doing so as this kind of loop would be done in one place and easy to notice
                else ecbc.AddComponent(jobIndex++, child, new Parent { Value = newParent });

            }

            public TimeEdit EditTime(in Entity target) => TimeEdit.BeginNew.WithEntity(target);
            public TimeEdit CreateTime() => TimeEdit.BeginNew;
        }
    }

    public struct TimeEdit
    {
        public static readonly TimeEdit BeginNew = new TimeEdit()
        {
            TimeScaleEditMode = ComponentCommandMode.NoEdit,
            DeltaTimeEditMode = ComponentCommandMode.NoEdit,
            ElapsedTimeEditMode = ComponentCommandMode.NoEdit,
            FixedTimeStepEditMode = ComponentCommandMode.NoEdit,
            FrameCountEditMode = ComponentCommandMode.NoEdit,
            StepCountEditMode = ComponentCommandMode.NoEdit,

            timeScale = 1,
            time = 1,
            timeStep = new FixedTimeStep(0.02f),
            frameCounter = new FrameCounter(0),
            stepCounter = new StepCounter(0),
            target = Entity.Null,
        };

        public Entity target;
        public ComponentCommandMode TimeScaleEditMode;
        public ComponentCommandMode DeltaTimeEditMode;
        public ComponentCommandMode ElapsedTimeEditMode;
        public ComponentCommandMode FixedTimeStepEditMode;
        public ComponentCommandMode FrameCountEditMode;
        public ComponentCommandMode StepCountEditMode;
        public TimeScale timeScale;
        public LocalTimeScale localTimeScale;
        public ElapsedTime time;
        public FixedTimeStep timeStep;
        public FrameCounter frameCounter;
        public StepCounter stepCounter;

        public TimeEdit WithNewEntity() { target = Entity.Null; return this; }
        public TimeEdit WithEntity(Entity target) { this.target = target; return this; }

        public TimeEdit SetTimeScale(float scale, bool keepTimeScale = false)
        {
            TimeScaleEditMode = ComponentCommandMode.Set;
            timeScale.value = scale;
            timeScale.KeepTimeScaleOnParentChange = keepTimeScale;
            return this;
        }
        public TimeEdit AddTimeScale() { TimeScaleEditMode = (TimeScaleEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : TimeScaleEditMode; return this; }
        public TimeEdit DontEditTimeScale() { TimeScaleEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveTimeScale() { TimeScaleEditMode = ComponentCommandMode.Remove; return this; }

        //public TimeEdit SetDeltaTime(float v) { DeltaTimeEditMode = ComponentCommandMode.Set; return this; }
        public TimeEdit AddDeltaTime() { DeltaTimeEditMode = (DeltaTimeEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : DeltaTimeEditMode; return this; }
        public TimeEdit DontEditDeltaTime() { DeltaTimeEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveDeltaTime() { DeltaTimeEditMode = ComponentCommandMode.Remove; return this; }

        public TimeEdit SetElapsedTime(float time) { ElapsedTimeEditMode = ComponentCommandMode.Set; this.time.value = time; return this; }
        public TimeEdit AddElapsedTime() { ElapsedTimeEditMode = (ElapsedTimeEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : ElapsedTimeEditMode; return this; }
        public TimeEdit DontEditElapsedTime() { ElapsedTimeEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveElapsedTime() { ElapsedTimeEditMode = ComponentCommandMode.Remove; return this; }

        public TimeEdit SetFixedTimeStep(float stepInternval) { FixedTimeStepEditMode = ComponentCommandMode.Set; timeStep.StepTime = stepInternval; return this; }
        public TimeEdit SetTimer(float timespan) { FixedTimeStepEditMode = ComponentCommandMode.Set; timeStep = FixedTimeStep.Timer(timespan); return this; }
        public TimeEdit SetProduceOverTime(float timePreProduct, int initialProduct = 0, int storageCap = 0) { FixedTimeStepEditMode = ComponentCommandMode.Set; timeStep = FixedTimeStep.Producer(timePreProduct, initialProduct, storageCap); return this; }

        public TimeEdit AddFixedTimeStep() { FixedTimeStepEditMode = (FixedTimeStepEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : FixedTimeStepEditMode; return this; }
        public TimeEdit DontEditFixedTimeStep() { FixedTimeStepEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveFixedTimeStep() { FixedTimeStepEditMode = ComponentCommandMode.Remove; return this; }

        public TimeEdit SetFrameCount(int count) { FrameCountEditMode = ComponentCommandMode.Set; frameCounter.Count = count; return this; }
        public TimeEdit AddFrameCount() { FrameCountEditMode = (FrameCountEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : FrameCountEditMode; return this; }
        public TimeEdit DontEditFrameCount() { FrameCountEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveFrameCount() { FrameCountEditMode = ComponentCommandMode.Remove; return this; }

        public TimeEdit SetStepCount(int count) { StepCountEditMode = ComponentCommandMode.Set; stepCounter.Count = count; return this; }
        public TimeEdit AddStepCount() { StepCountEditMode = (StepCountEditMode <= ComponentCommandMode.Remove) ? ComponentCommandMode.Create : StepCountEditMode; return this; }
        public TimeEdit DontEditStepCount() { StepCountEditMode = ComponentCommandMode.NoEdit; return this; }
        public TimeEdit RemoveStepCount() { StepCountEditMode = ComponentCommandMode.Remove; return this; }

        //public void Create

        public Entity ApplyNow(EntityManager em)
        {
            var applyTarget = this.target;
            if (applyTarget == Entity.Null) applyTarget = em.CreateEntity();
            EditByMode(FrameCountEditMode, applyTarget, frameCounter, FrameCounter.Zero, em);
            EditByMode(TimeScaleEditMode, applyTarget, timeScale, TimeScale.Default, em);
            EditByMode(TimeScaleEditMode, applyTarget, (LocalTimeScale)timeScale, LocalTimeScale.Default, em);
            EditByMode(DeltaTimeEditMode, applyTarget, DeltaTime.Zero, DeltaTime.Zero, em);
            EditByMode(ElapsedTimeEditMode, applyTarget, time, ElapsedTime.Zero, em);
            EditByMode(FixedTimeStepEditMode, applyTarget, timeStep, FixedTimeStep.Default, em);
            EditByMode(StepCountEditMode, applyTarget, stepCounter, StepCounter.Zero, em);
            return applyTarget;
        }

        public Entity ApplyBuffered(TimeEditor te) => ApplyBuffered(te.ecb);

        public Entity ApplyBuffered(EntityCommandBuffer ecb)
        {
            var applyTarget = this.target;
            if (applyTarget == Entity.Null) applyTarget = ecb.CreateEntity();
            EditByMode(FrameCountEditMode, applyTarget, frameCounter, FrameCounter.Zero, ecb);
            EditByMode(TimeScaleEditMode, applyTarget, timeScale, TimeScale.Default, ecb);
            EditByMode(TimeScaleEditMode, applyTarget, (LocalTimeScale)timeScale, LocalTimeScale.Default, ecb);
            EditByMode(DeltaTimeEditMode, applyTarget, DeltaTime.Zero, DeltaTime.Zero, ecb);
            EditByMode(ElapsedTimeEditMode, applyTarget, time, ElapsedTime.Zero, ecb);
            EditByMode(FixedTimeStepEditMode, applyTarget, timeStep, FixedTimeStep.Default, ecb);
            EditByMode(StepCountEditMode, applyTarget, stepCounter, StepCounter.Zero, ecb);
            return applyTarget;
        }

        public Entity ApplyConcurrent(int jobIndex, TimeEditor.Concurrent tec) => ApplyConcurrent(jobIndex, tec.ecbc);

        public Entity ApplyConcurrent(int jobIndex, EntityCommandBuffer.Concurrent ecbc)
        {
            var applyTarget = this.target;
            if (applyTarget == Entity.Null) applyTarget = ecbc.CreateEntity(jobIndex++);
            EditByMode(FrameCountEditMode, applyTarget, frameCounter, FrameCounter.Zero, ecbc, ref jobIndex);
            EditByMode(TimeScaleEditMode, applyTarget, timeScale, TimeScale.Default, ecbc, ref jobIndex);
            EditByMode(TimeScaleEditMode, applyTarget, (LocalTimeScale)timeScale, LocalTimeScale.Default, ecbc, ref jobIndex);
            EditByMode(DeltaTimeEditMode, applyTarget, DeltaTime.Zero, DeltaTime.Zero, ecbc, ref jobIndex);
            EditByMode(ElapsedTimeEditMode, applyTarget, time, ElapsedTime.Zero, ecbc, ref jobIndex);
            EditByMode(FixedTimeStepEditMode, applyTarget, timeStep, FixedTimeStep.Default, ecbc, ref jobIndex);
            EditByMode(StepCountEditMode, applyTarget, stepCounter, StepCounter.Zero, ecbc, ref jobIndex);
            return applyTarget;
        }

        public static void EditByMode<T>(in ComponentCommandMode mode, in Entity applyTarget, in T value, in T defaultValue, EntityManager em)
            where T : unmanaged, IComponentData
        {
            switch (mode)
            {
                case ComponentCommandMode.Create:
                    if (!em.HasComponent<T>(applyTarget)) em.AddComponentData(applyTarget, defaultValue);
                    break;
                case ComponentCommandMode.Remove:
                    if (em.HasComponent<T>(applyTarget)) em.RemoveComponent<T>(applyTarget);
                    break;
                case ComponentCommandMode.Set:
                    if (em.HasComponent<T>(applyTarget)) em.SetComponentData(applyTarget, value);
                    else em.AddComponentData(applyTarget, value);
                    break;
            }
        }

        public static void EditByMode<T>(in ComponentCommandMode mode, in Entity applyTarget, in T value, in T defaultValue, EntityCommandBuffer ecb)
            where T : unmanaged, IComponentData
        {
            switch (mode)
            {
                case ComponentCommandMode.Create: ecb.AddComponent(applyTarget, defaultValue); break;
                case ComponentCommandMode.Remove: ecb.RemoveComponent<T>(applyTarget); break;
                case ComponentCommandMode.Set: ecb.AddComponent<T>(applyTarget); ecb.SetComponent(applyTarget, value); break;
            }
        }

        public static void EditByMode<T>(in ComponentCommandMode mode, in Entity applyTarget, in T value, in T defaultValue, EntityCommandBuffer.Concurrent ecbc, ref int jobIndex)
            where T : unmanaged, IComponentData
        {
            switch (mode)
            {
                case ComponentCommandMode.Create: ecbc.AddComponent(jobIndex++, applyTarget, defaultValue); break;
                case ComponentCommandMode.Remove: ecbc.RemoveComponent<T>(jobIndex++, applyTarget); break;
                case ComponentCommandMode.Set: ecbc.AddComponent<T>(jobIndex++, applyTarget); ecbc.SetComponent(jobIndex++, applyTarget, value); break;
            }
        }

    }

}