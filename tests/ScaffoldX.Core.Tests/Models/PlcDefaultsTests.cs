using FluentAssertions;
using ScaffoldX.Core.Models;
using Xunit;

namespace ScaffoldX.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="PlcDefaults"/> verifying default PLC connection constants.
/// </summary>
public class PlcDefaultsTests
{
    // ── DefaultPlcIp ────────────────────────────────────────────────────────

    [Fact]
    public void DefaultPlcIp_IsValidIpAddress()
    {
        var result = System.Net.IPAddress.TryParse(PlcDefaults.DefaultPlcIp, out _);

        result.Should().BeTrue("DefaultPlcIp should be a valid IP address");
    }

    [Fact]
    public void DefaultPlcIp_IsPrivateRange()
    {
        // 192.168.x.x is a private range address
        PlcDefaults.DefaultPlcIp.Should().StartWith("192.168.");
    }

    [Fact]
    public void DefaultPlcIp_IsNotNullOrEmpty()
    {
        PlcDefaults.DefaultPlcIp.Should().NotBeNullOrWhiteSpace();
    }

    // ── DefaultPlcPort ──────────────────────────────────────────────────────

    [Fact]
    public void DefaultPlcPort_IsStandardS7Port()
    {
        PlcDefaults.DefaultPlcPort.Should().Be(102);
    }

    [Fact]
    public void DefaultPlcPort_IsValidPortRange()
    {
        PlcDefaults.DefaultPlcPort.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(65535);
    }

    // ── DefaultOpcUaEndpoint ────────────────────────────────────────────────

    [Fact]
    public void DefaultOpcUaEndpoint_StartsWithOpcTcpScheme()
    {
        PlcDefaults.DefaultOpcUaEndpoint.Should().StartWith("opc.tcp://");
    }

    [Fact]
    public void DefaultOpcUaEndpoint_ContainsPort4840()
    {
        PlcDefaults.DefaultOpcUaEndpoint.Should().Contain(":4840");
    }

    [Fact]
    public void DefaultOpcUaEndpoint_IsNotNullOrEmpty()
    {
        PlcDefaults.DefaultOpcUaEndpoint.Should().NotBeNullOrWhiteSpace();
    }

    // ── DefaultModbusPort ───────────────────────────────────────────────────

    [Fact]
    public void DefaultModbusPort_IsStandardModbusPort()
    {
        PlcDefaults.DefaultModbusPort.Should().Be(502);
    }

    [Fact]
    public void DefaultModbusPort_IsValidPortRange()
    {
        PlcDefaults.DefaultModbusPort.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(65535);
    }

    [Fact]
    public void DefaultModbusPort_DiffersFromS7Port()
    {
        PlcDefaults.DefaultModbusPort.Should().NotBe(PlcDefaults.DefaultPlcPort);
    }
}
