//{"NewSystem":"$basename","SRTK":"$MyNameSpace","NewJob":"The Job"}
using System;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SRTK
{
    public class NewSystem : SystemBase
    {
        EntityQuery mQuary;
        NewJob mJob;

        protected override void OnCreate()
        {
            mQuary = GetEntityQuery(ComponentType.ReadOnly<Translation>());
            mJob = new NewJob();
        }

        protected override void OnUpdate()
        {
            Dependency = mJob.Schedule(mQuary, Dependency);
        }

        protected override void OnDestroy() { }

        struct NewJob : IJobChunk
        {
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                throw new NotImplementedException();
            }
        }
    }
}