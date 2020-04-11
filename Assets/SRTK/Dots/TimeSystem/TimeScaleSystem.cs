/************************************************************************************
| File: TimeScaleSystem.cs                                                          |
| Project: lieene.TimeSystem                                                        |
| Created Date: Tue Mar 10 2020                                                     |
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
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;


namespace SRTK
{
    public class LoopReferenceException : Exception { public LoopReferenceException(string message) : base(message) { } }

    [UpdateInGroup(typeof(PrepareFrameSystemGroup))]
    public class TimeScaleSystem : SystemBase
    {
        private EntityQuery m_RootsGroup;
        private EntityQueryMask m_TimeScaleWriteGroupMaskLocalParent;
        private EntityQueryMask m_TimeScaleWriteGroupMaskParent;
        private EntityQuery AnyRootChangeQuery;
        private EntityQuery AnyRootChangeQueryLocal;
        private EntityQuery AnyChildChangeQuery;
        private EntityQuery AnyChildChangeQueryLocal;

        protected override void OnCreate()
        {
            m_RootsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<ChildTime>(), ComponentType.ReadOnly<TimeScale>() },
                None = new ComponentType[] { typeof(ParentTime) }
            });

            m_TimeScaleWriteGroupMaskLocalParent = EntityManager.GetEntityQueryMask(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadWrite<TimeScale>(),
                    ComponentType.ReadOnly<LocalTimeScale>()
                },
                Options = EntityQueryOptions.FilterWriteGroup
            }));

            m_TimeScaleWriteGroupMaskParent = EntityManager.GetEntityQueryMask(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadWrite<TimeScale>(),
                },
                Options = EntityQueryOptions.FilterWriteGroup
            }));

            AnyRootChangeQueryLocal = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ChildTime>(),
                    ComponentType.ReadOnly<LocalTimeScale>(),
                },
            });
            AnyRootChangeQueryLocal.AddChangedVersionFilter(typeof(LocalTimeScale));

            AnyRootChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ChildTime>(),
                    ComponentType.ReadOnly<TimeScale>(),
                },
            });
            AnyRootChangeQuery.AddChangedVersionFilter(typeof(TimeScale));


            AnyChildChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadOnly<TimeScale>(),
                },
            });
            AnyChildChangeQuery.AddChangedVersionFilter(typeof(ParentTime));
            AnyChildChangeQuery.AddChangedVersionFilter(typeof(TimeScale));

            AnyChildChangeQueryLocal = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadOnly<LocalTimeScale>(),
                },
            });
            AnyChildChangeQueryLocal.AddChangedVersionFilter(typeof(ParentTime));
            AnyChildChangeQueryLocal.AddChangedVersionFilter(typeof(LocalTimeScale));
        }

        protected override void OnUpdate()
        {
            var noHierarchyChange = AnyRootChangeQuery.CalculateChunkCount() < 1 && AnyRootChangeQueryLocal.CalculateChunkCount() < 1
                && AnyChildChangeQuery.CalculateEntityCount() < 1 && AnyChildChangeQueryLocal.CalculateChunkCount() < 1;

            //sync LocalTimeScale to TimeScale for all root time
            Dependency = Entities.WithNone<ParentTime>().WithChangeFilter<LocalTimeScale>()
                .ForEach((ref TimeScale ts, ref LocalTimeScale lts) =>
                {
                    if (lts.KeepTimeScale)
                    {
                        lts = ts;
                    }
                    else
                    {
                        ts.value = lts.value;
                    }
                    //lts = lts.KeepTimeScale ? ts.value = lts.value : ts;
                }).ScheduleParallel(Dependency);

            if (noHierarchyChange) return;
            else Dependency.Complete();
            //UnityEngine.Debug.LogAssertion("Change Found Updating Time Scale Tree");


            var TimeScaleType = GetArchetypeChunkComponentType<TimeScale>(true);
            var childType = GetArchetypeChunkBufferType<ChildTime>(true);
            var childFromEntity = GetBufferFromEntity<ChildTime>(true);
            var LocalTimeScaleFromEntity = GetComponentDataFromEntity<LocalTimeScale>(false);
            var TimeScaleFromEntity = GetComponentDataFromEntity<TimeScale>();

            Dependency = new RecursiveUpdateTimeScaleJob
            {
                TimeScaleType = TimeScaleType,
                ChildType = childType,
                ChildFromEntity = childFromEntity,
                LocalTimeScaleFromEntity = LocalTimeScaleFromEntity,
                TimeScaleFromEntity = TimeScaleFromEntity,
                timeScaleWriteGroupMaskLocalParent = m_TimeScaleWriteGroupMaskLocalParent,
                timeScaleWriteGroupMaskParent = m_TimeScaleWriteGroupMaskParent,
                LastSystemVersion = LastSystemVersion
            }.ScheduleParallel(m_RootsGroup, Dependency);
        }

        [BurstCompile]
        struct RecursiveUpdateTimeScaleJob : IJobChunk
        {
            public const int recursiveDepthLimit = 30;
            [ReadOnly] public ArchetypeChunkComponentType<TimeScale> TimeScaleType;
            [ReadOnly] public ArchetypeChunkBufferType<ChildTime> ChildType;
            [ReadOnly] public BufferFromEntity<ChildTime> ChildFromEntity;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<LocalTimeScale> LocalTimeScaleFromEntity;

            [ReadOnly] public EntityQueryMask timeScaleWriteGroupMaskLocalParent;
            [ReadOnly] public EntityQueryMask timeScaleWriteGroupMaskParent;

            public uint LastSystemVersion;

            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<TimeScale> TimeScaleFromEntity;

            [BurstDiscard]
            void LogError(Entity entity) { UnityEngine.Debug.LogError($"Drop at time hierarchy depth over {recursiveDepthLimit}. on Entity {entity.Index}"); }

            void ChildTimeScale(float parentTimeScale, Entity entity, bool updateChildrenTimeScale, int depth)
            {
                if (depth++ > recursiveDepthLimit)
                {
                    LogError(entity);
                    return;
                }
               
                bool IsWritingByParentAndLocal = timeScaleWriteGroupMaskLocalParent.Matches(entity);
                updateChildrenTimeScale = updateChildrenTimeScale || (IsWritingByParentAndLocal && LocalTimeScaleFromEntity.DidChange(entity, LastSystemVersion));

                bool needUpdate = true;

                if (updateChildrenTimeScale)
                {
                    if (IsWritingByParentAndLocal)
                    {
                        if (TimeScaleFromEntity[entity].KeepTimeScaleOnParentChange)
                        {
                            LocalTimeScaleFromEntity[entity] = parentTimeScale != 0 ? TimeScaleFromEntity[entity].value / parentTimeScale : TimeScaleFromEntity[entity].value;
                        }

                        parentTimeScale *= LocalTimeScaleFromEntity[entity].KeepTimeScale ? -LocalTimeScaleFromEntity[entity].value : LocalTimeScaleFromEntity[entity].value;
                        TimeScaleFromEntity[entity] = new TimeScale(parentTimeScale, TimeScaleFromEntity[entity].KeepTimeScaleOnParentChange);
                        needUpdate = false;
                    }
                    else if (timeScaleWriteGroupMaskParent.Matches(entity))
                    {
                        TimeScaleFromEntity[entity] = new TimeScale(parentTimeScale, TimeScaleFromEntity[entity].KeepTimeScaleOnParentChange);
                        needUpdate = false;
                    }
                }


                if (needUpdate)//This entity has a component with the WriteGroup(TimeScale)
                {
                    parentTimeScale = LocalTimeScaleFromEntity[entity].KeepTimeScale ? -TimeScaleFromEntity[entity].value: TimeScaleFromEntity[entity].value;
                }
                LocalTimeScaleFromEntity[entity] = LocalTimeScaleFromEntity[entity].KeepTimeScale ? -LocalTimeScaleFromEntity[entity].value : LocalTimeScaleFromEntity[entity].value;
                if (ChildFromEntity.Exists(entity))
                {
                    var children = ChildFromEntity[entity];
                    for (int i = 0; i < children.Length; i++)
                    {
                        ChildTimeScale(parentTimeScale, children[i].Value, updateChildrenTimeScale, depth);
                    }
                }
            }

            public void Execute(ArchetypeChunk chunk, int index, int entityOffset)
            {
                bool updateChildrenTimeScale =
                    chunk.DidChange<TimeScale>(TimeScaleType, LastSystemVersion) ||
                    chunk.DidChange<ChildTime>(ChildType, LastSystemVersion);
                
                var chunkTimeScale = chunk.GetNativeArray(TimeScaleType);
                var chunkChildren = chunk.GetBufferAccessor(ChildType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var timeScale = chunkTimeScale[i].value;
                    var children = chunkChildren[i];
                    
                    for (int j = 0; j < children.Length; j++)
                    {
                        ChildTimeScale(timeScale, children[j].Value, updateChildrenTimeScale, 0);

                    }
                }
            }
        }

    }
}