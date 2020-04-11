//{"NewComponentAuthoring":"$basename","SRTK":"$MyNameSpace"}
using System;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace SRTK
{
    [AddComponentMenu("STRK/NewComponentAuthoring")]
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class NewComponentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        private EntityManager entityManager;
        private Entity entity;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            this.entity = entity;
            this.entityManager = dstManager;
            dstManager.AddComponentData<Translation>(entity, new Translation() { Value = 0 });
        }

        private void OnValidate()
        {
            if(Application.isPlaying && entity!=Entity.Null && entityManager!=null)
            {
                entityManager.SetComponentData(entity, new Translation() { Value = 0 });
            }
        }
    }
}