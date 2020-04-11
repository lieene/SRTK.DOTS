using System.Runtime.InteropServices;
/************************************************************************************
| File: InstanceID.cs                                                               |
| Project: SRTK.AlgorithmX                                                          |
| Created Date: Tue Sep 17 2019                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Sep 24 2019                                                    |
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
using System.Threading;
using System.Threading.Tasks;

namespace SRTK
{
    public struct InstanceID
    {
        public int IID;
        public static implicit operator int(InstanceID id) => id.IID;
        public static implicit operator InstanceID(int id) => new InstanceID { IID = id };
        public override string ToString() => $"InstanceID[{IID}]";
    }

    public static class InstanceX
    {
        public const int InitCapacity = 16;
        public const int InitID = 1;

        public static async void ClearArray(Array a)
            => await Task.Run(() => { if (a != null) Array.Clear(a, 0, a.Length); });

        //-------------------------------------------------------------------------------------
        #region Global InstanceID
        internal struct TypeInsts<T> where T : class
        {
            public static object locker = new object();
            public static T[] Instances = new T[InitCapacity];
            public static int curInstanceID = InitID;
        }

        public static InstanceID AddInstance<T>(this T inst) where T : class
        {
            if (inst == null) throw new NullReferenceException("Adding null instance");
            if (TypeInsts<T>.curInstanceID == int.MaxValue)
                throw new OverflowException("InstanceID out range int");
            int id;
            T[] toClear = null;
            lock (TypeInsts<T>.locker)
            {
                id = TypeInsts<T>.curInstanceID++;
                if (TypeInsts<T>.Instances == null || TypeInsts<T>.Instances.Length <= id)
                {
                    var sizex2 = new T[TypeInsts<T>.Instances.Length << 1];
                    Array.Copy(TypeInsts<T>.Instances, sizex2, TypeInsts<T>.Instances.Length);
                    toClear = TypeInsts<T>.Instances;
                    TypeInsts<T>.Instances = sizex2;
                }
                TypeInsts<T>.Instances[id] = inst;
            }
            ClearArray(toClear);
            return id;
        }

        public static T GetInstance<T>(this InstanceID instID) where T : class
        {
            if (instID <= 0) return null;
            T inst;
            lock (TypeInsts<T>.locker)
            { inst = TypeInsts<T>.Instances[instID]; }
            return inst;
        }

        public static T RemoveInstance<T>(this InstanceID instID) where T : class
        {
            if (instID <= 0) return null;
            T inst;
            lock (TypeInsts<T>.locker)
            {
                inst = TypeInsts<T>.Instances[instID];
                TypeInsts<T>.Instances[instID] = null;
            }
            return inst;
        }

        public static void Clear<T>() where T : class
        {
            lock (TypeInsts<T>.locker)
            {
                Array.Clear(TypeInsts<T>.Instances, 0, TypeInsts<T>.Instances.Length);
                TypeInsts<T>.curInstanceID = InitID;
            }
        }

        #endregion Global InstanceID
        //-------------------------------------------------------------------------------------
        #region ThreadLocal Transient InstanceID

        internal class ObjInsts
        {
            public ObjInsts()
            {
                Insts = new object[InitCapacity];
                curIID = InitID;
            }
            public object[] Insts;
            public int curIID;
        }

        internal static readonly ThreadLocal<ObjInsts> TLTI
            = new ThreadLocal<ObjInsts>(() => new ObjInsts());

        public static InstanceID AddTransient<T>(this T inst) where T : class
        {
            if (inst == null) throw new NullReferenceException("Adding null instance");
            if (TLTI.Value.curIID == int.MaxValue)
                throw new OverflowException("InstanceID out range int");

            int id = TLTI.Value.curIID++;
            if (TLTI.Value.Insts == null || TLTI.Value.Insts.Length <= id)
            {
                var sizex2 = new object[TLTI.Value.Insts.Length << 1];
                Array.Copy(TLTI.Value.Insts, sizex2, TLTI.Value.Insts.Length);
                ClearArray(TLTI.Value.Insts);
                TLTI.Value.Insts = sizex2;
            }
            TLTI.Value.Insts[id] = inst;
            return id;
        }

        public static T GetTransient<T>(this InstanceID instID) where T : class
        {
            if (instID <= 0) return null;
            return TLTI.Value.Insts[instID] as T;
        }

        public static T RemoveTransient<T>(this InstanceID instID) where T : class
        {
            if (instID <= 0) return null;
            T inst = TLTI.Value.Insts[instID] as T;
            TLTI.Value.Insts[instID] = null;
            return inst;
        }

        public static void ClearTransient()
        {
            Array.Clear(TLTI.Value.Insts, 0, TLTI.Value.Insts.Length);
            TLTI.Value.curIID = InitID;
        }
    }
    #endregion TempInstanceID
    //-------------------------------------------------------------------------------------
}