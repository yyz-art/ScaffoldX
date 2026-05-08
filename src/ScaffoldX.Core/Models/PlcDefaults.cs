namespace ScaffoldX.Core.Models;

/// <summary>
/// Default PLC connection values used across configuration and code generation.
/// </summary>
public static class PlcDefaults
{
    /// <summary>Default PLC IP address for Siemens S7 and other drivers.</summary>
    public const string DefaultPlcIp = "192.168.1.1";

    /// <summary>Default PLC port for Siemens S7 communication.</summary>
    public const int DefaultPlcPort = 102;

    /// <summary>Default OPC-UA server endpoint URL.</summary>
    public const string DefaultOpcUaEndpoint = "opc.tcp://localhost:4840";

    /// <summary>Default Modbus TCP port.</summary>
    public const int DefaultModbusPort = 502;
}
