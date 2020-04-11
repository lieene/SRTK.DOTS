/************************************************************************************
| File: TimeHierarchySystem.cs                                                      |
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
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Collections.LowLevel.Unsafe;

namespace SRTK
{
    [UpdateInGroup(typeof(PrepareFrameSystemGroup))]
    [UpdateBefore(typeof(TimeScaleSystem))]
    public class TimeHierarchySystem : SystemBase
    {
        static readonly ProfilerMarker k_ProfileDeletedParents = new ProfilerMarker("TimeParentSystem.DeletedParents");
        static readonly ProfilerMarker k_ProfileRemoveParents = new ProfilerMarker("TimeParentSystem.RemoveParents");
        static readonly ProfilerMarker k_ProfileChangeParents = new ProfilerMarker("TimeParentSystem.ChangeParents");
        static readonly ProfilerMarker k_ProfileNewParents = new ProfilerMarker("TimeParentSystem.NewParents");

        EntityQuery ParentChildrenRemovedQuery;//Could be remove children buffer or entity destroyed
        EntityQuery ChildParentRemovedQuery;//Could be remove parent component or entity destroyed
        EntityQuery ChildNewParentQuery;
        EntityQuery ChildParentChangeQuery;

        EntityQuery ChildDontHaveTimeScaleQuery;
        EntityQuery ChildDontHaveLocalTimeScaleQuery;

        private EntityQuery ChildDontHaveTimescaleQuery;
        private EntityQuery ParentDontHaveChildQuery;
        protected override void OnCreate()
        {
            ChildDontHaveTimeScaleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<ParentTime>() },
                None = new ComponentType[] { typeof(TimeScale) },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            ChildDontHaveLocalTimeScaleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<ParentTime>() },
                None = new ComponentType[] { typeof(LocalTimeScale) },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            ChildNewParentQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadOnly<TimeScale>(),
                    ComponentType.ReadWrite<LocalTimeScale>()
                },
                None = new ComponentType[] { typeof(PreviousParentTime) },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            ChildParentRemovedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<PreviousParentTime>(),
                    ComponentType.ReadOnly<TimeScale>(),
                    ComponentType.ReadWrite<LocalTimeScale>()},
                None = new ComponentType[] { typeof(ParentTime) },
            });

            ChildDontHaveTimescaleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(PreviousParentTime) },
                None = new ComponentType[] { typeof(TimeScale), typeof(LocalTimeScale) }
            });

            ParentDontHaveChildQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(ChildTime) },
                None = new ComponentType[] { typeof(TimeScale), typeof(LocalTimeScale) }
            });

            ChildParentChangeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParentTime>(),
                    ComponentType.ReadOnly<TimeScale>(),
                    ComponentType.ReadWrite<LocalTimeScale>(),
                    ComponentType.ReadWrite<PreviousParentTime>()
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            ChildParentChangeQuery.SetChangedVersionFilter(typeof(ParentTime));

            ParentChildrenRemovedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<ChildTime>(), typeof(ParentTime) },
                None = new ComponentType[] { typeof(TimeScale), typeof(LocalTimeScale) },
            });
        }

        protected override void OnUpdate()
        {
            Dependency.Complete(); // #todo

            k_ProfileDeletedParents.Begin();
            UpdateParentWhoseChildrenRefIsRemoved();
            k_ProfileDeletedParents.End();

            k_ProfileChangeParents.Begin();
            ChildIsRemoved();
            k_ProfileChangeParents.End();

            k_ProfileChangeParents.Begin();
            ParentIsRemove();
            k_ProfileChangeParents.End();

            k_ProfileRemoveParents.Begin();
            UpdateChildWhoseParentRefIsRemoved();
            k_ProfileRemoveParents.End();

            k_ProfileNewParents.Begin();
            UpdateChildWhoseParentRefIsNew();
            k_ProfileNewParents.End();

            k_ProfileChangeParents.Begin();
            UpdateChildWhoseParentIsChanged();
            k_ProfileChangeParents.End();

            Dependency = new JobHandle();
        }

        static int FindChildIndex(DynamicBuffer<ChildTime> children, Entity entity)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Value == entity)
                    return i;
            }
            throw new InvalidOperationException("Child entity not in parent");
        }

        [BurstCompile]
        struct GatherChildEntities : IJob
        {
            public NativeArray<Entity> Parents;
            public NativeList<Entity> Children;

            [ReadOnly] public BufferFromEntity<ChildTime> ChildrenBuffAccess;
            [ReadOnly] public ComponentDataFromEntity<ParentTime> ParentTimeAccess;

            public void Execute()
            {
                for (int i = 0; i < Parents.Length; i++)
                {
                    var parentEntity = Parents[i];
                    var childEntitiesSource = ChildrenBuffAccess[parentEntity].AsNativeArray();
                    for (int j = 0; j < childEntitiesSource.Length; j++)
                    {
                        var childEid = childEntitiesSource[j].Value;
                        var parentComp = ParentTimeAccess[childEid];
                        Children.Add(childEid);
                    }
                }
            }
        }

        void UpdateParentWhoseChildrenRefIsRemoved()
        {
            if (ParentChildrenRemovedQuery.IsEmptyIgnoreFilter) return;

            var previousParents = ParentChildrenRemovedQuery.ToEntityArray(Allocator.TempJob);
            var ChildEntities = new NativeList<Entity>(Allocator.TempJob);
            var gatherChildEntitiesJob = new GatherChildEntities
            {
                Parents = previousParents,
                Children = ChildEntities,
                ChildrenBuffAccess = GetBufferFromEntity<ChildTime>()
            };
            var gatherChildEntitiesJobHandle = gatherChildEntitiesJob.Schedule();
            gatherChildEntitiesJobHandle.Complete();

            EntityManager.RemoveComponent(ChildEntities, typeof(ParentTime));
            EntityManager.RemoveComponent(ChildEntities, typeof(PreviousParentTime));
            EntityManager.RemoveComponent(ParentChildrenRemovedQuery, typeof(ChildTime));

            ChildEntities.Dispose();
            previousParents.Dispose();
        }

        void ChildIsRemoved()
        {
            if (ChildDontHaveTimescaleQuery.IsEmptyIgnoreFilter)
                return;

            var childEntities = ChildDontHaveTimescaleQuery.ToEntityArray(Allocator.TempJob);
            var previousParents = ChildDontHaveTimescaleQuery.ToComponentDataArray<PreviousParentTime>(Allocator.TempJob);
            for (int i = 0; i < childEntities.Length; i++)
            {
                var childEntity = childEntities[i];

                var previousParentEntity = previousParents[i].Value;
                if (!EntityManager.HasComponent<ChildTime>(previousParentEntity)) continue;

                var children = EntityManager.GetBuffer<ChildTime>(previousParentEntity);
                var childIndex = FindChildIndex(children, childEntity);
                children.RemoveAt(childIndex);
                if (children.Length == 0)
                {
                    EntityManager.RemoveComponent(previousParentEntity, typeof(ChildTime));
                }
            }
            EntityManager.RemoveComponent(ChildDontHaveTimescaleQuery, typeof(PreviousParentTime));
            childEntities.Dispose();
            previousParents.Dispose();
        }

        void ParentIsRemove()
        {
            if (ParentDontHaveChildQuery.IsEmptyIgnoreFilter) return;
            var parentEntities = ParentDontHaveChildQuery.ToEntityArray(Allocator.TempJob);
            var dynChild = new NativeList<Entity>(Allocator.TempJob);

            for (int i = 0; i < parentEntities.Length; i++)
            {
                var parentEntity = parentEntities[i];
                var children = EntityManager.GetBuffer<ChildTime>(parentEntity);
                for (int j = 0, len = children.Length; j < len; j++)
                {
                    var child = children[j];
                    dynChild.Add(child.Value);
                }
            }

            EntityManager.RemoveComponent<ParentTime>(dynChild);
            EntityManager.RemoveComponent<ChildTime>(ParentDontHaveChildQuery);
            parentEntities.Dispose();
            dynChild.Dispose();
        }

        void UpdateChildWhoseParentRefIsRemoved()
        {
            if (ChildParentRemovedQuery.IsEmptyIgnoreFilter) return;

            var childEntities = ChildParentRemovedQuery.ToEntityArray(Allocator.TempJob);
            var previousParents = ChildParentRemovedQuery.ToComponentDataArray<PreviousParentTime>(Allocator.TempJob);
            var ltsAccess = GetComponentDataFromEntity<LocalTimeScale>(false);
            var tsAccess = GetComponentDataFromEntity<TimeScale>(true);
            for (int i = 0; i < childEntities.Length; i++)
            {
                var childEntity = childEntities[i];
                ltsAccess[childEntity] = ltsAccess[childEntity].RecordParentChange(tsAccess[childEntity].KeepTimeScaleOnParentChange);
                var previousParentEntity = previousParents[i].Value;
                if (!EntityManager.HasComponent<ChildTime>(previousParentEntity)) continue;

                var children = EntityManager.GetBuffer<ChildTime>(previousParentEntity);
                var childIndex = FindChildIndex(children, childEntity);
                children.RemoveAt(childIndex);

                if (children.Length == 0) EntityManager.RemoveComponent(previousParentEntity, typeof(ChildTime));
            }
            EntityManager.RemoveComponent(ChildParentRemovedQuery, typeof(PreviousParentTime));
            childEntities.Dispose();
            previousParents.Dispose();
        }

        void UpdateChildWhoseParentRefIsNew()
        {
            if (ChildNewParentQuery.IsEmptyIgnoreFilter) return;
            var childEntities = ChildNewParentQuery.ToEntityArray(Allocator.TempJob);
            var ltsAccess = GetComponentDataFromEntity<LocalTimeScale>();
            var tsAccess = GetComponentDataFromEntity<TimeScale>(true);
            for (int i = 0; i < childEntities.Length; i++)
            {
                var childEntity = childEntities[i];
                ltsAccess[childEntity] = ltsAccess[childEntity].RecordParentChange(tsAccess[childEntity].KeepTimeScaleOnParentChange);
            }
            childEntities.Dispose();
            EntityManager.AddComponent(ChildNewParentQuery, typeof(PreviousParentTime));
        }

        [BurstCompile]
        struct GatherChangedParents : IJobChunk
        {
            public NativeMultiHashMap<Entity, Entity>.ParallelWriter ParentChildrenToAdd;
            public NativeMultiHashMap<Entity, Entity>.ParallelWriter ParentChildrenToRemove;
            public NativeHashMap<Entity, int>.ParallelWriter UniqueParents;
            public ArchetypeChunkComponentType<PreviousParentTime> PreviousParentType;

            [ReadOnly] public ArchetypeChunkComponentType<ParentTime> ParentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;

            [ReadOnly] public ComponentDataFromEntity<TimeScale> tsAccess;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<LocalTimeScale> ltsAccess;
            public uint LastSystemVersion;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.DidChange(ParentType, LastSystemVersion))
                {
                    var chunkPreviousParents = chunk.GetNativeArray(PreviousParentType);
                    var chunkParents = chunk.GetNativeArray(ParentType);
                    var chunkEntities = chunk.GetNativeArray(EntityType);

                    for (int j = 0; j < chunk.Count; j++)
                    {
                        if (chunkParents[j].Value != chunkPreviousParents[j].Value)
                        {
                            var childEntity = chunkEntities[j];

                            ltsAccess[childEntity] = ltsAccess[childEntity].RecordParentChange(tsAccess[childEntity].KeepTimeScaleOnParentChange);
                            var parentEntity = chunkParents[j].Value;
                            var previousParentEntity = chunkPreviousParents[j].Value;

                            ParentChildrenToAdd.Add(parentEntity, childEntity);
                            UniqueParents.TryAdd(parentEntity, 0);

                            if (previousParentEntity != Entity.Null)
                            {
                                ParentChildrenToRemove.Add(previousParentEntity, childEntity);
                                UniqueParents.TryAdd(previousParentEntity, 0);
                            }
                            chunkPreviousParents[j] = new PreviousParentTime { Value = parentEntity };
                        }
                    }
                }
            }
        }

        [BurstCompile]
        struct FindMissingChild : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, int> UniqueParents;
            [ReadOnly] public BufferFromEntity<ChildTime> ChildFromEntity;
            public NativeList<Entity> ParentsMissingChild;
            public void Execute()
            {
                var parents = UniqueParents.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < parents.Length; i++)
                {
                    var parent = parents[i];
                    if (!ChildFromEntity.Exists(parent)) ParentsMissingChild.Add(parent);
                }
            }
        }

        [BurstCompile]
        struct FixupChangedChildren : IJob
        {
            [ReadOnly] public NativeMultiHashMap<Entity, Entity> ParentChildrenToAdd;
            [ReadOnly] public NativeMultiHashMap<Entity, Entity> ParentChildrenToRemove;
            [ReadOnly] public NativeHashMap<Entity, int> UniqueParents;
            public BufferFromEntity<ChildTime> ChildFromEntity;

            public void Execute()
            {
                var parents = UniqueParents.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < parents.Length; i++)
                {
                    var parent = parents[i];
                    var children = ChildFromEntity[parent];
                    {//Remove
                        if (ParentChildrenToRemove.TryGetFirstValue(parent, out var child, out var it))
                        {
                            do { var childIndex = FindChildIndex(children, child); children.RemoveAt(childIndex); }
                            while (ParentChildrenToRemove.TryGetNextValue(out child, ref it));
                        }
                    }
                    {//Add
                        if (ParentChildrenToAdd.TryGetFirstValue(parent, out var child, out var it))
                        {
                            do { children.Add(new ChildTime() { Value = child }); }
                            while (ParentChildrenToAdd.TryGetNextValue(out child, ref it));
                        }
                    }
                }
            }
        }

        void UpdateChildWhoseParentIsChanged()//Including change form previous updates
        {
            if (ChildParentChangeQuery.IsEmptyIgnoreFilter) return;
            var count = ChildParentChangeQuery.CalculateEntityCount() * 2; // Potentially 2x changed: current and previous 
            if (count == 0) return;

            // 1. Get (Parent,Child) to remove
            // 2. Get (Parent,Child) to add
            // 3. Get unique Parent change list
            // 4. Set PreviousParent to new Parent
            var parentChildrenToAdd = new NativeMultiHashMap<Entity, Entity>(count, Allocator.TempJob);
            var parentChildrenToRemove = new NativeMultiHashMap<Entity, Entity>(count, Allocator.TempJob);
            var uniqueParents = new NativeHashMap<Entity, int>(count, Allocator.TempJob);
            var gatherChangedParentsJob = new GatherChangedParents
            {
                ParentChildrenToAdd = parentChildrenToAdd.AsParallelWriter(),
                ParentChildrenToRemove = parentChildrenToRemove.AsParallelWriter(),
                tsAccess = GetComponentDataFromEntity<TimeScale>(true),
                ltsAccess = GetComponentDataFromEntity<LocalTimeScale>(false),
                UniqueParents = uniqueParents.AsParallelWriter(),
                PreviousParentType = GetArchetypeChunkComponentType<PreviousParentTime>(false),
                ParentType = GetArchetypeChunkComponentType<ParentTime>(true),
                EntityType = GetArchetypeChunkEntityType(),
                LastSystemVersion = LastSystemVersion
            };
            var gatherChangedParentsJobHandle = gatherChangedParentsJob.Schedule(ChildParentChangeQuery);
            gatherChangedParentsJobHandle.Complete();

            // 5. (Structural change) Add any missing Child to Parents
            var parentsMissingChildBuf = new NativeList<Entity>(Allocator.TempJob);
            var findMissingChildJob = new FindMissingChild
            {
                UniqueParents = uniqueParents,
                ChildFromEntity = GetBufferFromEntity<ChildTime>(),
                ParentsMissingChild = parentsMissingChildBuf
            };
            var findMissingChildJobHandle = findMissingChildJob.Schedule();
            findMissingChildJobHandle.Complete();

            EntityManager.AddComponent(parentsMissingChildBuf.AsArray(), typeof(ChildTime));

            // 6. Get Child[] for each unique Parent
            // 7. Update Child[] for each unique Parent
            var fixupChangedChildrenJob = new FixupChangedChildren
            {
                ParentChildrenToAdd = parentChildrenToAdd,
                ParentChildrenToRemove = parentChildrenToRemove,
                UniqueParents = uniqueParents,
                ChildFromEntity = GetBufferFromEntity<ChildTime>()
            };

            var fixupChangedChildrenJobHandle = fixupChangedChildrenJob.Schedule();
            fixupChangedChildrenJobHandle.Complete();

            //Mark Time that became Parent with HasChildTIme tag. this tag is use to track Destroyed Parent entity
            //EntityManager.AddComponent<HasChildTime>(ParentChildrenRemovedQuery);
            //todo add localtimescale timescale
            EntityManager.AddComponent<TimeScale>(ParentChildrenRemovedQuery);
            EntityManager.AddComponent<LocalTimeScale>(ParentChildrenRemovedQuery);
            //ParentChildrenRemovedQuery is the same as Parent whose has no marked HasChildTIme has childTime buffer added to it
            parentChildrenToAdd.Dispose();
            parentChildrenToRemove.Dispose();
            uniqueParents.Dispose();
            parentsMissingChildBuf.Dispose();
        }
    }
}