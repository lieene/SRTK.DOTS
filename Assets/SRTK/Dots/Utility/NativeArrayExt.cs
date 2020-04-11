/************************************************************************************
| File: NativeArrayExt.cs                                                           |
| Project: lieene.Utility                                                           |
| Created Date: Wed Mar 25 2020                                                     |
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
| Date      	By	Comments                                                          |
| ----------	---	----------------------------------------------------------        |
************************************************************************************/

using System.Collections.Generic;
using System;
using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using SRTK.Utility;

namespace SRTK
{
    using static MathX;
    public static class NativeArrayExt
    {
        internal struct EquatableComparer<T> : IComparer<T> where T : IEquatable<T> { public int Compare(T x, T y) => x.Equals(y) ? 0 : 1; }

        internal struct ComparableComparer<T> : IComparer<T> where T : IComparable<T> { public int Compare(T x, T y) => x.CompareTo(y); }

        public static int BinarySearch<T>(this NativeArray<T> array, in T value, in IComparer<T> comp) where T : struct
        {
            int hi = array.Length - 1;
            if (array.Length < 0) return ~0;
            int lo = 0;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int c = comp.Compare(array[i], value);
                if (c == 0) return i;
                if (c < 0) lo = i + 1; else hi = i - 1;
            }
            return ~lo;
        }

        public static int BinarySearch<T>(this NativeArray<T> array, T value) where T : struct, IComparable<T>
            => array.BinarySearch<T>(value, new ComparableComparer<T>());

        public static int BinarySearch<T>(this NativeList<T> list, T value) where T : struct, IComparable<T>
            => list.AsArray().BinarySearch(value);

        public static int BinarySearch<T>(this NativeList<T> list, in T value, in IComparer<T> comp) where T : struct
            => list.AsArray().BinarySearch(value, comp);

        public static void Sort<T>(this NativeList<T> list) where T : struct, IComparable<T> => list.AsArray().Sort();

        public static void Sort<T>(this NativeList<T> list, in IComparer<T> comp) where T : struct => list.AsArray().Sort(comp);

        internal enum GroupSearchState
        {
            GroupInitialize,
            Search1stNoneGroupValueAtferProcessedGroupRange,
            Search1stGroupValueAfterNoneGroupValues,
            SearchLastGroupValueToSwapBack,
        }

        /// <summary>
        /// Group equal value together(group order is not consistent) and return group count
        /// </summary>
        /// <param name="array">array to group</param>
        /// <typeparam name="T">value type</typeparam>
        /// <param name="comp">equality tester</param>
        /// <returns>group count</returns>
        public static int Group<T>(this NativeArray<T> array, IComparer<T> comp) where T : struct
        {
            var len = array.Length;
            if (len < 2) return len;

            int groupCount = 0;
            int position = 0;
            //int currentGroupRangeEnd = 0;
            int replaceIndex = 0;
            var state = GroupSearchState.GroupInitialize;
            T groupValue = default;
            while (true)
            {
                switch (state)
                {
                    case GroupSearchState.GroupInitialize:
                        //[...5 5 3 3 3 3 3 1 ...]
                        // previous groups^ ^we are here
                        groupCount++;
                        groupValue = array[position];
                        //currentGroupRangeEnd = position;
                        state = GroupSearchState.Search1stNoneGroupValueAtferProcessedGroupRange;
                        position++;

                        //should never happen if forward check is done properly
                        //cases with array length smaller then 2 is processed
                        Assert.IsTrue(position < len);
                        break;
                    case GroupSearchState.Search1stNoneGroupValueAtferProcessedGroupRange:
                        //search for first none group value from group offset, previous elements are okay to keep
                        //               |<---we are in this range --->|
                        //[...3 3 3 3 3 3 1 1 1 1 1 1 1 1 1 1 1 1 1 1 2 ...]
                        //previous group^            seaching for this^and switch to Search1stGroupValueAfterNoneGroupValues
                        if (comp.Compare(groupValue, array[position]) != 0)
                        {
                            //[...3 3 3 3 3 3 1 1 1 1 1 1 1 2 ...]
                            //previous group^|^group start  ^we are here
                            replaceIndex = position;
                            state = GroupSearchState.Search1stGroupValueAfterNoneGroupValues;
                            position++;
                            //forward check
                            if (position >= len)
                            {
                                //[...3 3 3 3 3 3 1 1 1 1 1 1 1 2]<=now we are at the end
                                //previous group^|^group start  ^we were here
                                //one last group to go
                                return ++groupCount;
                            }
                        }
                        else
                        {
                            position++;//keep searching
                                       //forward check
                            if (position >= len)
                            {
                                //[...3 3 3 3 3 3 1 1 1 1 1 1 1 1]<=now we are at the end
                                //previous group^|^group start  ^we were here
                                //all done
                                return groupCount;
                            }
                        }
                        break;
                    case GroupSearchState.Search1stGroupValueAfterNoneGroupValues:
                        //             |<---we are in this range --->|
                        //[...1 1 1 1 2 4 3 5 7 6 3 8 9 2 3 4 5 2 6 1 ...]
                        //replace this^            seaching for this^and switch to SearchLastGroupValueToSwapBack
                        if (comp.Compare(groupValue, array[position]) == 0)
                        {
                            state = GroupSearchState.SearchLastGroupValueToSwapBack;
                            position++;
                            //forward check
                            if (position >= len)
                            {
                                //end of array found
                                //[...1 1 1 1 9 ...no group values... 1]<= we are at the end now
                                //replace this^                       ^we were here
                                //swap target back and start a new group, no more group value will be found
                                var temp = array[--position];
                                array[position] = array[replaceIndex];
                                array[replaceIndex] = temp;
                                //now we are one step back on position

                                //if (replaceIndex + 1 >= len) no way, as we have been there

                                if ((replaceIndex + 2) >= len)//new location forward check
                                {
                                    //[...1 1 1 1 1 1 9]<= we were at the end
                                    // replaced this^ ^we are here
                                    //  go back here^is not need
                                    //one last group to go
                                    return ++groupCount;
                                }
                                else
                                {
                                    // [...1 1 1 1 1 3 4 2 5 8 7 3 5 5 5 9 9]<= we were at the end
                                    //replaced this^                       ^we are here
                                    //  go back here^
                                    position = replaceIndex + 1;
                                    //there are still other groups to process
                                    state = GroupSearchState.GroupInitialize;
                                }
                            }
                        }
                        else
                        {
                            position++;//keep searching
                                       //forward check
                            if (position >= len)
                            {
                                //if (replaceIndex + 1 >= len) no way, as we have been there

                                //if (replaceIndex + 2 >= len)
                                // [...1 1 1 1 9 7]<=now we are at the end
                                //replace index^ ^we were here
                                //let GroupInitialize handle this case

                                // [...1 1 1 1 9 3 4 2 5 8 7 3 5 5 5 9 7]<=now we are at the end
                                //replace index^to go back             ^we were here
                                //cancel swap start new group
                                state = GroupSearchState.GroupInitialize;
                                position = replaceIndex;
                            }
                        }
                        break;
                    case GroupSearchState.SearchLastGroupValueToSwapBack:
                        //                      |<---we are in this range --->|
                        //[... 1 2 4 3 5 7 6 3 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 3 ...]
                        //replace^  not by this^                but by this^
                        //  go back^here
                        if (position >= len || comp.Compare(groupValue, array[position]) != 0)
                        {
                            //Case 1
                            //[... 1 2 4 3 1 1 1 1 1 1 1 3 ...]
                            //replace^        we are here^
                            //  go back^here

                            //Case 2
                            //[... 1 2 4 3 1 1 1 1 1 1 1]<=now we are at the end
                            //replace^     we were here^
                            //  go back^here

                            var temp = array[--position];
                            array[position] = array[replaceIndex];
                            array[replaceIndex] = temp;
                            position = replaceIndex + 1;
                            state = GroupSearchState.Search1stNoneGroupValueAtferProcessedGroupRange;
                            //no forward check as we are going back to a safe position
                        }
                        else position++;//keep searching
                        break;
                }
            }
        }

        /// <summary>
        /// Group equal value together(group order is not consistent) and return group count
        /// </summary>
        /// <param name="array">array to group</param>
        /// <typeparam name="T">value type</typeparam>
        /// <returns>group count</returns>
        public static int Group<T>(this NativeArray<T> array) where T : struct, IEquatable<T>
            => array.Group(new EquatableComparer<T>());

        /// <summary>
        /// Group equal value together(group order is not consistent) and return group count
        /// </summary>
        /// <param name="list">list to group</param>
        /// <typeparam name="T">value type</typeparam>
        /// <param name="comp">equality tester</param>
        /// <returns>group count</returns>
        public static int Group<T>(this NativeList<T> list, IComparer<T> comp) where T : struct
            => list.AsArray().Group(comp);

        /// <summary>
        /// Group equal value together(group order is not consistent) and return group count
        /// </summary>
        /// <param name="list">list to group</param>
        /// <typeparam name="T">value type</typeparam>
        /// <returns>group count</returns>
        public static int Group<T>(this NativeList<T> list) where T : struct, IEquatable<T>
            => list.AsArray().Group();

        /// <summary>
        /// Get group ranges from grouped array
        /// </summary>
        /// <param name="array">grouped array</param>
        /// <param name="groupCount">group count</param>
        /// <param name="comp">equality tester</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> RangeOfGrouped<T>(this NativeArray<T> array, int groupCount, IComparer<T> comp, Allocator allocator) where T : struct
        {
            if (groupCount == 0) return new NativeArray<Range>(0, allocator);
            var ranges = new NativeArray<Range>(groupCount, allocator);
            if (groupCount == 1)
            {
                ranges[0] = new Range(0, 1);
                return ranges;
            }
            Range range = new Range(0, 0);
            T value = array[0];
            var arrayLen = array.Length;
            var gIdx = 0;
            for (int i = 1; i < arrayLen; i++)
            {
                var cur = array[i];
                if (comp.Compare(value, cur) != 0)
                {
                    Assert.IsTrue(gIdx < groupCount);
                    range.End = i;
                    ranges[gIdx++] = range;
                    range.Start = i;
                    value = cur;
                }
            }
            Assert.IsTrue((gIdx + 1) == groupCount);
            range.End = arrayLen;
            ranges[gIdx] = range;
            return ranges;
        }

        /// <summary>
        /// Get group ranges from grouped array
        /// </summary>
        /// <param name="array">grouped array</param>
        /// <param name="groupCount">group count</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> RangeOfGrouped<T>(this NativeArray<T> array, int groupCount, Allocator allocator) where T : struct, IEquatable<T>
            => array.RangeOfGrouped(groupCount, new EquatableComparer<T>(), allocator);

        /// <summary>
        /// Get group ranges from grouped list
        /// </summary>
        /// <param name="list">grouped list</param>
        /// <param name="groupCount">group count</param>
        /// <param name="comp">equality tester</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> RangeOfGrouped<T>(this NativeList<T> list, int groupCount, IComparer<T> comp, Allocator allocator) where T : struct
            => list.AsArray().RangeOfGrouped(groupCount, comp, allocator);

        /// <summary>
        /// Get group ranges from grouped list
        /// </summary>
        /// <param name="list">grouped list</param>
        /// <param name="groupCount">group count</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> RangeOfGrouped<T>(this NativeList<T> list, int groupCount, Allocator allocator) where T : struct, IEquatable<T>
            => list.AsArray().RangeOfGrouped(groupCount, new EquatableComparer<T>(), allocator);

        /// <summary>
        /// Group an array and get group ranges
        /// </summary>
        /// <param name="array">array to group</param>
        /// <param name="groupCount">group count</param>
        /// <param name="comp">equality tester</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> GroupRange<T>(this NativeArray<T> array, IComparer<T> comp, Allocator allocator) where T : struct
        {
            var groupCount = array.Group(comp);
            return array.RangeOfGrouped(groupCount, comp, allocator);
        }

        /// <summary>
        /// Group an array and get group ranges
        /// </summary>
        /// <param name="array">array to group</param>
        /// <param name="groupCount">group count</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> GroupRange<T>(this NativeArray<T> array, Allocator allocator) where T : struct, IEquatable<T>
            => array.GroupRange(new EquatableComparer<T>(), allocator);

        /// <summary>
        /// Group an list and get group ranges
        /// </summary>
        /// <param name="list">list to group</param>
        /// <param name="groupCount">group count</param>
        /// <param name="comp">equality tester</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> GroupRange<T>(this NativeList<T> list, IComparer<T> comp, Allocator allocator) where T : struct
            => list.AsArray().GroupRange(comp, allocator);


        /// <summary>
        /// Group an list and get group ranges
        /// </summary>
        /// <param name="list">list to group</param>
        /// <param name="groupCount">group count</param>
        /// <param name="allocator">range container allocation type</param>
        /// <returns>NativeArray of group ranges</returns>
        public static NativeArray<Range> GroupRange<T>(this NativeList<T> list, Allocator allocator) where T : struct, IEquatable<T>
            => list.AsArray().GroupRange(new EquatableComparer<T>(), allocator);


    }
}