using NUnit.Framework;
using UnityEngine;

public class TextFormatterTests
{
    [Test]
    public void Formats_Integer_Default()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Value: {0}", 42);
        Assert.AreEqual("Value: 42", new string(buf, 0, len));
    }

    [Test]
    public void Formats_Integer_Width()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Value: {0,5}", 42);
        Assert.AreEqual("Value:    42", new string(buf, 0, len));
    }

    [Test]
    public void Formats_Integer_ZeroPadding()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Value: {0:0000}", 42);
        Assert.AreEqual("Value: 0042", new string(buf, 0, len));
    }

    [Test]
    public void Formats_Float_Default()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Value: {0}", 3.14f);
        Assert.AreEqual("Value: 3.14", new string(buf, 0, len));
    }

    [Test]
    public void Formats_Float_CustomPrecision()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Value: {0:00.000}", 3.14159f);
        Assert.AreEqual("Value: 03.142", new string(buf, 0, len));
    }

    [Test]
    public void Formats_String_LeftAlign()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Name: {0,-10}", "Bob");
        Assert.AreEqual("Name: Bob       ", new string(buf, 0, len));
    }

    [Test]
    public void Escapes_Braces()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "Curly: {{ and }}");
        Assert.AreEqual("Curly: { and }", new string(buf, 0, len));
    }

    [Test]
    public void Formats_MultipleArguments()
    {
        char[] buf = new char[32];
        int len = StringFormatter.Write(ref buf, 0, "{0} + {1} = {2}", 2, 3, 5);
        Assert.AreEqual("2 + 3 = 5", new string(buf, 0, len));
    }
} 