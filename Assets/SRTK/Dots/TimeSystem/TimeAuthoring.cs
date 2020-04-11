/************************************************************************************
| File: TimeAuthoring.cs                                                            |
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

using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace SRTK
{
    [AddComponentMenu("SRTK/TimeAuthoring")]
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class TimeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] float localTimeScale = 1;
        [SerializeField] int initialFrameCount = int.MinValue;
        [SerializeField] bool hasDeltaTime = false;
        [SerializeField] float initialElapsedTime = float.MinValue;
        [SerializeField] float fixedTimeStep = float.MinValue;
        [SerializeField] int initialStepCount = int.MinValue;
        [SerializeField] TimeAuthoring parent = null;
        [SerializeField] List<TimeAuthoring> children = new List<TimeAuthoring>();

        EntityManager dstManager = null;
        Entity entity = Entity.Null;

        List<Entity> pendingChildren = new List<Entity>();
        Entity pendingParent = Entity.Null;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            this.dstManager = dstManager;
            this.entity = entity;
            DoConvert(entity, dstManager);

            if (parent != null) parent.pendingChildren.Add(entity);
            if (children.Count > 0) foreach (var c in children) if (c != null) c.pendingParent = entity;

            if (pendingParent != Entity.Null) DoParentChildLink(entity, pendingParent, dstManager);
            if (pendingChildren.Count > 0) foreach (var c in pendingChildren) DoParentChildLink(c, entity, dstManager);
        }

        void DoParentChildLink(Entity child, Entity parent, EntityManager dstManager) => dstManager.AddComponentData<ParentTime>(child, new ParentTime() { Value = parent });

        void DoConvert(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponentData<TimeScale>(entity, new TimeScale(1));
            dstManager.AddComponentData<LocalTimeScale>(entity, new LocalTimeScale(localTimeScale));
            if (initialFrameCount != int.MinValue) dstManager.AddComponentData<FrameCounter>(entity, new FrameCounter(initialFrameCount));
            if (hasDeltaTime || fixedTimeStep != float.MinValue) dstManager.AddComponentData<DeltaTime>(entity, new DeltaTime(0));
            if (initialElapsedTime != float.MinValue) dstManager.AddComponentData<ElapsedTime>(entity, new ElapsedTime(initialElapsedTime));
            if (fixedTimeStep != float.MinValue) dstManager.AddComponentData<FixedTimeStep>(entity, new FixedTimeStep(fixedTimeStep));
            if (initialStepCount != int.MinValue) dstManager.AddComponentData<StepCounter>(entity, new StepCounter(initialStepCount));
        }

        private void OnValidate()
        {
            if (Application.isPlaying && entity != Entity.Null && dstManager != null)
            {

            }
        }
    }
}