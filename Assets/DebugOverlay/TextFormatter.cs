using System;
using UnityEngine;

public struct FormatSpec
{
    public int argWidth;
    public bool leadingZero;
    public int integerWidth;
    public int fractWidth;
}

public unsafe interface IConverter<T>
{
    void Convert(ref char* dst, char* end, T value, FormatSpec formatSpec);
}

public unsafe class Converter : IConverter<int>, IConverter<float>, IConverter<string>, IConverter<byte>
{
    public static Converter instance = new Converter();

    void IConverter<int>.Convert(ref char* dst, char* end, int value, FormatSpec formatSpec)
    {
        ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.integerWidth, formatSpec.leadingZero);
    }

    void IConverter<byte>.Convert(ref char* dst, char* end, byte value, FormatSpec formatSpec)
    {
        ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.integerWidth, formatSpec.leadingZero);
    }

    void IConverter<float>.Convert(ref char* dst, char* end, float value, FormatSpec formatSpec)
    {
        if (formatSpec.fractWidth == 0)
            formatSpec.fractWidth = 2;

        var intWidth = formatSpec.argWidth - formatSpec.fractWidth - 1;
        // Very crappy version for now
        bool neg = false;
        if (value < 0.0f)
        {
            neg = true;
            value = -value;
        }
        int v1 = Mathf.FloorToInt(value);
        float fractMult = (int)Mathf.Pow(10.0f, formatSpec.fractWidth);
        int v2 = Mathf.FloorToInt(value * fractMult) % (int)(fractMult);
        ConvertInt(ref dst, end, neg ? -v1 : v1, intWidth, formatSpec.integerWidth, formatSpec.leadingZero);
        if (dst < end)
            *dst++ = '.';
        ConvertInt(ref dst, end, v2, formatSpec.fractWidth, formatSpec.fractWidth, true);
    }
    void IConverter<string>.Convert(ref char* dst, char* end, string value, FormatSpec formatSpec)
    {
        int lpadding = 0, rpadding = 0;
        if (formatSpec.argWidth < 0)
            rpadding = -formatSpec.argWidth - value.Length;
        else
            lpadding = formatSpec.argWidth - value.Length;

        while (lpadding-- > 0 && dst < end)
            *dst++ = ' ';

        for (int i = 0, l = value.Length; i < l && dst < end; i++)
            *dst++ = value[i];

        while (rpadding-- > 0 && dst < end)
            *dst++ = ' ';
    }

    void ConvertInt(ref char* dst, char* end, int value, int argWidth, int integerWidth, bool leadingZero)
    {
        // Dryrun to calculate size
        int numberWidth = 0;
        int signWidth = 0;
        int intpaddingWidth = 0;
        int argpaddingWidth = 0;

        bool neg = value < 0;
        if (neg)
        {
            value = -value;
            signWidth = 1;
        }

        int v = value;
        do
        {
            numberWidth++;
            v /= 10;
        }
        while (v != 0);

        if (numberWidth < integerWidth)
            intpaddingWidth = integerWidth - numberWidth;
        if (numberWidth + intpaddingWidth + signWidth < argWidth)
            argpaddingWidth = argWidth - numberWidth - intpaddingWidth - signWidth;

        dst += numberWidth + intpaddingWidth + signWidth + argpaddingWidth;

        if (dst > end)
            return;

        var d = dst;

        // Write out number
        do
        {
            *--d = (char)('0' + (value % 10));
            value /= 10;
        }
        while (value != 0);

        // Format width padding
        while (intpaddingWidth-- > 0)
            *--d = leadingZero ? '0' : ' ';

        // Sign if needed
        if (neg)
            *--d = '-';

        // Argument width padding
        while (argpaddingWidth-- > 0)
            *--d = ' ';
    }
}

public unsafe interface IArgList
{
    int Count { get; }

    void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec);
}

public unsafe struct ArgList0 : IArgList
{
    public int Count { get { return 0; } }
    public void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec)
    {
    }
}
public unsafe struct ArgList1<T0> : IArgList
{
    public int Count { get { return 1; } }
    T0 t0;
    public ArgList1(T0 a0)
    {
        t0 = a0;
    }
    public void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec)
    {
        switch (argIndex)
        {
            case 0: (Converter.instance as IConverter<T0>).Convert(ref dst, end, t0, formatSpec); break;
        }
    }
}
public unsafe struct ArgList2<T0, T1> : IArgList
{
    public int Count { get { return 2; } }
    T0 t0;
    T1 t1;
    public ArgList2(T0 a0, T1 a1)
    {
        t0 = a0;
        t1 = a1;
    }
    public void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec)
    {
        switch (argIndex)
        {
            case 0: (Converter.instance as IConverter<T0>).Convert(ref dst, end, t0, formatSpec); break;
            case 1: (Converter.instance as IConverter<T1>).Convert(ref dst, end, t1, formatSpec); break;
        }
    }
}
public unsafe struct ArgList3<T0, T1, T2> : IArgList
{
    public int Count { get { return 3; } }
    T0 t0;
    T1 t1;
    T2 t2;
    public ArgList3(T0 a0, T1 a1, T2 a2)
    {
        t0 = a0;
        t1 = a1;
        t2 = a2;
    }
    public void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec)
    {
        switch (argIndex)
        {
            case 0: (Converter.instance as IConverter<T0>).Convert(ref dst, end, t0, formatSpec); break;
            case 1: (Converter.instance as IConverter<T1>).Convert(ref dst, end, t1, formatSpec); break;
            case 2: (Converter.instance as IConverter<T2>).Convert(ref dst, end, t2, formatSpec); break;
        }
    }
}

public unsafe struct ArgList4<T0, T1, T2, T3> : IArgList
{
    public int Count { get { return 4; } }
    T0 t0;
    T1 t1;
    T2 t2;
    T3 t3;
    public ArgList4(T0 a0, T1 a1, T2 a2, T3 a3)
    {
        t0 = a0;
        t1 = a1;
        t2 = a2;
        t3 = a3;
    }
    public void Format(ref char* dst, char* end, int argIndex, FormatSpec formatSpec)
    {
        switch (argIndex)
        {
            case 0: (Converter.instance as IConverter<T0>).Convert(ref dst, end, t0, formatSpec); break;
            case 1: (Converter.instance as IConverter<T1>).Convert(ref dst, end, t1, formatSpec); break;
            case 2: (Converter.instance as IConverter<T2>).Convert(ref dst, end, t2, formatSpec); break;
            case 3: (Converter.instance as IConverter<T3>).Convert(ref dst, end, t3, formatSpec); break;
        }
    }
}

/// <summary>
/// Garbage free string formatter
/// </summary>
public static unsafe class StringFormatter
{
    public static int __Write<T>(ref char[] dst, int destIdx, string format, T argList) where T : IArgList
    {
        int written = 0;
        fixed (char* p = format, d = &dst[0])
        {
            var dest = d + destIdx;
            var end = d + dst.Length;
            var l = format.Length;
            var src = p;
            while (*src > 0 && dest < end)
            {
                // Simplified parsing of {<argnum>[,<width>][:<format>]} where <format> is one of either 0000.00 or ####.## type formatters.
                if(*src == '{' && *(src+1) == '{')
                {
                    *dest++ = *src++;
                    src++;
                }
                else if (*src == '}')
                {
                    if (*(src + 1) == '}')
                    {
                        *dest++ = *src++;
                        src++;
                    }
                    else
                        throw new FormatException("You must escape curly braces");
                }
                else if (*src == '{')
                {
                    src++;

                    // Default values of FormatSpec in case none are given in format string
                    FormatSpec s;
                    s.argWidth = 0;
                    s.integerWidth = 0;
                    s.fractWidth = 0;
                    s.leadingZero = false;

                    // Parse argument number
                    int argNum = 0;
                    argNum = ReadNum(ref src);

                    // Parse optional width
                    if (*src == ',')
                    {
                        src++;
                        s.argWidth = ReadNum(ref src);
                    }

                    // Parse optional format specifier 
                    if (*src == ':')
                    {
                        src++;
                        var ch = *src;
                        s.leadingZero = (ch == '0');
                        s.integerWidth = CountChar(ref src, ch);
                        if (*src == '.')
                        {
                            src++;
                            s.fractWidth = CountChar(ref src, ch);
                        }
                    }

                    // Skip to }
                    while (*src != '\0' && *src != '}')
                        src++;

                    if (*src == '\0')
                        throw new FormatException("Invalid format. Missing '}'?");
                    else
                        src++;

                    if (argNum < 0 || argNum >= argList.Count)
                        throw new IndexOutOfRangeException(argNum.ToString());

                    argList.Format(ref dest, end, argNum, s);
                }
                else
                {
                    *dest++ = *src++;
                }
            }
            written = (int)(dest - d + destIdx);
        }
        return written;
    }
    static int ReadNum(ref char* p)
    {
        int res = 0;
        bool neg = false;
        if (*p == '-')
        {
            neg = true;
            p++;
        }
        while (*p >= '0' && *p <= '9')
        {
            res *= 10;
            res += (*p - '0');
            p++;
        }
        return neg ? -res : res;
    }

    static int CountChar(ref char* p, char ch)
    {
        int res = 0;
        while (*p == ch)
        {
            res++;
            p++;
        }
        return res;
    }
}
