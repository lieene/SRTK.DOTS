/************************************************************************************
| File: LocalTestSystem.cs                                                          |
| Project: lieene.SRTK                                                              |
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
    //[DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(BeforeTransformSystemGroup))]
    public class LocalTestSystem : SystemBase
    {
        TimeSystem timeSystem;
        Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            timeSystem = World.GetOrCreateSystem<TimeSystem>();
            random = new Unity.Mathematics.Random((uint)32746);
        }

        protected override void OnUpdate()
        {
            TestTimeSystem();
        }

        void TestTimeSystem()
        {
            // Test Data---------------------------------------------
            int batchCount = 0;
            if (Input.GetKeyDown(KeyCode.BackQuote)) batchCount = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha1)) batchCount = 10;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) batchCount = 100;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) batchCount = 1000;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) batchCount = 10000;
            else if (Input.GetKeyDown(KeyCode.Alpha5)) batchCount = 100000;
            else if (Input.GetKeyDown(KeyCode.Alpha6)) batchCount = 1000000;
            else if (Input.GetKey(KeyCode.Space)) batchCount = 100;
            if (batchCount > 0)
            {
                if (Input.GetKey(KeyCode.X))//remove
                {
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(),typeof(TimeScale),typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    if (_allTime.Length > 0)
                    {
                        batchCount = math.min(_allTime.Length, batchCount);
                        var editor = timeSystem.CreateTimeEditor();
                        var remainingIndexes = new NativeList<int>(_allTime.Length, Allocator.Temp);
                        for (int i = 0; i < _allTime.Length; i++) remainingIndexes.Add(i);
                        do
                        {
                            var n = random.NextInt(remainingIndexes.Length - 1);
                            
                            editor.ECB.DestroyEntity(_allTime[remainingIndexes[n]]);

                            remainingIndexes.RemoveAtSwapBack(n);
                        }
                        while (--batchCount > 0);
                        remainingIndexes.Dispose();
                    }
                    _allTime.Dispose();
                }
                else if (Input.GetKey(KeyCode.Z))//change parent
                {
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(), typeof(TimeScale), typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    batchCount = batchCount < 2 ? 2 : batchCount;
                    if (_allTime.Length >= 2)
                    {
                        batchCount = math.min(_allTime.Length, batchCount);
                        var editor = timeSystem.CreateTimeEditor();
                        var remainingIndexes = new NativeList<int>(_allTime.Length, Allocator.Temp);
                        for (int i = 0; i < _allTime.Length; i++) remainingIndexes.Add(i);
                        do
                        {
                            var childIdx = random.NextInt(remainingIndexes.Length);
                            var child = _allTime[remainingIndexes[childIdx]];
                            remainingIndexes.RemoveAtSwapBack(childIdx);
                            var parent = _allTime[remainingIndexes[random.NextInt(remainingIndexes.Length)]];
                            editor.SetParent(child, parent);
                        }
                        while (--batchCount > 1);
                        remainingIndexes.Dispose();
                    }
                    _allTime.Dispose();
                }
                else if (Input.GetKey(KeyCode.E))//edit time
                {
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(), typeof(TimeScale), typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    if (_allTime.Length > 0)
                    {
                        batchCount = math.min(_allTime.Length, batchCount);
                        var editor = timeSystem.CreateTimeEditor();
                        var template = editor.EditTime(_allTime[0]).AddElapsedTime().SetTimeScale(.01f);
                        var remainingIndexes = new NativeList<int>(_allTime.Length, Allocator.Temp);
                        for (int i = 0; i < _allTime.Length; i++) remainingIndexes.Add(i);
                        do
                        {
                            var n = random.NextInt(remainingIndexes.Length);
                            template.WithEntity(_allTime[remainingIndexes[n]]).ApplyBuffered(editor);
                            remainingIndexes.RemoveAtSwapBack(n);
                        }
                        while (--batchCount > 0);
                        remainingIndexes.Dispose();
                    }
                    _allTime.Dispose();
                }
                else if (Input.GetKey(KeyCode.R))//clear parent
                {
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(), typeof(TimeScale), typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    if (_allTime.Length > 0)
                    {
                        batchCount = math.min(_allTime.Length, batchCount);
                        var editor = timeSystem.CreateTimeEditor();
                        var remainingIndexes = new NativeList<int>(_allTime.Length, Allocator.Temp);
                        for (int i = 0; i < _allTime.Length; i++) remainingIndexes.Add(i);
                        do
                        {
                            var n = random.NextInt(remainingIndexes.Length);
                            editor.ClearParent(_allTime[remainingIndexes[n]]);
                            remainingIndexes.RemoveAtSwapBack(n);
                        }
                        while (--batchCount > 0);
                        remainingIndexes.Dispose();
                    }
                    _allTime.Dispose();
                }
                else if (Input.GetKey(KeyCode.C))//mixed action
                {
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(), typeof(TimeScale), typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    if (_allTime.Length > 0)
                    {
                        batchCount = math.min(_allTime.Length, batchCount);
                        var editor = timeSystem.CreateTimeEditor();
                        var remainingIndexes = new NativeList<int>(_allTime.Length, Allocator.Temp);
                        for (int i = 0; i < _allTime.Length; i++) remainingIndexes.Add(i);
                        do
                        {
                            var n = remainingIndexes[random.NextInt(remainingIndexes.Length)];
                            var target = _allTime[remainingIndexes[n]];
                            remainingIndexes.RemoveAtSwapBack(n);
                            if (random.NextBool())//remove time
                            {
                                editor.ECB.DestroyEntity(target);
                            }
                            else
                            {
                                if (random.NextBool())//parent change
                                {
                                    if (random.NextBool() && batchCount >= 2)//set as child
                                    {
                                        var parent = _allTime[remainingIndexes[random.NextInt(remainingIndexes.Length)]];
                                        editor.SetParent(target, parent);
                                    }
                                    else //set as root
                                    {
                                        editor.ClearParent(target);
                                    }
                                }
                                if (random.NextBool())
                                {
                                    editor.EditTime(target).SetTimeScale(0.01f).ApplyBuffered(editor);
                                }
                            }
                        }
                        while (--batchCount > 0);
                        remainingIndexes.Dispose();
                    }
                    _allTime.Dispose();
                }
                else//create time
                {
                    Entity parent = Entity.Null;
                    var _allTime = GetEntityQuery(ComponentType.ReadOnly<ElapsedTime>(), typeof(TimeScale), typeof(LocalTimeScale)).ToEntityArrayAsync(Allocator.TempJob, out var _allTimeJob);
                    _allTimeJob.Complete();
                    var cnt = _allTime.Length;
                    if (cnt > 0)
                    {
                        var pid = random.NextInt(cnt);
                        parent = _allTime[pid];
                    }
                    _allTime.Dispose();

                    var editor = timeSystem.CreateTimeEditor();

                    var template = editor.CreateTime().AddElapsedTime().SetTimeScale(2f*random.NextInt(1,3),true);
                    var first = template.ApplyBuffered(editor);
                    if (parent != Entity.Null) editor.SetParent(first, parent);
                    for (int i = 1; i < batchCount; i++)
                    {
                        var cld = template.ApplyBuffered(editor);
                        editor.SetParent(cld, first);
                    }
                }
            }
            // Test Data---------------------------------------------
            if (Input.GetKeyDown(KeyCode.P))
            { TimeHierarchyCheck(); }
        }
        void TimeHierarchyCheck()
        {
            bool isOkay = true;
            Entities.ForEach((Entity p, DynamicBuffer<ChildTime> pc) =>
            {
                for (int i = 0, count = pc.Length; i < count; i++)
                {
                    var c = pc[i].Value;
                    if (HasComponent<ParentTime>(c))
                    {
                        var pp = GetComponent<ParentTime>(c).Value;
                        if (p != pp)
                        {
                            Debug.LogError($"Miss match! From Parent[{p.Index}|{p.Version}] found Child[{c.Index}|{c.Version}] with different Parent[{pp.Index}|{pp.Version}]");
                            isOkay = false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Miss match! From Parent[{p.Index}|{p.Version}] found Child[{c.Index}|{c.Version}] with no Parent");
                        isOkay = false;
                    }
                }
            }).WithoutBurst().Run();

            Entities.ForEach((Entity c, ParentTime cp) =>
            {
                var p = cp.Value;
                if (EntityManager.Exists(p))
                {
                    if (EntityManager.HasComponent<ChildTime>(p))
                    {
                        var b = EntityManager.GetBuffer<ChildTime>(p);
                        var idx = b.FindFirstElement(new ChildTime() { Value = c });
                        if (idx < 0)
                        {
                            Debug.LogError($"Miss match! From Child[{c.Index}|{c.Version}] found Parent[{p.Index}|{p.Version}] which dont have this child");
                            isOkay = false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Miss match! From Child[{c.Index}|{c.Version}] found Parent[{p.Index}|{p.Version}] which dont have child buffer");
                        isOkay = false;
                    }
                }
                else
                {
                    Debug.LogError($"Miss match! From Child[{c.Index}|{c.Version}] found Parent[{p.Index}|{p.Version}] which is Destroyed");
                    isOkay = false;
                }
            }).WithoutBurst().Run();

            if (isOkay) Debug.Log("Parent and Children Matched");
        }

    }
}