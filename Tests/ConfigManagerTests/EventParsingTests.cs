using System.Xml;
using Xunit;

namespace LoginGuard.Tests;

public class EventParsingTests
{
    private static string? ExtractEventDataValue(string xmlContent, string dataName)
    {
        try
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlContent);

            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/win/2004/08/events/event");

            var node = xml.SelectSingleNode($"//ns:Data[@Name='{dataName}']", nsmgr);
            return node?.InnerText;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsInteractiveLogon(string? logonType)
    {
        return logonType is "2" or "7" or "10";
    }

    private const string SampleEvent4625Xml = @"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'>
  <System>
    <Provider Name='Microsoft-Windows-Security-Auditing' Guid='{54849625-5478-4994-A5BA-3E3B0328C30D}' />
    <EventID>4625</EventID>
    <Version>0</Version>
    <Level>0</Level>
    <Task>12544</Task>
    <Opcode>0</Opcode>
    <Keywords>0x8010000000000000</Keywords>
    <TimeCreated SystemTime='2026-09-30T10:00:00.0000000Z' />
    <EventRecordID>123456</EventRecordID>
    <Channel>Security</Channel>
    <Computer>WORKSTATION-PC</Computer>
    <Security />
  </System>
  <EventData>
    <Data Name='TargetUserSid'>S-1-0-0</Data>
    <Data Name='TargetUserName'>JohnDoe</Data>
    <Data Name='TargetDomainName'>WORKSTATION-PC</Data>
    <Data Name='Status'>0xc000006d</Data>
    <Data Name='FailureReason'>%%2313</Data>
    <Data Name='SubStatus'>0xc000006a</Data>
    <Data Name='LogonType'>2</Data>
    <Data Name='LogonProcessName'>User32</Data>
    <Data Name='AuthenticationPackageName'>Negotiate</Data>
    <Data Name='WorkstationName'>WORKSTATION-PC</Data>
    <Data Name='TransmittedServices'>-</Data>
    <Data Name='LmPackageName'>-</Data>
    <Data Name='KeyLength'>0</Data>
    <Data Name='ProcessId'>0x3e4</Data>
    <Data Name='ProcessName'>C:\Windows\System32\winlogon.exe</Data>
    <Data Name='IpAddress'>127.0.0.1</Data>
    <Data Name='IpPort'>0</Data>
  </EventData>
</Event>";

    [Fact]
    public void ExtractEventDataValue_ParsesTargetUserName()
    {
        string? targetUser = ExtractEventDataValue(SampleEvent4625Xml, "TargetUserName");
        Assert.Equal("JohnDoe", targetUser);
    }

    [Fact]
    public void ExtractEventDataValue_ParsesWorkstationName()
    {
        string? workstation = ExtractEventDataValue(SampleEvent4625Xml, "WorkstationName");
        Assert.Equal("WORKSTATION-PC", workstation);
    }

    [Fact]
    public void ExtractEventDataValue_ParsesLogonType()
    {
        string? logonType = ExtractEventDataValue(SampleEvent4625Xml, "LogonType");
        Assert.Equal("2", logonType);
        Assert.True(IsInteractiveLogon(logonType));
    }

    [Theory]
    [InlineData("2", true)]   // Interactive
    [InlineData("7", true)]   // Unlock
    [InlineData("10", true)]  // RemoteInteractive
    [InlineData("3", false)]  // Network (SMB scan)
    [InlineData("4", false)]  // Batch
    [InlineData("5", false)]  // Service
    [InlineData("8", false)]  // NetworkCleartext
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsInteractiveLogon_CorrectlyFiltersLogonTypes(string? logonType, bool expectedInteractive)
    {
        Assert.Equal(expectedInteractive, IsInteractiveLogon(logonType));
    }

    [Fact]
    public void ExtractEventDataValue_MalformedXml_ReturnsNull()
    {
        string? result = ExtractEventDataValue("<not valid xml", "TargetUserName");
        Assert.Null(result);
    }

    [Fact]
    public void ExtractEventDataValue_MissingField_ReturnsNull()
    {
        string? result = ExtractEventDataValue(SampleEvent4625Xml, "NonExistentField");
        Assert.Null(result);
    }
}
