/************************************************************************************
| File: List.cs                                                                     |
| Project: lieene.Collections                                                       |
| Created Date: Mon Oct 14 2019                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 15 2019                                                    |
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

using System.Collections.Generic;
namespace SRTK
{
    public interface IListX<T> : IList<T>
    {
        bool IsFixedSize { get; }
        bool Remove();
        bool RemoveMany(int count);
        void RemoveRange(int index, int count);
        void InsertRange(int index, IEnumerable<T> collection);
        void InsertMany(int index, params T[] elems);
        void AddRange(IEnumerable<T> collection);
        void AddMany(params T[] elems);
    }

    public class ListX<T> : List<T>, IListX<T>
    {
        public ListX() : base() { }
        public ListX(IEnumerable<T> collection) : base(collection) { }
        public ListX(int capacity) : base(capacity) { }

        public bool IsFixedSize => false;

        public bool Remove()
        {
            var index = Count - 1;
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public bool RemoveMany(int count)
        {
            if(count<0) return false;
            var index = Count - count;
            if (index < 0) return false;
            RemoveRange(index, count);
            return true;
        }

        public void InsertMany(int index, params T[] elems) { base.InsertRange(index, elems); }

        public void AddMany(params T[] elems) { base.AddRange(elems); }
    }

    public static class Array2IList
    {
        // public static void RemoveRange<T>(this T[] a,int index, int count)
        // {        }

        // public static void InsertRange<T>(this T[] a,int index, IEnumerable<T> collection)
        // {        }
    }
}