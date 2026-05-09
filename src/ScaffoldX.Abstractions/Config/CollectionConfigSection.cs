namespace ScaffoldX.Abstractions.Config;

public sealed class CollectionConfigSection : IConfigSection
{
    public bool EnableSiemensS7 { get; set; }
    public bool EnableModbusTcp { get; set; }
    public bool EnableOpcUa { get; set; }
    public bool EnableMitsubishiMc { get; set; }
    public bool EnableOmronFins { get; set; }
    public bool EnableSimulationDriver { get; set; } = true;
    public string DefaultPLCIp { get; set; } = "192.168.1.1";
    public int DefaultPLCPort { get; set; } = 102;
    public int S7Rack { get; set; }
    public int S7Slot { get; set; } = 1;
    public string OpcUaEndpoint { get; set; } = "opc.tcp://localhost:4840";

    public string SectionId => "Scaffold.Collection";
    public string DisplayName => "采集配置";

    public Dictionary<string, object> GetDefaults()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["EnableSiemensS7"] = EnableSiemensS7,
            ["EnableModbusTcp"] = EnableModbusTcp,
            ["EnableOpcUa"] = EnableOpcUa,
            ["EnableMitsubishiMc"] = EnableMitsubishiMc,
            ["EnableOmronFins"] = EnableOmronFins,
            ["HasAnyCollection"] = EnableSiemensS7 || EnableModbusTcp || EnableOpcUa || EnableMitsubishiMc || EnableOmronFins,
            ["EnableSimulationDriver"] = EnableSimulationDriver,
            ["DefaultPLCIp"] = DefaultPLCIp,
            ["DefaultPLCPort"] = DefaultPLCPort,
            ["S7Rack"] = S7Rack,
            ["S7Slot"] = S7Slot,
            ["OpcUaEndpoint"] = OpcUaEndpoint,
        };
    }

    public IReadOnlyList<ValidationError> Validate() => [];
}
