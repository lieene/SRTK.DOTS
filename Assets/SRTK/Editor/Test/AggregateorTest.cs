using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SRTK;
// using SRTK.WarHead;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;

namespace Tests
{
    public class AggregatorTest
    {
        [Test]
        public void NativeAggregatorTest()
        {
        }

        [Test]
        public void AtomicAggregatorTest()
        {
            int forEachCount = 256;
            var aggregator = new AtomicIntAggregator(Allocator.TempJob, AggregationType.Sum, 0);
            Assert.AreEqual(0, aggregator.Value);
            var decadency = new AtomicIntAggregatorJob() { aggregator = aggregator }.Schedule(forEachCount, 1);
            decadency.Complete();
            aggregator.Evaluate();
            Debug.Log($"Sum of 0 to {forEachCount - 1} : {aggregator.Result}");
            Assert.AreEqual((forEachCount - 1) * (forEachCount >> 1), aggregator.Result);
            aggregator.Dispose();

            aggregator = new AtomicIntAggregator(Allocator.TempJob, AggregationType.Avg, 0);
            Assert.AreEqual(0, aggregator.Value);
            decadency = new AtomicIntAggregatorJob() { aggregator = aggregator }.Schedule(forEachCount, 1);
            decadency.Complete();
            aggregator.Evaluate();
            Debug.Log($"Avg of 0 to {forEachCount - 1} : {aggregator.Result}");
            Assert.AreEqual((forEachCount - 1) >> 1, aggregator.Result);
            aggregator.Dispose();

            aggregator = new AtomicIntAggregator(Allocator.TempJob, AggregationType.Max);
            Assert.AreEqual(int.MinValue, aggregator.Value);
            decadency = new AtomicIntAggregatorJob() { aggregator = aggregator }.Schedule(forEachCount, 1);
            decadency.Complete();
            aggregator.Evaluate();
            Debug.Log($"Max of 0 to {forEachCount - 1} : {aggregator.Result}");
            Assert.AreEqual(forEachCount - 1, aggregator.Result);

            aggregator.Reset();
            Debug.Log($"Max Aggregator Reset : {aggregator.Value}");
            Assert.AreEqual(int.MinValue, aggregator.Value);

            aggregator.Dispose();

            aggregator = new AtomicIntAggregator(Allocator.TempJob, AggregationType.Min);
            Assert.AreEqual(int.MaxValue, aggregator.Value);
            decadency = new AtomicIntAggregatorJob() { aggregator = aggregator }.Schedule(forEachCount, 1);
            decadency.Complete();
            aggregator.Evaluate();
            Debug.Log($"Min of 0 to {forEachCount - 1} : {aggregator.Result}");
            Assert.AreEqual(0, aggregator.Result);

            aggregator.Reset();
            Debug.Log($"Min Aggregator Reset : {aggregator.Value}");
            Assert.AreEqual(int.MaxValue, aggregator.Value);

            aggregator.Dispose();

        }

        public struct AtomicIntAggregatorJob : IJobParallelFor
        {
            public AtomicIntAggregator aggregator;
            [NativeSetThreadIndex] public int threadID;
            public void Execute(int index)
            {
                aggregator.Aggregate(index,out _);
                //JobLogger.Log("T:", threadID, " I: ", index, " ", aggregator.AggregationType);
            }
        }
    }
} 