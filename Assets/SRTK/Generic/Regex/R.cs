/************************************************************************************
| File: R.cs                                                                        |
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
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif
namespace SRTK.Utility.RegexBuilder
{
    /// <summary>
    /// Regex (Regular Expressions) Builder
    /// </summary>
    [Serializable]
    public partial struct R
    {
        //----------------------------------------------------------------------
        #region Field
        /// <summary>
        /// the Regex string being built
        /// </summary>
#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        string _string;

        #endregion Field
        //----------------------------------------------------------------------

        /// <summary>
        /// Build Regex and free internal string builder
        /// </summary>
        /// <param name="compile">should the Regex be compile to IL</param>
        /// <param name="rightToLeft">should text be parsed right to left</param>
        /// <returns>regex built</returns>
        public Regex Build(bool compile = true, bool rightToLeft = false)
        {
            RegexOptions op = RegexOptions.None;
            if (compile) op |= RegexOptions.Compiled;
            if (rightToLeft) op |= RegexOptions.RightToLeft;
            return new Regex(V, op);
        }

        //----------------------------------------------------------------------
        #region Helper functions
        /// <summary>
        /// IsEmpty
        /// </summary>
        public bool IsEmpty
        {
            get { return String.IsNullOrEmpty(_string); }
        }


        #endregion Helper functions
        //----------------------------------------------------------------------
        #region Operator

        /// <summary>
        /// auto convert
        /// </summary>
        public static implicit operator R(string expression) { return new R(expression); }

        /// <summary>
        /// auto convert
        /// </summary>
        public static implicit operator R(char c) { return new R(c.ToString()); }

        /// <summary>
        /// auto convert
        /// </summary>
        public static implicit operator string(R expression)
        {
            return expression.V;
        }

        /// <summary>
        /// appending b at end of a 
        /// </summary>
        public static R operator +(R a, R b)
        {
            return a.Value + b.Value;
        }

        /// <summary>
        /// "a|b" a or b
        /// </summary>
        public static R operator |(R a, R b)
        {
            return string.Concat(a.V, OR, b.V);
        }

        /// <summary>
        /// return the regex string
        /// </summary>
        public override string ToString() { return V; }

        #endregion Operator
        //----------------------------------------------------------------------
        #region Generated Func
        //----------------------------------------------------------------------
        #region Constructors
        R(string initStr) { _string = initStr; }

        /// <summary>
        /// Creats a new Empty R for later use
        /// </summary>
        public static R Empty { get { return new R { _string = string.Empty }; } }

        /// <summary>
        /// fast construct new R from string, without escape
        /// </summary>
        /// <returns>constructed builder</returns>
        public static R X(string str) { return new R(str); }

        /// <summary>
        /// build a R from char, without escape
        /// </summary>
        public static R X(char Char) { return new R(Char.ToString()); }

        /// <summary>
        /// build a R from raw string, with escape
        /// Escape raw string by add escape sequence to special character (\.) (\?) etc.
        /// </summary>
        public static R E(string raw) { return new R(Regex.Escape(raw)); }

        /// <summary>
        /// build a R from raw char, with escape
        /// </summary>
        public static R E(char raw) { return new R(Regex.Escape(raw.ToString())); }

        #endregion Constructors
        //----------------------------------------------------------------------
        #region Builder Clause

        /// <summary>
        /// string Value of this R
        /// </summary>
        public String V
        {
            get
            {
                if (string.IsNullOrEmpty(_string)) return string.Empty;
                else return _string;
            }
        }

        /// <summary>
        /// string Value of this R
        /// </summary>
        public String Value { get { return V; } }


        /// <summary>
        /// fast construct new R from string, without escape
        /// </summary>
        /// <returns>constructed builder</returns>
        public R RX(string str) { return V + X(str); }

        /// <summary>
        /// build a R from char, without escape
        /// </summary>
        public R RX(char Char) { return V + X(Char); }

        /// <summary>
        /// build a R from raw string, with escape
        /// Escape raw string by add escape sequence to special character (\.) (\?) etc.
        /// </summary>
        public R RE(string raw) { return V + E(raw); }

        /// <summary>
        /// build a R from raw char, with escape
        /// </summary>
        public R RE(char raw) { return V + E(raw); }

        /// <summary>
        /// insert exp in front of current R
        /// </summary>
        public R Insert(string exp) { return exp + V; }

        /// <summary>
        /// Append exp at end of current R
        /// </summary>
        public R Append(string exp) { return V + exp; }

        /// <summary>
        /// Concat
        /// </summary>
        public R C(params string[] rs) { return V + string.Concat(rs); }

        /// <summary>
        /// Concat
        /// </summary>
        public static R Concat(params string[] rs) { return string.Concat(rs); }

        /// <summary>
        /// a|b|c alternatives
        /// </summary>
        /// <param name="rs">alternatives</param>
        public static R Alt(params string[] rs)
        {
            if (rs.Length == 0) return Empty;
            else if (rs.Length == 1) return X(rs[0]);
            else return string.Join(OR, rs);
        }
        #endregion Builder Clause
        //----------------------------------------------------------------------
        #region Condition Clauses

        /// <summary>
        /// Conditional Alternation
        /// </summary>
        /// <param name="condition">group name or expressing to test condition</param>
        /// <param name="yes">expressing on condition match</param>
        /// <param name="no">expressing on condition not match</param>
        /// <returns></returns>
        public static R If(string condition, string yes, string no)
        {
            return string.Concat(
                COND_START,
                condition,
                COND_END,
                yes, OR, no,
                COND_END);
        }

        public R YesNo(string yes, string no)
        { return If(this, yes, no); }

        /// <summary>
        /// [If](condition)Then(yse)Or(no)Endif
        /// </summary>
        public static R If(string condition)
        { return string.Concat(COND_START, condition, COND_END); }

        /// <summary>
        /// If(condition)[Then](yse)Or(no)Endif
        /// </summary>
        public R Then(string expression)
        { return V + expression; }

        /// <summary>
        /// If(condition)Then(yse)[Or](no)Endif
        /// </summary>
        public R Or(string expression)
        { return string.Concat(V, OR, expression); }

        /// <summary>
        /// If(condition)Then(yse)Or(no)[Endif]
        /// </summary>
        public R EndIf { get { return V + COND_END; } }

        #endregion Condition Clauses
        //----------------------------------------------------------------------
        #region Group

        public R GBegin { get { return V + INDX_GROUP_START; } }

        public R _GBegin { get { return V + NOCP_GROUP_START; } }

        public R GnBegin(string name)
        {
            return V + string.Format(NAME_GROUP_START, name);
        }

        public R GbBegin(string name1, string name2)
        {
            return V + string.Format(BLNC_GROUP_START, name1, name2);
        }

        public R GEnd { get { return  V + GROUP_END; } }

        /// <summary>
        /// (exp) Index capture Group
        /// </summary>
        /// <param name="expressions">expressions</param>
        public static R G(params string[] expressions)
        { return INDX_GROUP_START + Concat(expressions) + GROUP_END; }

        /// <summary>
        /// (e|x|p) Index capture Group with Alternatives
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        public static R G_(params string[] expressions)
        { return INDX_GROUP_START + Alt(expressions) + GROUP_END; }


        /// <summary>
        /// (?:exp) None capture Group
        /// </summary>
        /// <param name="expressions">expressions</param>
        public static R _G(params string[] expressions)
        { return NOCP_GROUP_START + Concat(expressions) + GROUP_END; }

        /// <summary>
        /// (?:e|x|p) None capture Group with Alternatives
        /// </summary>
        /// <param name="expressions">expressions</param>
        public static R _G_(params string[] expressions)
        { return NOCP_GROUP_START + Alt(expressions) + GROUP_END; }

        /// <summary>
        /// (?(name)exp) Named group
        /// </summary>
        /// <param name="expressions">expressions</param>
        /// <param name="name">name of group</param>
        /// <returns>R</returns>
        public static R Gn(string name, params string[] expressions)
        {
            return string.Format(NAME_GROUP_START, name)
                  + Concat(expressions) + GROUP_END;
        }

        /// <summary>
        /// (?(name)e|x|p) Named group with Alternatives
        /// </summary>
        /// <param name="expressions">expressions</param>
        /// <param name="name">name of group</param>
        /// <returns>R</returns>
        public static R Gn_(string name, params string[] expressions)
        {
            return string.Format(NAME_GROUP_START, name)
                  + Alt(expressions) + GROUP_END;
        }

        /// <summary>
        /// (?(name1-name2)exp) Balance group
        /// </summary>
        /// <param name="expressions">expressions</param>
        /// <param name="name1">name 1</param>
        /// <param name="name2">name 2</param>
        /// <returns>R</returns>
        public static R Gb(string name1, string name2,
            params string[] expressions)
        {
            return string.Format(BLNC_GROUP_START, name1, name2)
                  + Concat(expressions) + GROUP_END;
        }

        /// <summary>
        /// (?(name1-name2)exp) Balance group with Alternatives
        /// </summary>
        /// <param name="expressions">expressions</param>
        /// <param name="name1">name 1</param>
        /// <param name="name2">name 2</param>
        /// <returns>R</returns>
        public static R Gb_(string name1, string name2,
            params string[] expressions)
        {
            return string.Format(BLNC_GROUP_START, name1, name2)
                  + Alt(expressions) + GROUP_END;
        }

        /// <summary>
        /// left positive search
        /// Check pattern before the match (lookbehind), require pattern match to match original expression
        /// </summary>
        /// <param name="pattern">lookbehind pattern</param>
        /// <returns>R</returns>
        public R _Is(string pattern)
        { return string.Format(BWD_GROUP, pattern) + V; }

        /// <summary>
        /// left positive search
        /// Check pattern before the match (lookbehind), require pattern no match to match original expression
        /// </summary>
        /// <param name="builder">add to this builder</param>
        /// <param name="expression">lookbehind pattern</param>
        /// <returns>R</returns>
        public R _Not(string pattern)
        { return string.Format(BWD_N_GROUP, pattern) + V; }

        /// <summary>
        /// right search 
        /// Check pattern after the match (lookahead), require pattern match to match original expression
        /// </summary>
        /// <param name="pattern">lookahead pattern</param>
        public R Is_(string pattern)
        { return V + string.Format(FWD_GROUP, pattern); }

        /// <summary>
        /// right search 
        /// Check pattern after the match (lookahead), require pattern no match to match original expression
        /// </summary>
        /// <param name="pattern">lookahead pattern</param>
        public R Not_(string pattern)
        { return V + string.Format(FWD_N_GROUP, pattern); }

        /// <summary>
        /// Greedy
        /// convert to Non-backtracking group
        /// </summary>
        public R Gr { get { return NON_BACK_GROUP_START + V + GROUP_END; } }

        /// <summary>
        /// Greedy
        /// convert to Non-backtracking group
        /// </summary>
        public static R Greedy(string exp)
        { return NON_BACK_GROUP_START + exp + GROUP_END; }

        #endregion Group
        //----------------------------------------------------------------------
        #region Character

        public static R Ctrl(char ctrlChar)
        {
            return CTRL_CHAR + ctrlChar;
        }

        public static R Hex2(byte id)
        {
            return string.Concat(HEX2, id.ToString("X2"));
        }

        public static R Hex4(ushort id)
        {
            return string.Concat(HEX4, id.ToString("X4"));
        }
        public static R Oct(byte id)
        {
            return string.Concat(C_ESC, id.ToString());
        }

        /// <summary>
        /// positive single char set
        /// </summary>
        public static R Set(params R[] chars)
        {
            StringBuilder b = new StringBuilder();
            int length = chars.Length;
            if (length == 0) return Any;// no restriction return any
            b.Append(SET_START);
            for (int i = 0; i < length; i++)
            {
                b.Append(chars[i].V);
            }
            b.Append(SET_END);
            return b.ToString();
        }

        /// <summary>
        /// nagetive single char set
        /// </summary>
        /// <returns></returns>
        public static R _Set(params R[] chars)
        {
            StringBuilder b = new StringBuilder();
            int length = chars.Length;
            if (length == 0) return SET_N_START + Any + SET_END;// no restriction return any

            b.Append(SET_N_START);
            for (int i = 0; i < length; i++)
            {
                b.Append(chars[i].V);
            }
            b.Append(SET_END);
            return b.ToString();
        }

        /// <summary>
        /// append a positive single char set
        /// </summary>
        public R S(params R[] chars)
        {
            return V + Set(chars);
        }

        /// <summary>
        /// append a nagetive single char set
        /// </summary>
        public R _S(params R[] chars)
        {
            return V + _Set(chars);
        }


        /// <summary>
        /// from-to set
        /// </summary>
        /// <returns></returns>
        public static R F_T(char from, char to)
        {
            return from + SET_RANGE + to;
        }

        // control character and anchors are in const define
        #endregion Character
        //----------------------------------------------------------------------
        #region Unicode
        /// <summary>
        /// Unicode category helper struct
        /// </summary>
        public partial struct UnicodeC
        {
            bool include;
            string prevExpression;
            public UnicodeC(bool inclusive, string preExp)
            {
                include = inclusive;
                prevExpression = preExp;
            }
            public UnicodeC(bool inclusive)
            {
                include = inclusive;
                prevExpression = string.Empty;
            }


            public R Cata(string s)
            {
                if (include)
                    return string.Concat(prevExpression, UNICODE_START, s, UNICODE_END);
                else
                    return string.Concat(prevExpression, UNICODE_N_START, s, UNICODE_END);
            }
        }

        /// <summary>
        /// Generate inclusive Unicode category
        /// for Supported Unicode General Categories refer to:
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?view=netframework-4.7.1#SupportedUnicodeGeneralCategories
        /// and http://www.unicode.org/reports/tr44/
        /// </summary>
        public static UnicodeC UTF16 { get { return new UnicodeC(true); } }

        /// <summary>
        /// Generate exclusive Unicode category or block
        /// for Supported Unicode General Categories refer to:
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?view=netframework-4.7.1#SupportedUnicodeGeneralCategories
        /// and http://www.unicode.org/reports/tr44/
        /// </summary>
        public static UnicodeC _UTF16 { get { return new UnicodeC(false); } }

        /// <summary>
        /// Generate Unicode category
        /// for Supported Unicode General Categories refer to:
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?view=netframework-4.7.1#SupportedUnicodeGeneralCategories
        /// and http://www.unicode.org/reports/tr44/
        /// </summary>
        public static UnicodeC Unicode(bool inclusive) { return new UnicodeC(inclusive); }

        /// <summary>
        /// Append inclusive Unicode category
        /// for Supported Unicode General Categories refer to:
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?view=netframework-4.7.1#SupportedUnicodeGeneralCategories
        /// and http://www.unicode.org/reports/tr44/
        /// </summary>
        public UnicodeC U { get { return new UnicodeC(true, V); } }

        /// <summary>
        /// Append exclusive Unicode category
        /// for Supported Unicode General Categories refer to:
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?view=netframework-4.7.1#SupportedUnicodeGeneralCategories
        /// and http://www.unicode.org/reports/tr44/
        /// </summary>
        public UnicodeC _U { get { return new UnicodeC(false, V); } }


        #endregion Unicode
        //----------------------------------------------------------------------
        #region Quantifiers
        /// <summary>
        /// *? zero or more lazy
        /// </summary>
        public R _0n_ { get { return Value + ZERO_MORE_LAZY; } }

        /// <summary>
        /// +? one or more lazy
        /// </summary>
        public R _1n_ { get { return Value + ONE_MORE_LAZY; } }

        /// <summary>
        /// ?? zero or one lazy
        /// </summary>
        public R _01_ { get { return Value + ZERO_ONE_LAZY; } }

        /// <summary>
        /// * zero or more
        /// </summary>
        public R _0n { get { return Value + ZERO_MORE; } }

        /// <summary>
        /// + one or more
        /// </summary>
        public R _1n { get { return Value + ONE_MORE; } }

        /// <summary>
        /// ? zero or one
        /// </summary>
        public R _01 { get { return this + ZERO_ONE; } }

        /// <summary>
        /// Exactly n times
        /// </summary>
        public R n(int n)
        {
            return string.Concat(V, TIMES_START, n, TIMES_END);
        }

        /// <summary>
        /// Exactly n times lazy
        /// </summary>
        public R n_(int n)
        {
            return string.Concat(V, TIMES_START, n, TIMES_END_LAZY);
        }

        /// <summary>
        /// n to m times
        /// </summary>
        public R nm(int n, int m, bool greedy = true)
        {
            return string.Concat(V, TIMES_START, n, TIMES_RANGE, m, TIMES_END);
        }

        /// <summary>
        /// n to m times lazy
        /// </summary>
        public R nm_(int n, int m, bool greedy = true)
        {
            return string.Concat(V, TIMES_START, n, TIMES_RANGE, m, TIMES_END_LAZY);
        }

        /// <summary>
        /// n times at least
        /// </summary>
        public R nx(int n, bool greedy = true)
        {
            return string.Concat(V, TIMES_START, n, TIMES_RANGE, TIMES_END);
        }

        /// <summary>
        /// n times at least lazy
        /// </summary>
        public R nx_(int n, bool greedy = true)
        {
            return string.Concat(V, TIMES_START, n, TIMES_RANGE, TIMES_END_LAZY);
        }

        #endregion Quantifiers
        //----------------------------------------------------------------------
        #region Back reference
        /// <summary>
        /// Use group value in last match
        /// </summary>
        public R Ref(byte groupId)
        { return V + BR_INDEX + groupId; }

        /// <summary>
        /// Use group value in last match
        /// </summary>
        public R Ref(string groupName)
        { return V + string.Format(BR_NAME, groupName); }
        
        #endregion Back reference
        //----------------------------------------------------------------------
        #region Inline options

        /// <summary>
        /// Append a Empty inline option
        /// (?flags-flags)
        /// </summary>
        public InlineOptions O
        { get { return new InlineOptions(V, string.Empty); } }

        /// <summary>
        /// Append a Empty inline option
        /// (?flags-flags)
        /// </summary>
        public InlineOptions Og(string innerExpression)
        { return new InlineOptions(V, innerExpression);}

        /// <summary>
        /// Generates a Empty inline option with wraped expression
        /// (?flags-flags:innerExpression)
        /// </summary>
        public static InlineOptions OptG(string innerExpression)
        {
            var o = InlineOptions.Empty;
            return o.Wrap(innerExpression);
        }

        /// <summary>
        /// Generates a Empty inline option without wraped expression
        /// (?flags-flags:innerExpression)
        /// </summary>
        public static InlineOptions Opt
        { get { return InlineOptions.Empty; } }

        /// <summary>
        /// RegX inline options
        /// </summary>
        public struct InlineOptions
        {
            //----------------------------------------------------------------------
            #region defines
            static readonly char[] OPTIONS =
            {
                IGNOR_CASE,
                MUILTI_LINE,
                SINGLE_LINE,
                EXPLICIT,
                IGNOR_SPC
            };

            const byte one = 1;
            const byte i = 0;
            const byte im = 1;
            const byte m = 1;
            const byte mm = 1 << m;
            const byte s = 2;
            const byte sm = 1 << s;
            const byte n = 3;
            const byte nm = 1 << n;
            const byte x = 4;
            const byte xm = 1 << x;
            const byte length = 5;

            public static InlineOptions Empty
            {
                get
                {
                    return new InlineOptions
                    {
                        positive = 0,
                        negative = 0,
                        prevExpression = string.Empty,
                        innerExpression = string.Empty
                    };
                }
            }

            internal InlineOptions(string prev, string inner)
            {
                prevExpression = prev;
                innerExpression = inner;
                positive = 0;
                negative = 0;
            }

            #endregion defines
            //----------------------------------------------------------------------
            #region Fields
            // i,m,s,n
            int positive;
            int negative;
            string prevExpression;
            string innerExpression;
            #endregion Fields
            //----------------------------------------------------------------------
            #region Properties
            public bool PositiveIgnorCase { get { return (positive & im) != 0; } }
            public bool PositiveMultiLine { get { return (positive & mm) != 0; } }
            public bool PositiveSingleLine { get { return (positive & sm) != 0; } }
            public bool PositiveExplicit { get { return (positive & nm) != 0; } }
            public bool PositiveIngnoreSpace { get { return (positive & xm) != 0; } }

            public bool NegaTiveIgnorCase { get { return (negative & im) != 0; } }
            public bool NegaTiveMultiLine { get { return (negative & mm) != 0; } }
            public bool NegaTiveSingleLine { get { return (negative & sm) != 0; } }
            public bool NegaTiveExplicit { get { return (negative & nm) != 0; } }
            public bool NegaTiveIngnoreSpace { get { return (negative & xm) != 0; } }
            #endregion Properties
            //----------------------------------------------------------------------
            #region Operations
            /// <summary>
            /// Set inline expression
            /// </summary>
            public InlineOptions Wrap(string exp)
            {
                InlineOptions o = this;
                o.innerExpression = exp;
                return o;
            }
            /// <summary>
            /// Clear inline expression
            /// </summary>
            public InlineOptions UnWrap()
            {
                InlineOptions o = this;
                o.innerExpression = string.Empty;
                return o;
            }

            /// <summary>
            /// Bit-wiise flip
            /// </summary>
            public static InlineOptions operator ~(InlineOptions op)
            {
                InlineOptions o = new InlineOptions
                {
                    negative = 0,
                    positive = 0,
                    innerExpression = op.innerExpression,
                    prevExpression = op.prevExpression
                };
                for (byte b = 0; b < length; b++)
                {
                    if ((op.positive & (one << b)) != 0)
                    {
                        o.negative |= one << b;
                    }
                    else if ((op.negative & (one << b)) != 0)
                    {
                        o.positive |= one << b;
                    }
                }
                return o;
            }

            public static InlineOptions operator +(InlineOptions l, InlineOptions r)
            {
                InlineOptions o = new InlineOptions
                {
                    negative = 0,
                    positive = 0,
                    innerExpression = l.innerExpression,
                    prevExpression = l.prevExpression
                };

                for (byte b = 0; b < length; b++)
                {
                    if ((r.positive & (one << b)) != 0)
                    {
                        o.positive |= one << b;
                    }
                    else if ((l.positive & (one << b)) != 0)
                    {
                        o.positive |= one << b;
                    }
                }
                return o;
            }


            /// <summary>
            /// combine positive and nagetive options
            /// </summary>
            public static InlineOptions operator -(InlineOptions l, InlineOptions r)
            {
                string iexp = l.innerExpression;
                if (string.IsNullOrEmpty(iexp)) iexp = r.innerExpression;


                string pexp = l.prevExpression;
                if (string.IsNullOrEmpty(pexp)) iexp = r.prevExpression;

                InlineOptions o = new InlineOptions
                {
                    negative = 0,
                    positive = 0,
                    innerExpression = iexp,
                    prevExpression = pexp
                };
                for (byte b = 0; b < length; b++)
                {
                    if ((r.positive & (one << b)) != 0)
                    {
                        o.negative |= one << b;
                    }
                    else if ((l.positive & (one << b)) != 0)
                    {
                        o.positive |= one << b;
                    }
                }
                return o;
            }

            /// <summary>
            /// Build string flags for this options
            /// </summary>
            public string Flags
            {
                get
                {
                    StringBuilder builder = new StringBuilder();
                    for (byte b = 0; b < length; b++)
                    {
                        if ((positive & (one << b)) != 0)
                        {
                            builder.Append(OPTIONS[b]);
                        }
                    }
                    int positiveEnd = builder.Length;
                    for (byte b = 0; b < length; b++)
                    {
                        if ((negative & (one << b)) != 0)
                        {
                            builder.Append(OPTIONS[b]);
                        }
                    }
                    if (positiveEnd != builder.Length)
                    {
                        builder.Insert(positiveEnd, R.OPT_DISABLE);
                    }
                    return builder.ToString();
                }
            }
            #endregion Operations
            //----------------------------------------------------------------------
            #region Pre-Option
            /// <summary>
            /// Ignore Case
            /// </summary>
            public InlineOptions IgnoreCase(bool value)
            {
                InlineOptions o;
                o.innerExpression = innerExpression;
                o.prevExpression = prevExpression;
                if (value)
                {
                    o.positive = positive | im;
                    o.negative = negative & ~im;
                }
                else
                {
                    o.negative = negative | im;
                    o.positive = positive & ~im;
                }
                return o;
            }

            /// <summary>
            /// Ignore Case
            /// </summary>
            public InlineOptions I { get { return this + Empty.IgnoreCase(true); } }

            /// <summary>
            /// Muilti-Line
            /// </summary>
            public InlineOptions MuiltiLine(bool value)
            {
                InlineOptions o;
                o.innerExpression = innerExpression;
                o.prevExpression = prevExpression;
                if (value)
                {
                    o.positive = positive | mm;
                    o.negative = negative & ~mm;
                }
                else
                {
                    o.negative = negative | mm;
                    o.positive = positive & ~mm;
                }
                return o;
            }

            /// <summary>
            /// Muilti-Line
            /// </summary>
            public InlineOptions M { get { return this + Empty.MuiltiLine(true); } }

            /// <summary>
            /// Single-Line
            /// </summary>
            public InlineOptions SingleLine(bool value)
            {
                InlineOptions o;
                o.innerExpression = innerExpression;
                o.prevExpression = prevExpression;
                if (value)
                {
                    o.positive = positive | sm;
                    o.negative = negative & ~sm;
                }
                else
                {
                    o.negative = negative | sm;
                    o.positive = positive & ~sm;
                }
                return o;
            }

            /// <summary>
            /// Single-Line
            /// </summary>
            public InlineOptions S { get { return this + Empty.SingleLine(true); } }

            /// <summary>
            /// Explicit
            /// </summary>
            public InlineOptions Named(bool value)
            {
                InlineOptions o;
                o.innerExpression = innerExpression;
                o.prevExpression = prevExpression;
                if (value)
                {
                    o.positive = positive | nm;
                    o.negative = negative & ~nm;
                }
                else
                {
                    o.negative = negative | nm;
                    o.positive = positive & ~nm;
                }
                return o;
            }
            /// <summary>
            /// Explicit
            /// </summary>
            public InlineOptions N { get { return this + Empty.Named(true); } }

            /// <summary>
            /// Ignore space
            /// </summary>
            public InlineOptions IgnoreSpc(bool value)
            {
                InlineOptions o;
                o.innerExpression = innerExpression;
                o.prevExpression = prevExpression;
                if (value)
                {
                    o.positive = positive | xm;
                    o.negative = negative & ~xm;
                }
                else
                {
                    o.negative = negative | xm;
                    o.positive = positive & ~xm;
                }
                return o;
            }
            /// <summary>
            /// Ignore space
            /// </summary>
            public InlineOptions X { get { return this + Empty.IgnoreSpc(true); } }
            #endregion Pre-Option
            //----------------------------------------------------------------------

            /// <summary>
            /// return inline option string value
            /// </summary>
            public R V
            {
                get
                {
                    if (string.IsNullOrEmpty(innerExpression))
                    {
                        return string.Concat(prevExpression, OPT_START, Flags, OPT_END);
                    }
                    else
                    {
                        return string.Concat(prevExpression, OPT_START, Flags, OPT_EXP_START,
                            innerExpression, OPT_END);
                    }
                }
            }

            /// <summary>
            /// return inline option string value
            /// </summary>
            public R Value { get { return V; } }


            public override string ToString()
            { return V; }

            public static implicit operator string(InlineOptions o)
            { return o.V; }
        }
        #endregion Inline options
        //----------------------------------------------------------------------
        #endregion Generated Func
    }
    //----------------------------------------------------------------------

    public static class StringToR
    {
        #region Generated Ext
        /// <summary>
        /// fast construct new R from string, without escape
        /// </summary>
        public static R X(this string s)
        { return R.X(s); }


        /// <summary>
        /// build a R from raw string, with escape
        /// Escape raw string by add escape sequence to special character (\.) (\?) etc.
        /// </summary>
        public static R E(this string s)
        { return R.E(s); }
        #endregion Generated
    }
}
