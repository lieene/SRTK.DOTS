/************************************************************************************
| File: U_PoolEx.cs                                                                 |
| Project: SRTK.Pool                                                                |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Oct 14 2019                                                    |
| Modified By: Lieene Guo                                                           |
| -----                                                                             |
| MIT License                                                                       |
|                                                                                   |
| Copyright (c) 2019 Lieene@ShadeRealm                                              |
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
using System.Collections;
using System.Collections.Generic;

namespace SRTK.Pool
{
    using static MathX;
    using static PoolEx;
    /// <summary>
    /// Common Object pool, with param in constructor controls thread safty and reference safty.
    /// cirtical conditions are checked, in-pool object and allocated object are guarantee to be unique.
    /// Internal Debug reports: all are solved internally so these are not critical.
    ///     -Freeing in-pool object         | solution: drop to GC  | chance: depend on user
    /// </summary>
    /// <exception cref="ArgumentNullException"> On Constructor if factory function is null</exception>
    /// <exception cref="ArgumentNullException"> On Allocate if factory function returns null</exception>
    /// <typeparam name="T">referenced object type</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly int allocbufferSize = Environment.ProcessorCount * 2;
        // callback to create new instance (like new(...))
        private readonly Func<T> _factory;

        // objects in the pool for later allocate
        private readonly Stack _freeInstances;

        // object allocated by pool and not freed yet
        private readonly HashSet<T> _allocatedInstances;

        // max pool storage
        private readonly int _capacity;

        /// <summary>
        /// max pool storage
        /// </summary>
        public int Capacity
        {
            get
            {
                if (_synchronized)
                    return _capacity + allocbufferSize;
                else return _capacity;
            }
        }

        // Is this pool Synchronized (thread safe)
        private readonly bool _synchronized;

        /// <summary>
        /// Is this pool Synchronized (thread safe)
        /// </summary>
        public bool IsSynchronized { get { return _synchronized; } }

        // dose this pool auto prevent invlid free 
        private readonly bool _trackAlloc;

        /// <summary>
        /// Dose this pool auto prevent invlid free 
        /// </summary>
        public bool IsFreeSafe { get { return _trackAlloc; } }

        /// <summary>
        /// number of cached objects in pool
        /// </summary>
        public int Useage { get { return _freeInstances.Count; } }

        /// <summary>
        /// [0-1] ratio of used pool solts vs. pool size
        /// </summary>
        public float UseageRate { get { return _freeInstances.Count / (float)_capacity; } }

        /// <summary>
        /// number of allocated objects which are not returned to pool yet
        /// </summary>
        public int AllocationCount { get { return _allocatedInstances.Count; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory">user defined customefunction acts as new(), value must not null, must not return null on call</param>
        /// <param name="capacity">expected size of this pool, actual pool size with bigger as there some buffer</param>
        /// <param name="sync">should this pool be synchronized (thread safe)</param>
        /// <param name="trackAlloc">should this pool make sure no multi-free on same object.
        /// So there will not be any identical object in the pool.
        /// Set this to False when you are sure that there are no multi-free, to make pool run faster</param>
        public ObjectPool(Func<T> factory, int capacity, bool sync, bool trackAlloc = true)
        {
            if (factory == null) throw new ArgumentNullException("factory is null");
            _capacity = Max(capacity, MinObjectPoolCapacity);

            _factory = factory;
            _synchronized = sync;
            _trackAlloc = trackAlloc;

            // Environment.ProcessorCount * 2 could help reduce stack internal space re-allocation
            // by give 1 slot of buffer for each thread and this is the max concurrent waiting thread there can be
            if (_synchronized) _freeInstances = Stack.Synchronized(new Stack(_capacity + allocbufferSize));
            else _freeInstances = new Stack(_capacity);

            _allocatedInstances = new HashSet<T>();
        }

        /// <summary>
        /// Get a new object reference from pool
        /// </summary>
        /// <returns>object reference allocated</returns>
        public T Allocate()
        {
            T inst = null;

            // pre-check to minmum chance of try catch 
            if (_freeInstances.Count > 0)
            {
                try { inst = _freeInstances.Pop() as T; }
                // _freeInstances is empty bacause some other thread Pop first
                // this is vary rare case as Count test and Pop() is very short
                // leave this case to factory...
                catch (InvalidOperationException) { }
            }

            if (inst == null)
            {
                inst = _factory();
                if (inst == null) throw new NullReferenceException("Factory returns null");
            }

            if (_trackAlloc)
            {
                //allocation tracing
                if (_synchronized) lock (_allocatedInstances) _allocatedInstances.Add(inst);
                else _allocatedInstances.Add(inst);
            }

            return inst;
        }


        /// <summary>
        /// return object reference to pool
        /// </summary>
        /// <param name="item">object reference to free</param>
        public void Free(T item)
        {
            if (item == null) return;

            if (_trackAlloc)
            {
                // Pre-Free check
                // Only objects allocated by this pool which is not currentlly in pool can be freed
                // Or following allocate may return identical object which is critically unsafe
                if (_synchronized)
                {
                    lock (_allocatedInstances)
                    {
                        if (_allocatedInstances.Contains(item)) _allocatedInstances.Remove(item);
                        else throw new InvalidOperationException("Freeing invalid(pooled / not allocated by pool) object.");
                    }
                }
                else
                {
                    if (_allocatedInstances.Contains(item)) _allocatedInstances.Remove(item);
                    else throw new InvalidOperationException("Freeing invalid(pooled / not allocated by pool) object.");
                }
            }

            if (_freeInstances.Count < _capacity)
            {
                // Size check and push intentional not synced. push operation is synced on need.
                // Un-synced Count+push may add objects more than size to stack.
                // But this is unlikely to happen as _pool.Count getter is super fast.
                // Let's say it happend....
                // Going over stack size will tirgger space re-allocation which could use a bit of time.
                // but with {Environment.ProcessorCount * 2;} buffer that is EXTREMELY unlikely to happen
                // Environment.ProcessorCount * 2 give 1 slot of buffer for each thread.
                // that it all concurrent thread there is. There should not be more call to Push.
                // No big deal event on re-allocation.
                _freeInstances.Push(item);
            }
        }
        
        public void Free(ref T item)
        {
            Free(item);
            item = default(T);
        }


        // readonly int _typeHash = typeof(T).GetTypeHash();
        // public int GetTypeHash() => _typeHash;
        // public int GetPoolHash() => PoolEx.ObjectPoolHash;
    }
}