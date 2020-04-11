/************************************************************************************
| File: Cache.cs                                                                    |
| Project: SRTK.Pool                                                                |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Mar 23 2020                                                    |
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
using System.Collections.Generic;
using System.Threading;

namespace SRTK.Pool
{
    using static MathX;
    using static CacheEx;

    /// <summary>
    /// /// Fast cache for small transitional obejcts.
    /// Can be used in multi-tread case.
    /// NOTE: ASSUMING OBJECT PASSED TO POOL IS NOT IN POOL (no repeating free on same object)
    /// Threading:
    ///     Intentionally not synchronize none-critial container operations to get better performance
    ///     In multi-tread case:
    ///     -may miss some freeing object to GC which is not critical
    ///     -may miss some just freeed object on allocate and create new object witch is also not critical
    ///     -will NOT allocate same instance as long as no repeating Free on same object
    /// </summary>
    /// <typeparam name="T">cache object type</typeparam>
    public class Cache<T> : ICache<T> where T : class
    {
        private struct Element { public T Value; }

        private readonly Func<T> _factory = null;
        private readonly Action<T> _restor = null;

        private T _firstItem;
        private readonly Element[] _items;

        /// <summary>
        /// Create a cache with a factory function that new() objects
        /// </summary>
        /// <param name="factory">factory function</param>
        public Cache(Func<T> factory, Action<T> restor = null) : this(factory, DefaultCacheCapacity, restor) { }

        /// <summary>
        /// Create a cache with a factory function that new() objects
        /// </summary>
        /// <param name="factory">factory function</param>
        /// <param name="capacity">total size of cache</param>
        public Cache(Func<T> factory, int capacity, Action<T> restor = null)
        {
            if (factory == null) throw new ArgumentNullException("factory is null");
            capacity = capacity.MinAt(0);
            _factory = factory;
            _restor = restor;
            _items = new Element[capacity];
        }

        internal T TryAlloc()
        {
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            T inst = _firstItem;
            if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
            {
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    // Note that the initial read is optimistically not synchronized. That is intentional. 
                    // We will interlock only when we have a candidate. in a worst case we may miss some
                    // recently returned objects. Not a big deal.
                    inst = _items[i].Value;
                    if (inst != null && inst == Interlocked.CompareExchange(ref _items[i].Value, null, inst))
                        break;
                }
            }
            return inst;
        }

        /// <summary>
        /// Get new object of type<see cref="T"/> from cache
        /// </summary>
        /// <returns>Allocate object</returns>
        public T Allocate()
        {
            T inst = TryAlloc();
            if (inst == null) inst = _factory();
            else if (_restor != null) _restor(inst);
            if (inst == null) throw new NullReferenceException("Factory returns null");
            return inst;
        }

        /// <summary>
        /// Return object to cache and stop using it
        /// </summary>
        /// <param name="item">returned object</param>
        public void Free(T item)
        {
            if (item == null) return;
            if (_firstItem == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem = item;
            }
            else
            {
                // searchIndex is used to track max freeing slot index
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    if (_items[i].Value == null)
                    {
                        // Intentionally not using interlocked here. 
                        // In a worst case scenario two objects may be stored into same slot.
                        // It is very unlikely to happen and will only mean that one of the objects will get collected.
                        _items[i].Value = item;
                        item = null;
                        break;
                    }
                }
            }
        }

        public void Free(ref T item)
        {
            Free(item);
            item = default(T);
        }


        /// <summary>
        /// number of cached objects in cache
        /// </summary>
        public int Useage
        {
            get
            {
                int counter = 0;
                if (_firstItem != null) counter++;
                int length = _items.Length;
                for (int i = 0; i < length; i++)
                {
                    if (_items[i].Value != null) counter++;
                }
                return counter;
            }
        }

        /// <summary>
        /// [0-1] ratio of used cache solts vs. cache Capacity
        /// </summary>
        public float UseageRate { get { return Useage / (float)(_items.Length + 1); } }

        public void Clear()
        {
            _firstItem = null;
            int length = _items.Length;
            for (int i = 0; i < length; i++) _items[i].Value = null;
        }
    }
}