//{"NewComponentData":"$basename","SRTK":"$MyNameSpace"}
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
    [GenerateAuthoringComponent]
    public struct NewComponentData : IComponentData, IEquatable<NewComponentData>
    {
        public bool Equals(NewComponentData other) => throw new NotImplementedException();
    }
}
