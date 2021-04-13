using System;
using System.Runtime.InteropServices;

using fts;
// ------------------------------------------------------------------------
// Auto Lookup
//
// Requires 'NativePluginLoader' object to exist in scene
// ------------------------------------------------------------------------
[PluginAttr("cpp_example_dll")]
public static class FooPluginAPI_Auto
{
    //Examples
    [PluginFunctionAttr("simple_func")] 
    public static SimpleFunc simpleFunc = null;
    public delegate int SimpleFunc();

    [PluginFunctionAttr("sum")] 
    public static Sum sum = null;
    public delegate float Sum(float a, float b);

    [PluginFunctionAttr("string_length")] 
    public static StringLength stringLength = null;
    public delegate int StringLength([MarshalAs(UnmanagedType.LPStr)]string s);

    [PluginFunctionAttr("send_struct")] 
    public static SendStruct sendStruct = null;
    public delegate double SendStruct(ref SimpleStruct ss);

    [PluginFunctionAttr("recv_struct")]
    public static RecvStruct recvStruct = null;
    public delegate SimpleStruct RecvStruct();

    //Add New ones
    [PluginFunctionAttr("cochlear_curl")]
    public static CochlearCurl cochlearCurl = null;
    public delegate float CochlearCurl(float curl);

    [PluginFunctionAttr("copy_array")]
    public static CopyArray copyArray = null;
    public delegate int CopyArray(double[] output, int length);

    [PluginFunctionAttr("send_single")]
    public static SendSingle sendSingle = null;
    public delegate double SendSingle(double value);
}


// ------------------------------------------------------------------------
// C Structs
// ------------------------------------------------------------------------
[StructLayout(LayoutKind.Sequential)]
public struct SimpleStruct {
    public int a;
    public float b;
    public bool c;
    
    public SimpleStruct(int a, float b, bool c) {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}