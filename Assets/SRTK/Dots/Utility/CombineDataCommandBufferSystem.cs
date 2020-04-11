/************************************************************************************
| File: CombineDataCommandBufferSystem.cs                                           |
| Project: lieene.ECBPlus                                                           |
| Created Date: Mon Mar 9 2020                                                      |
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

using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;

namespace SRTK
{
    public interface IEntityManagerPlayBack { void PlayBack(EntityManager em, DeferEntityAccessor accessor); }
    public interface ICombineAble<T> { T CombineWith(T prev); }

    public abstract class CombineDataCommandBufferSystem<T> : SystemBase
        where T : unmanaged, IComponentData, ICombineAble<T>
    {
        internal struct CombainComponentCommand : IEntityManagerPlayBack
        {
            public DeferEntity target;
            public T data;
            public void PlayBack(EntityManager em, DeferEntityAccessor accessor)
            {
                var e = target.ecbPlaceHolderEntity;
                if (e.Index <= 0) e = accessor.GetDeferEntity(target.DeferID);
                if (target.ecbPlaceHolderEntity.Index >= 0)
                {
                    if (em.Exists(e))
                    {
                        if (em.HasComponent<T>(e))
                        {
                            var prev = em.GetComponentData<T>(e);
                            em.SetComponentData(e, data.CombineWith(prev));
                        }
                        else em.AddComponentData(e, data);
                    }
                }
            }
        }

        public struct CommandBuffer
        {
            internal NativeQueue<CombainComponentCommand> commands;

            public void AddOrCombineComponent(Entity entity, T data)
            {
                commands.Enqueue(new CombainComponentCommand()
                { target = new DeferEntity(1, entity), data = data });
            }

            public void AddOrCombineComponent(DeferEntity entity, T data)
            {
                commands.Enqueue(new CombainComponentCommand()
                { target = entity, data = data });
            }

            public Concurrent ToConcurrent() => new Concurrent() { commands = commands.AsParallelWriter() };

            public struct Concurrent
            {
                internal NativeQueue<CombainComponentCommand>.ParallelWriter commands;

                public void AddOrCombineComponent(Entity entity, T data)
                {
                    commands.Enqueue(new CombainComponentCommand()
                    { target = new DeferEntity(1, entity), data = data });
                }

                public void AddOrCombineComponent(DeferEntity entity, T data)
                {
                    commands.Enqueue(new CombainComponentCommand()
                    { target = entity, data = data });
                }
            }
        }

        internal NativeQueue<CombainComponentCommand> commands;
        internal DeferEntitySystem des;

        public CommandBuffer GetCommandBuffer() => new CommandBuffer() { commands = commands };

        public void AddWorkerDependency(JobHandle Dep) => Dependency = JobHandle.CombineDependencies(Dep, this.Dependency);

        protected override void OnCreate()
        {
            commands = new NativeQueue<CombainComponentCommand>(Allocator.Persistent);
            des = World.GetOrCreateSystem<DeferEntitySystem>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            if (commands.Count > 0)
            {
                var accessor = des.GetAccessor();
                do
                {
                    var cmd = commands.Dequeue();
                    cmd.PlayBack(EntityManager, accessor);
                }
                while (commands.Count > 0);
            }
            Dependency = default;
        }

        override protected void OnDestroy()=>commands.Dispose();
    }
}
