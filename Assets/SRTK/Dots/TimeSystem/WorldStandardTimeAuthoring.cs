/************************************************************************************
| File: WorldStandardTimeAuthoring.cs                                               |
| Project: lieene.TimeSystem                                                        |
| Created Date: Fri Mar 20 2020                                                     |
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
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
// using NaughtyAttributes;

namespace SRTK
{
    [AddComponentMenu("SRTK/WorldStandardTimeAuthoring")]
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class WorldStandardTimeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] [Range(0,100f)] internal float timeScale = 1;
        [SerializeField] [Range(10,240)] internal float StepPreSecond = 60;

        private EntityManager EntityManager;
        private Entity worldTimeScaleEntity;
        private Entity worldTimeEntity;
        private Entity worldTimeStepEntity;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            EntityManager = dstManager;
            using (var q = EntityManager.CreateEntityQuery(typeof(WorldStandardTime)))
            {
                if (q.CalculateEntityCount() > 0)
                {
                    using (var a = q.ToEntityArray(Allocator.TempJob)) { worldTimeEntity = a[0]; }
                }
                else
                {
                    worldTimeEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(worldTimeEntity, new WorldStandardTime()
                    {
                        frameCounter = FrameCounter.Zero,
                        unscaledDeltaTime = DeltaTime.Zero,
                        unscaledTime = ElapsedTime.Zero,
                        deltaTime = DeltaTime.Zero,
                        time = ElapsedTime.Zero,
                    });
                }
            }

            using (var q = EntityManager.CreateEntityQuery(typeof(WorldStandardTimeScale)))
            {
                if (q.CalculateEntityCount() > 0)
                {
                    using (var a = q.ToEntityArray(Allocator.TempJob))
                    {
                        worldTimeScaleEntity = a[0];
                        EntityManager.SetComponentData(worldTimeScaleEntity, new WorldStandardTimeScale() { timeScale = timeScale });
                    }
                }
                else
                {
                    worldTimeScaleEntity = EntityManager.CreateEntity();
                    EntityManager.SetName(worldTimeScaleEntity, nameof(WorldStandardTimeScale));
                    EntityManager.AddComponentData(worldTimeScaleEntity, new WorldStandardTimeScale() { timeScale = timeScale });
                }
            }

            using (var q = EntityManager.CreateEntityQuery(typeof(WorldStandardTimeStep)))
            {
                if (q.CalculateEntityCount() > 0)
                {
                    using (var a = q.ToEntityArray(Allocator.TempJob))
                    {
                        worldTimeStepEntity = a[0];
                        var worldTimeStep = EntityManager.GetComponentData<WorldStandardTimeStep>(worldTimeStepEntity);
                        worldTimeStep.fixedTimeStep.StepPreSecond = StepPreSecond;
                        EntityManager.SetComponentData(worldTimeStepEntity, worldTimeStep);
                    }
                }
                else
                {
                    worldTimeStepEntity = EntityManager.CreateEntity();
                    EntityManager.SetName(worldTimeStepEntity, nameof(WorldStandardTimeStep));

                    EntityManager.AddComponentData(worldTimeStepEntity, new WorldStandardTimeStep()
                    {
                        fixedTimeStep = FixedTimeStep.PhysicsStep(StepPreSecond),
                        stepCounter = StepCounter.Zero,
                    });
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && worldTimeEntity != Entity.Null && worldTimeScaleEntity != Entity.Null && worldTimeStepEntity != Entity.Null && EntityManager != null)
            {
                EntityManager.SetComponentData(worldTimeScaleEntity, new WorldStandardTimeScale() { timeScale = timeScale });
                var worldTimeStep = EntityManager.GetComponentData<WorldStandardTimeStep>(worldTimeStepEntity);
                worldTimeStep.fixedTimeStep.StepPreSecond = StepPreSecond;
                EntityManager.SetComponentData(worldTimeStepEntity, worldTimeStep);
            }
        }
    }
}