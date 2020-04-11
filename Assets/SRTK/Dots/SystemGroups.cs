/************************************************************************************
| File: SystemGroups.cs                                                             |
| Project: lieene.SRTK                                                              |
| Created Date: Wed Mar 11 2020                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Sat Mar 14 2020                                                    |
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
    /// <summary>
    /// Update in <see cref="InitializationSystemGroup"/> prepare frame before SimulationSystemGroup
    /// Next syncpoint <see cref="EndInitializationEntityCommandBufferSystem"/>
    /// process entity/components after <see cref="BeginInitializationEntityCommandBufferSystem"/>
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    class PrepareFrameSystemGroup : ComponentSystemGroup
    {
        internal EndInitializationEntityCommandBufferSystem nextCommandBufferSystem;
        public EntityCommandBuffer CreateCommandBuffer => nextCommandBufferSystem.CreateCommandBuffer();
        public EntityCommandBufferSystem NextECBS => nextCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            nextCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    class BeforeTransformSystemGroup : ComponentSystemGroup
    {
        internal EndSimulationEntityCommandBufferSystem nextCommandBufferSystem;

        public EntityCommandBuffer CreateCommandBuffer => nextCommandBufferSystem.CreateCommandBuffer();
        public EntityCommandBufferSystem NextECBS => nextCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            nextCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    class AfterTransformSystemGroup : ComponentSystemGroup
    {
        internal EndSimulationEntityCommandBufferSystem nextCommandBufferSystem;

        public EntityCommandBuffer CreateCommandBuffer => nextCommandBufferSystem.CreateCommandBuffer();
        public EntityCommandBufferSystem NextECBS => nextCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            nextCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
    }

    /// <summary>
    /// Update in <see cref="PresentationSystemGroup"/> process entity/components after <see cref="EndSimulationEntityCommandBufferSystem"/> and <see cref="BeginPresentationEntityCommandBufferSystem"/>
    /// Next syncpoint <see cref="BeginInitializationEntityCommandBufferSystem"/>
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    class PostProcessSystemGroup : ComponentSystemGroup
    {
        
        internal BeginInitializationEntityCommandBufferSystem nextCommandBufferSystem;
        public EntityCommandBufferSystem NextECBS => nextCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            nextCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
    }
}
