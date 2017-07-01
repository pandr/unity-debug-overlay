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

public unsafe class Converter : IConverter<int>, IConverter<float>, IConverter<string>
{
    public static Converter instance = new Converter();

    void IConverter<int>.Convert(ref char* dst, char* end, int value, FormatSpec formatSpec)
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

/// <summary>
/// Screen is a virtual screen overlay featuring fixed with characters to be written anywyere (inside width x height)
/// In addition, a number of 'extras' can be put on the screen -- used by graphs and labels and such.
/// </summary>
public unsafe class Screen
{
    public char[] charBuffer;

    public struct ExtraData
    {
        public Vector4 rect;
        public Vector4 color;
        public char character;
    }

    public int numExtras = 0;
    public ExtraData[] extras = new ExtraData[0];

    public int width { get { return m_Width; } }
    public int height { get { return m_Height; } }

    int m_Width, m_Height;

    int AllocExtras(int num)
    {
        var idx = numExtras;
        numExtras += num;
        if (numExtras > extras.Length)
        {
            var newRects = new ExtraData[numExtras + 128];
            Array.Copy(extras, newRects, extras.Length);
            extras = newRects;
        }
        return idx;
    }

    public struct DrawHistSpec
    {
        public float[] data;
        public bool autoRange;
        public float rangeMin;
        public float rangeMax;
        public Color color;

    }

    // Draw a stacked histogram from numSets of data. Data is interleaved.
    public void DrawHist(float x, float y, float w, float h, float[] data, Color[] color, int numSets, float maxRange = -1.0f)
    {
        if (data.Length % numSets != 0)
            throw new ArgumentException("Length of data must be a multiple of numSets");
        if(color.Length != numSets)
            throw new ArgumentException("Length of colors must be numSets");

        var numSamples = data.Length / numSets;

        var idx = AllocExtras(numSets * numSamples);

        float maxData = float.MinValue;

        for (var i = 0; i < numSamples; i++)
        {
            float sum = 0;
            for (var j = 0; j < numSets; j++)
            {
                sum += data[i * numSets + j];
            }
            if (sum > maxData) maxData = sum;
        }

        if (maxData > maxRange)
            maxRange = maxData;
        float minRange = 0;

        float dx = w / numSamples;
        float scale = maxRange > minRange ? h / (maxRange - minRange) : 1.0f;

        ExtraData extraData;
        extraData.character = '\0';
        for (var i = 0; i < numSamples; i++)
        {
            float stackOffset = 0;
            for (var j = 0; j < numSets; j++)
            {
                extraData.color = color[j];
                float d = data[i * numSets + j];
                float scaledData = d * scale; // now in [0, h]
                extraData.rect = new Vector4(x + dx * i, y + h - d * scale - stackOffset, dx, d * scale);
                stackOffset += scaledData;
                extras[idx + i * numSets + j] = extraData;
            }
        }
    }

    Color[] m_Colors = new Color[1];
    public void DrawHist(float x, float y, float w, float h, float[] data, Color color, float maxRange = -1.0f)
    {
        m_Colors[0] = color;
        DrawHist(x, y, w, h, data, m_Colors, 1, maxRange);
    }

    public void DrawRect(float x, float y, float w, float h, Color col)
    {
        ExtraData rd;
        rd.rect = new Vector4(x, y, w, h);
        rd.color = col;
        var idx = AllocExtras(1);
        rd.character = '\0';
        extras[idx] = rd;
    }

    public Screen(int width, int height)
    {
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        charBuffer = new char[width * height];
        m_Width = width;
        m_Height = height;
    }

    public void Clear()
    {
        for (int i = 0, c = charBuffer.Length; i < c; i++)
            charBuffer[i] = (char)0;
        numExtras = 0;
    }

    public void Write(int x, int y, string format)
    {
        _Write(x, y, format, new ArgList0());
    }
    public void Write<T>(int x, int y, string format, T arg)
    {
        _Write(x, y, format, new ArgList1<T>(arg));
    }
    public void Write<T0, T1>(int x, int y, string format, T0 arg0, T1 arg1)
    {
        _Write(x, y, format, new ArgList2<T0, T1>(arg0, arg1));
    }
    public void Write<T0, T1, T2>(int x, int y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        _Write(x, y, format, new ArgList3<T0, T1, T2>(arg0, arg1, arg2));
    }

    int ReadNum(ref char* p)
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

    int CountChar(ref char* p, char ch)
    {
        int res = 0;
        while (*p == ch)
        {
            res++;
            p++;
        }
        return res;
    }

    void _Write<T>(int x, int y, string format, T argList) where T : IArgList
    {
        int i = m_Width * y + x;
        if (i < 0 || i >= charBuffer.Length)
            return;
        fixed (char* d = &charBuffer[i], p = format)
        {
            var end = d + charBuffer.Length - i;
            var l = format.Length;
            var dest = d;
            var src = p;
            while (*src > 0 && dest < end)
            {
                // Simplified parsing of {<argnum>[,<width>][:<format>]} where <format> is one of either 0000.00 or ####.## type formatters.
                if (*src == '{')
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
                    while (*src != '\0' && *src++ != '}')
                        ;

                    if (argNum < 0 || argNum >= argList.Count)
                        throw new IndexOutOfRangeException(argNum.ToString());

                    argList.Format(ref dest, end, argNum, s);
                }
                else
                {
                    *dest++ = *src++;
                }
            }
        }
    }
}
