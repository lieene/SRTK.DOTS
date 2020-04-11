/************************************************************************************
| File: EntityManagerExt.cs                                                         |
| Project: lieene.Utility                                                           |
| Created Date: Fri Mar 6 2020                                                      |
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
using Unity.Entities;
using Unity.Collections;

namespace SRTK
{
    public static class EntityManagerExt
    {
        static class SCDListCache<T> where T : unmanaged, ISharedComponentData
        {
            static List<T> scdList = new List<T>();
            public static List<T> ListInstance => scdList;
        }
        static List<int> scdIdList = new List<int>();

        public static NativeArray<(T data, int index)> GetAllUniqueSharedComponentNativeTuple<T>(this EntityManager em, Allocator allocator)
            where T : unmanaged, ISharedComponentData
        {
            var scdList = SCDListCache<T>.ListInstance;
            scdList.Clear(); scdIdList.Clear();
            em.GetAllUniqueSharedComponentData<T>(scdList, scdIdList);
            var len = scdIdList.Count;
            var native = new NativeArray<(T, int)>(len, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < len; i++) native[i] = (scdList[i], scdIdList[i]);
            return native;
        }

        public static (NativeArray<T> data, NativeArray<int> index) GetAllUniqueSharedComponentNative<T>(this EntityManager em, Allocator allocator)
            where T : unmanaged, ISharedComponentData
        {
            var scdList = SCDListCache<T>.ListInstance;
            scdList.Clear(); scdIdList.Clear();
            em.GetAllUniqueSharedComponentData<T>(scdList, scdIdList);
            var len = scdIdList.Count;
            var nativeData = new NativeArray<T>(len, allocator, NativeArrayOptions.UninitializedMemory);
            var nativeIndex = new NativeArray<int>(len, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < len; i++) { nativeData[i] = scdList[i]; nativeIndex[i] = scdIdList[i]; }
            return (nativeData, nativeIndex);
        }

        public static NativeArray<T> GetAllUniqueSharedComponentNativeData<T>(this EntityManager em, Allocator allocator)
            where T : unmanaged, ISharedComponentData
        {
            var scdList = SCDListCache<T>.ListInstance;
            scdList.Clear(); scdIdList.Clear();
            em.GetAllUniqueSharedComponentData<T>(scdList, scdIdList);
            var len = scdIdList.Count;
            var nativeData = new NativeArray<T>(len, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < len; i++) nativeData[i] = scdList[i];
            return nativeData;
        }

        public static NativeArray<int> GetAllUniqueSharedComponentNativeIdx<T>(this EntityManager em, Allocator allocator)
            where T : unmanaged, ISharedComponentData
        {
            var scdList = SCDListCache<T>.ListInstance;
            scdList.Clear(); scdIdList.Clear();
            em.GetAllUniqueSharedComponentData<T>(scdList, scdIdList);
            var len = scdIdList.Count;
            var native = new NativeArray<int>(len, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < len; i++) native[i] = scdIdList[i];
            return native;
        }

        public static (List<T> data, List<int> index) GetAllUniqueSharedComponentList<T>(this EntityManager em)
            where T : unmanaged, ISharedComponentData
        {
            var scdList = SCDListCache<T>.ListInstance;
            scdList.Clear(); scdIdList.Clear();
            em.GetAllUniqueSharedComponentData<T>(scdList, scdIdList);
            return (scdList, scdIdList);
        }
    }
}