//{"NewComponentSystem":"$basename","SRTK":"$MyNameSpace"}
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
    public class NewComponentSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref Translation t) => { });
            throw new NotImplementedException();
        }

        protected override void OnDestroy()
        {
        }
    }
}