/************************************************************************************
| File: TimeSystem.cs                                                               |
| Project: SRTK.TimeSystem                                                          |
| Created Date: Mon Feb 24 2020                                                     |
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

using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SRTK
{
    //using JobDesc = Unity.Entities.CodeGeneratedJobForEach.ForEachLambdaJobDescription;
    //internal struct TimeRank : ISharedComponentData { internal int rank; public int Rank => rank; }

    public struct WorldStandardTime : IComponentData
    {
        public ElapsedTime unscaledTime;
        public DeltaTime unscaledDeltaTime;
        public FrameCounter frameCounter;
        public ElapsedTime time;
        public DeltaTime deltaTime;
    }

    public struct WorldStandardTimeScale : IComponentData
    {
        public TimeScale timeScale;
    }
    public struct WorldStandardTimeStep : IComponentData
    {
        public FixedTimeStep fixedTimeStep;
        public StepCounter stepCounter;
    }

    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(PrepareFrameSystemGroup))]
    [UpdateAfter(typeof(TimeScaleSystem))]
    public class TimeSystem : SystemBase
    {
        #region Settings

        public enum DeltaTimeSourceType
        {
            UnityEngineDeltaTime = 0,
            UnityEngineUnscaledDeltaTime = 1,
            ComponentSystemTime = 2,
            NoSource = 3,
        }

        public DeltaTimeSourceType TimeSourceType { get; set; } = DeltaTimeSourceType.UnityEngineDeltaTime;

        #endregion Settings
        //--------------------------------------------------------------------------------------------------------------------------
        #region Time Control

        public TimeEditor CreateTimeEditor() => new TimeEditor(endSimCBS);

        #endregion Time Control

        #region ECS Fields

        EntityQuery activeTimesQuery;
        TimeChunkJob timeJob;
        EndSimulationEntityCommandBufferSystem endSimCBS;

        #endregion ECS Fields
        //--------------------------------------------------------------------------------------------------------------------------
        #region LifeCycle

        protected override void OnCreate()
        {
            timeJob = new TimeChunkJob() { };

            activeTimesQuery = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new ComponentType[]
                {
                    ComponentType.ReadWrite<DeltaTime>(),
                    ComponentType.ReadWrite<ElapsedTime>(),
                     ComponentType.ReadWrite<FixedTimeStep>()
                },
            });

            endSimCBS = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        public void WorldTimeTick(float worldDeltaTimeSource)
        {
            WorldStandardTimeScale worldTimeScale;
            if (!HasSingleton<WorldStandardTimeScale>())
            {
                worldTimeScale.timeScale = TimeScale.Default;
                var worldTimeScaleEntity = EntityManager.CreateEntity();
                EntityManager.SetName(worldTimeScaleEntity, nameof(WorldStandardTimeScale));
                EntityManager.AddComponentData(worldTimeScaleEntity, worldTimeScale);
            }
            else worldTimeScale = GetSingleton<WorldStandardTimeScale>();

            WorldStandardTime worldTime;
            if (!HasSingleton<WorldStandardTime>())
            {
                worldTime = new WorldStandardTime()
                {
                    frameCounter = FrameCounter.Zero.Tick(),
                    unscaledDeltaTime = worldDeltaTimeSource,
                    unscaledTime = worldDeltaTimeSource,
                    deltaTime = worldDeltaTimeSource,
                    time = worldDeltaTimeSource,
                };
                var worldTimeEntity = EntityManager.CreateEntity();
                EntityManager.SetName(worldTimeEntity, nameof(WorldStandardTime));
                EntityManager.AddComponentData(worldTimeEntity, worldTime);
            }
            else
            {
                worldTime = GetSingleton<WorldStandardTime>();
                worldTime.frameCounter.Tick();
                worldTime.unscaledDeltaTime = worldDeltaTimeSource;
                worldTime.unscaledTime.Tick(worldDeltaTimeSource);
                worldTime.deltaTime = worldTimeScale.timeScale.Scale(worldDeltaTimeSource);
                worldTime.time.Tick(worldTime.deltaTime);
                SetSingleton(worldTime);
            }

            WorldStandardTimeStep worldTimeStep;
            if (!HasSingleton<WorldStandardTimeStep>())
            {
                worldTimeStep = new WorldStandardTimeStep()
                {
                    fixedTimeStep = FixedTimeStep.Default.Tick(worldTime.deltaTime.value),
                    stepCounter = StepCounter.Zero,
                };
                worldTimeStep.stepCounter.Tick(worldTimeStep.fixedTimeStep.AggSteps);                
                var worldTimeStepEntity = EntityManager.CreateEntity();
                EntityManager.SetName(worldTimeStepEntity, nameof(WorldStandardTimeStep));
                EntityManager.AddComponentData(worldTimeStepEntity, worldTimeStep);
            }
            else
            {
                worldTimeStep = GetSingleton<WorldStandardTimeStep>();
                worldTimeStep.fixedTimeStep.Tick(worldTime.deltaTime);
                worldTimeStep.stepCounter.Tick(worldTimeStep.fixedTimeStep.AggSteps);
                SetSingleton(worldTimeStep);
            }
        }

        protected override void OnUpdate()
        {
            float worldDeltaTimeSource = 0;
            //world time source --------------------------------------------------------
            switch (TimeSourceType)
            {
                case DeltaTimeSourceType.UnityEngineDeltaTime:
                    worldDeltaTimeSource = UnityEngine.Time.deltaTime;
                    break;
                case DeltaTimeSourceType.UnityEngineUnscaledDeltaTime:
                    worldDeltaTimeSource = UnityEngine.Time.unscaledDeltaTime;
                    break;
                case DeltaTimeSourceType.ComponentSystemTime:
                    worldDeltaTimeSource = this.Time.DeltaTime;
                    break;
            }

            //world time ----------------------------------------------------------------
            WorldTimeTick(worldDeltaTimeSource);

            //update frame count ---------------------------------------------------------
            var frameCountJib = Entities.ForEach((ref FrameCounter fc) => fc.Tick()).ScheduleParallel(Dependency);

            var allTimCount = activeTimesQuery.CalculateChunkCount();
            if (allTimCount > 0)
            {
                //update Archetype ---------------------------------------------------------------------
                timeJob.tsTp = GetArchetypeChunkComponentType<TimeScale>(true);
                timeJob.dtTp = GetArchetypeChunkComponentType<DeltaTime>(false);
                timeJob.etTp = GetArchetypeChunkComponentType<ElapsedTime>(false);
                timeJob.ftsTp = GetArchetypeChunkComponentType<FixedTimeStep>(false);
                timeJob.scTp = GetArchetypeChunkComponentType<StepCounter>(false);
                timeJob.deltaTime = worldDeltaTimeSource;
                //Update DeltaTime and ElapsedTime -----------------------------------------------------
                Dependency = timeJob.ScheduleParallel(activeTimesQuery, Dependency);
            }

            Dependency = JobHandle.CombineDependencies(frameCountJib, Dependency);
        }

        #endregion LifeCycle
        //--------------------------------------------------------------------------------------------------------------------------
        #region Time Jobs
        [BurstCompile]
        struct TimeChunkJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<TimeScale> tsTp;
            public ArchetypeChunkComponentType<DeltaTime> dtTp;
            public ArchetypeChunkComponentType<ElapsedTime> etTp;
            public ArchetypeChunkComponentType<FixedTimeStep> ftsTp;
            public ArchetypeChunkComponentType<StepCounter> scTp;
            [ReadOnly] public float deltaTime;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                //DeltaTime dt;
                var count = chunk.Count;
                var chunkDeltaTime = chunk.Has(etTp) ? chunk.GetNativeArray(dtTp) : default;
                var chunkTimeScale = chunk.Has(tsTp) ? chunk.GetNativeArray(tsTp) : default;
                var chunkElapsedTime = chunk.Has(etTp) ? chunk.GetNativeArray(etTp) : default;
                var chunkFts = chunk.Has(ftsTp) ? chunk.GetNativeArray(ftsTp) : default;
                var chunkSc = chunk.Has(scTp) ? chunk.GetNativeArray(scTp) : default;
                if (chunkTimeScale != default)//has time scale
                {
                    if (chunkElapsedTime != default)//has ElapsedTime
                    {
                        if (chunkDeltaTime != default)//has DeltaTime and ElapsedTime
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                        chunkDeltaTime[i] = dt;
                                        var fst = chunkFts[i] = chunkFts[i].Tick(dt);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                        chunkDeltaTime[i] = dt;
                                        chunkFts[i] = chunkFts[i].Tick(dt);
                                    }
                                }
                            }
                            else//no fixed time step
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    var dt = chunkTimeScale[i].Scale(deltaTime);
                                    chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                    chunkDeltaTime[i] = dt;
                                }
                            }
                        }
                        else//has ElapsedTime no DeltaTime
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                        var fst = chunkFts[i] = chunkFts[i].Tick(dt);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                        chunkFts[i] = chunkFts[i].Tick(dt);
                                    }
                                }
                            }
                            else//no fixed time step
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    var dt = chunkTimeScale[i].Scale(deltaTime);
                                    chunkElapsedTime[i] = chunkElapsedTime[i].Tick(dt);
                                }
                            }
                        }
                    }
                    else//no ElapsedTime
                    {
                        //has DeltaTime no ElapsedTime
                        if (chunkDeltaTime != default)
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkDeltaTime[i] = chunkTimeScale[i].Scale(deltaTime);
                                        var fst = chunkFts[i] = chunkFts[i].Tick(dt);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        chunkDeltaTime[i] = chunkTimeScale[i].Scale(deltaTime);
                                        chunkFts[i] = chunkFts[i].Tick(dt);
                                    }
                                }
                            }
                            //no fixed time step
                            else for (int i = 0; i < count; i++) chunkDeltaTime[i] = chunkTimeScale[i].Scale(deltaTime);
                        }
                        else//no DeltaTime no ElapsedTime
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var dt = chunkTimeScale[i].Scale(deltaTime);
                                        var fst = chunkFts[i] = chunkFts[i].Tick(dt);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                //has fixed time step no step counter
                                else for (int i = 0; i < count; i++) chunkFts[i] = chunkFts[i].Tick(chunkTimeScale[i].Scale(deltaTime));
                            }//no fixed time step
                        }
                        //else no DeltaTime no ElapsedTime no fixedtime step no update
                    }
                }
                else//no time scale
                {
                    if (chunkElapsedTime != default)//has ElapsedTime
                    {
                        if (chunkDeltaTime != default)//has DeltaTime and ElapsedTime
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                                        chunkDeltaTime[i] = deltaTime;
                                        var fst = chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                                        chunkDeltaTime[i] = deltaTime;
                                        chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                    }
                                }
                            }
                            else//no fixed time step
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                                    chunkDeltaTime[i] = deltaTime;
                                }
                            }
                        }
                        else//has ElapsedTime no DeltaTime
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                                        var fst = chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                                        chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                    }
                                }
                            }
                            //no fixed time step
                            else for (int i = 0; i < count; i++) chunkElapsedTime[i] = chunkElapsedTime[i].Tick(deltaTime);
                        }
                    }
                    else//no ElapsedTime
                    {
                        //has DeltaTime no ElapsedTime
                        if (chunkDeltaTime != default)
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkDeltaTime[i] = deltaTime;
                                        var fst = chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        chunkDeltaTime[i] = deltaTime;
                                        chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                    }
                                }
                            }
                            //no fixed time step
                            else for (int i = 0; i < count; i++) chunkDeltaTime[i] = deltaTime;
                        }
                        else//no DeltaTime no ElapsedTime 
                        {
                            if (chunkFts != default)//has fixed time step
                            {
                                if (chunkSc != default)//has step counter
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var fst = chunkFts[i] = chunkFts[i].Tick(deltaTime);
                                        chunkSc[i] = chunkSc[i].Tick(fst.AggSteps);
                                    }
                                }
                                else//has fixed time step no step counter
                                { for (int i = 0; i < count; i++) chunkFts[i] = chunkFts[i].Tick(deltaTime); }
                            }
                        }
                        //else no DeltaTime no ElapsedTime no fixedtime step no update
                    }
                }
            }
        }

        #endregion Time Jobs

    }//Time System


}