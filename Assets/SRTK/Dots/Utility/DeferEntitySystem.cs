/************************************************************************************
| File: DeferEntitySystem.cs                                                        |
| Project: lieene.Unsafe                                                            |
| Created Date: Mon Mar 2 2020                                                      |
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
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(PrepareFrameSystemGroup))]
    public class DeferEntitySystem : JobComponentSystem
    {
        internal BeginSimulationEntityCommandBufferSystem simBeginCBS;
        internal EndSimulationEntityCommandBufferSystem simEndCBS;
        internal DeferEntityHolder holderPing;
        internal DeferEntityHolder holderPong;
        internal EntityQuery qWithDeferEntityID;
        FillDeferEntityJob mFillCacheJob;

        public DeferEntityCreator GetCreator(EntityCommandBufferSystem ecbs)
        {
            if (ecbs == null) throw new ArgumentNullException("Target EntityCommandBufferSystem is null");
            return new DeferEntityCreator(ecbs, holderPing);
        }

        public DeferEntityCreator GetSimBeginCreater() => new DeferEntityCreator(simBeginCBS, holderPing);
        public DeferEntityCreator GetSimEndCreator() => new DeferEntityCreator(simEndCBS, holderPing);

        public DeferEntityAccessor GetAccessor() => new DeferEntityAccessor(holderPong);
        public DeferEntityAccessor.Parallel GetParallelAccessor() => new DeferEntityAccessor(holderPong).ToParallel();

        [BurstCompile]
        struct FillDeferEntityJob : IJobChunk
        {
            [ReadOnly] internal ArchetypeChunkEntityType entityTp;
            [ReadOnly] internal ArchetypeChunkComponentType<DeferEntityID> deferETp;
            [NativeDisableParallelForRestriction] internal DeferEntityHolder.ParallelAccessor writer;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(entityTp);
                var defereds = chunk.GetNativeArray(deferETp);
                var count = chunk.Count;
                for (int i = 0; i < count; i++) { writer.SetDeferEntity(defereds[i].index, entities[i]); }
            }
        }

        protected override void OnCreate()
        {
            simBeginCBS = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            simEndCBS = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            qWithDeferEntityID = GetEntityQuery(ComponentType.ReadOnly<DeferEntityID>());
            mFillCacheJob = new FillDeferEntityJob();
        }


        protected void SwapBuffer()
        {
            if (holderPong.IsCreated) holderPong.Dispose();
            if (holderPing.IsCreated)
            {
                holderPong = holderPing;
                holderPong.AllocateCacheForFilling();
            }
            holderPing = new DeferEntityHolder(Allocator.TempJob);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();
            SwapBuffer();
            if (holderPong.IsCreated && qWithDeferEntityID.CalculateChunkCount() > 0)
            {
                mFillCacheJob.entityTp = GetArchetypeChunkEntityType();
                mFillCacheJob.deferETp = GetArchetypeChunkComponentType<DeferEntityID>();
                mFillCacheJob.writer = holderPong.ToParallelAccessor();
                mFillCacheJob.Schedule(qWithDeferEntityID, default).Complete();
                EntityManager.RemoveComponent<DeferEntityID>(qWithDeferEntityID);
            }
            return default;
        }

        protected override void OnDestroy()
        {
            if (holderPing.IsCreated) holderPing.Dispose();
            if (holderPong.IsCreated) holderPong.Dispose();
        }
    }


}
