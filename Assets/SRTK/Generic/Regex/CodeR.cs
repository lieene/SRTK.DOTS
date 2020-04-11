/************************************************************************************
| File: CodeR.cs                                                                    |
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



using System.Text.RegularExpressions;

namespace SRTK.Utility.RegexBuilder
{
    /// <summary>
    /// R extention to source code
    /// </summary>
    public static class CodeR
    {
        //----------------------------------------------------------------------
        #region Coding Charactors
        /// <summary>
        /// _ Underscore
        /// </summary>
        public static readonly R _ = @"_";

        /// <summary>
        /// /* Start of multi-line comment
        /// </summary>
        public static readonly R StartComment = R.E(@"/*");

        /// <summary>
        /// */ End of multi-line comment
        /// </summary>
        public static readonly R EndComment = R.E(@"*/");

        /// <summary>
        /// // Start of single-line comment
        /// </summary>
        public static readonly R Comment = R.E(@"//");

        /// <summary>
        /// c style directive line Continuation escape
        /// </summary>
        public static readonly R CLineContinue = R.E(@"\");

        /// <summary>
        /// c style directive skiped new line
        /// </summary>
        public static readonly R SkipNewLine = CLineContinue + R.N;

        /// <summary>
        /// match End of String or Before New line
        /// </summary>
        public static readonly R EndOfLineOrFile = R.N.Or(R.StrEndStrict);

        /// <summary>
        /// match End of String or Before New line
        /// </summary>
        public static readonly R EolfAnkor = R.Empty.Is_(EndOfLineOrFile);

        /// <summary>
        /// A-Z for set use
        /// </summary>
        public static readonly R A_Z = @"A-Z";
        /// <summary>
        /// A-Za-z for set use
        /// </summary>
        public static readonly R A_z = @"A-Za-z";
        /// <summary>
        /// a-z for set use
        /// </summary>
        public static readonly R a_z = @"a-z";
        /// <summary>
        /// 0-9 for set use
        /// </summary>
        public static readonly R _0_9 = @"0-9";

        /// <summary>
        /// one or more space not including new line
        /// </summary>
        public static readonly R SomeSpace = @"[ \t]+";

        /// <summary>
        /// zero or more space not including new line
        /// </summary>
        public static readonly R AnySpace = @"[ \t]*";

        #endregion Coding Charactors
        //----------------------------------------------------------------------

        //----------------------------------------------------------------------
        #region Identifiler
        /// <summary>
        /// [_A-Z] start of indentifier uppercase 
        /// </summary>
        public static readonly R ID_ = @"[_A-Z]";

        /// <summary>
        /// [_\p{Lu}] start of wide charactor set indentifier uppercase 
        /// </summary>
        public static readonly R IDx_ = R.Set('_', R.Unicode(true).Lu);

        /// <summary>
        /// [_a-z] start of indentifier lowercase 
        /// </summary>
        public static readonly R id_ = @"[_a-z]";

        /// <summary>
        /// [_\p{Ll}] start of wide charactor set indentifier lowercase 
        /// </summary>
        public static readonly R idx_ = R.Set('_', R.Unicode(true).Ll);

        /// <summary>
        /// [_A-Za-z] start of indentifier ignorecase 
        /// </summary>
        public static readonly R Id_ = @"[_A-Za-z]";

        /// <summary>
        /// [_\p{L}] start of wide charactor set indentifier ignorecase 
        /// </summary>
        public static readonly R Idx_ = R.Set('_', R.Unicode(true).L);

        /// <summary>
        /// [_A-Z0-9] content of indentifier uppercase 
        /// </summary>
        public static readonly R _ID = @"[_A-Z0-9]";

        /// <summary>
        /// [_\p{Lu}\d] content of wide charactor set indentifier uppercase 
        /// </summary>
        public static readonly R _IDx = R.Set('_', R.Unicode(true).Lu, R.Digit);

        /// <summary>
        /// [_A-Z0-9] content of indentifier lowercase 
        /// </summary>
        public static readonly R _id = @"[_a-z0-9]";

        /// <summary>
        /// [_\p{Ll}\d] content of wide charactor set indentifier uppercase 
        /// </summary>
        public static readonly R _idx = R.Set('_', R.Unicode(true).Ll, R.Digit);

        /// <summary>
        /// [_A-Za-z0-9] content of indentifier ignorecase 
        /// </summary>
        public static readonly R _Id = @"[_A-Za-z0-9]";

        /// <summary>
        /// [_\p{L}\d] content of wide charactor set indentifier ignorecase 
        /// </summary>
        public static readonly R _Idx = R.Set('_', R.Unicode(true).L, R.Digit);

        #endregion Identifiler
        public static readonly R Name = R.Set(A_z)._1n;

        /// <summary>
        /// [_A-Za-z][_A-Za-z0-9]*
        /// </summary>
        public static readonly R Identifiler = string.Concat(Id_.V, _Id._0n.V);

        /// <summary>
        /// [_\p{L}][_\p{L}\d]*
        /// </summary>
        public static readonly R IdentifilerX = string.Concat(Idx_.V, _Idx._0n.V);
        //----------------------------------------------------------------------

        //----------------------------------------------------------------------
        #region Numbers
        #region Helper
        public const string HEX_NUM = @"[0-9A-Fa-f]";
        public const string HEX_PRE = @"0[Xx]";
        public const string POS_NEG = @"+-";
        public const string NUM_EXP = @"Ee";
        public const string NUM_D = @"0-9";

        /// <summary>
        /// Ture if has dot in number string
        /// False other wise
        /// </summary>
        /// <param name="m">match from <see cref="Number"/></param>
        /// <returns>ture if there is a dot, otherwise false</returns>
        public static bool NumberHasDot(this Match m)
        {
            return m.Groups["dot"].Success;
        }

        /// <summary>
        /// Ture if has exponent in number string
        /// False other wise
        /// </summary>
        /// <param name="m">match from <see cref="Number"/></param>
        /// <returns>ture if there is a dot, otherwise false</returns>
        public static bool NumberHasExp(this Match m)
        {
            return m.Groups["exp"].Success;
        }

        /// <summary>
        /// Get exponent value of number string
        /// </summary>
        /// <param name="m">match from <see cref="Number"/></param>
        /// <returns>
        /// exponent value from string,
        /// 0 if there's no exponent expression in string.
        /// </returns>
        public static int NumberExp(this Match m)
        {
            var expv = m.Groups["expv"];
            if (expv.Success) return int.Parse(expv.Value);
            else return 0;
        }

        /// <summary>
        /// Get Number type identifyer at end of number
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static char NumberTypeID(this Match m)
        {
            var type = m.Groups["type"];
            if (type.Success) return type.Value[0];
            else return '\0';
        }

        #endregion Helper

        /// <summary>
        /// Match Number:
        /// 1 1.2 0.1 .12 1.2f 1.2e-10 1e10
        /// </summary>
        public static R Number
        {
            get
            {
                var exp = R.Gn("exp", R.Set(NUM_EXP), R.Gn("expv", R.Set(POS_NEG)._01, R.Set(NUM_D)._1n))._01;
                var digits = R.D._0n;
                var dot = R.Gn("dot", R.Dot)._01;
                var type = R.Gn("type", R.Set(A_z))._01;
                return string.Concat(R.Wb, digits.V, dot.V, digits.V, exp.V, type.V);
            }
        }

        /// <summary>
        /// Match 1-2 Byte Hex Number
        /// </summary>
        public static R Hex2Num { get { return HEX_PRE + R.X(HEX_NUM).nm(1, 2); } }

        /// <summary>
        /// Match 1-4 Byte Hex Number
        /// </summary>
        public static R HexNum { get { return HEX_PRE + R.X(HEX_NUM).nm(1, 8); } }

        /// <summary>
        /// Match 1-8 Byte Hex Number
        /// </summary>
        public static R Hex8Num { get { return HEX_PRE + R.X(HEX_NUM).nm(1, 16); } }
        #endregion Numbers
        //----------------------------------------------------------------------

        /// <summary>
        /// [^,)]+ one or more None-(Comma or Right Parentheses) charactors
        /// </summary>
        public static readonly R RawParam = R._Set(R.Comma, R.ClosePrth)._1n;

    }
}