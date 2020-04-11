/************************************************************************************
| File: RegexEx.cs                                                                  |
| Project: SRTK.Regex                                                               |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified:                                                                    |
| Modified By:                                                                      |
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
using System.IO;
using System.Text.RegularExpressions;

namespace SRTK.Utility
{
    using R = RegexBuilder.R;
    public static class RegexEx
    {
        /// <summary>
        /// Used match new lines in string.
        /// Can also be used to <see cref="Regex.Split(string)"/> lines in string.
        /// Note: <see cref="Regex.Split(string)"/> will return lines WITHOUT new line sequence
        /// </summary>
        public static readonly Regex NewLine = R.N.Build();
        //public static readonly Regex NewLine = new Regex(@"\r?\n", RegexOptions.Compiled);

        public static class FileAndPath
        {
            /// <summary>
            /// Find valid file extension name from string.
            /// extention will be value of first Match.
            /// Note: Match "ccc" in "...aaa.bbb..ccc..", witch is the last dot squence ingoring ending dots.
            /// </summary>
            public static readonly Regex FindFileExtension = R._Set(R.Dot)._1n.Build(true, true);
        }

        public static int NextMathStart(this Group prevMatch)
        {
            if (prevMatch == null) throw new ArgumentNullException("prevMatch");
            if (!prevMatch.Success) throw new InvalidDataException("prevMatch Faild");
            return prevMatch.Index + prevMatch.Length;
        }
    }
}
