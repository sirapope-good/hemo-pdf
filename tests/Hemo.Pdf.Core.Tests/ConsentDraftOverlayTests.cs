using System.Text.Json;
using Hemo.Pdf.Application;

namespace Hemo.Pdf.Core.Tests;

public class ConsentDraftOverlayTests
{
    [Fact]
    public void Apply_WithoutDraftFlag_ReturnsOriginal()
    {
        var data = JsonDocument.Parse("""{"signedByName":"A","patientName":"A"}""").RootElement.Clone();
        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["signedByName"] = "B",
        });

        Assert.Equal("A", result.GetProperty("signedByName").GetString());
    }

    [Fact]
    public void Apply_OverlaysNamesDateAndSignatures()
    {
        var data = JsonDocument.Parse("""
        {
          "language": "en",
          "patientName": "Patient Zero",
          "signedByName": "Patient Zero",
          "isRepresentative": false,
          "witnessName": "",
          "expiryMonths": 6,
          "signedDate": { "day": "1", "month": "January", "year": "2020" },
          "patientSignatureBase64": null,
          "witnessSignatureBase64": null
        }
        """).RootElement.Clone();

        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["draft"] = true,
            ["signedByName"] = "Legal Rep",
            ["relationship"] = "บิดา",
            ["witnessName"] = "Witness One",
            ["signedDate"] = "2026-08-11",
            ["patientSignatureBase64"] = "data:image/png;base64,aaa",
            ["witnessSignatureBase64"] = null,
        });

        Assert.Equal("Legal Rep", result.GetProperty("signedByName").GetString());
        Assert.True(result.GetProperty("isRepresentative").GetBoolean());
        Assert.Equal("บิดา", result.GetProperty("relationship").GetString());
        Assert.Equal("Witness One", result.GetProperty("witnessName").GetString());
        Assert.Equal("11", result.GetProperty("signedDate").GetProperty("day").GetString());
        Assert.Equal("August", result.GetProperty("signedDate").GetProperty("month").GetString());
        Assert.Equal("2026", result.GetProperty("signedDate").GetProperty("year").GetString());
        Assert.Equal("11", result.GetProperty("expiryDate").GetProperty("day").GetString());
        Assert.Equal("February", result.GetProperty("expiryDate").GetProperty("month").GetString());
        Assert.Equal("2027", result.GetProperty("expiryDate").GetProperty("year").GetString());
        Assert.Equal("data:image/png;base64,aaa", result.GetProperty("patientSignatureBase64").GetString());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("witnessSignatureBase64").ValueKind);
    }

    [Fact]
    public void Apply_Skeleton_BlanksFillAndSignZones_KeepsPatientHeader()
    {
        var data = JsonDocument.Parse("""
        {
          "language": "th",
          "patientName": "จอร์จ วิสลีย์",
          "patientHn": "6529635",
          "signedByName": "จอร์จ วิสลีย์",
          "doctorName": "Dr A",
          "nurseName": "Nurse B",
          "witnessName": "W",
          "patientSignatureBase64": "data:image/png;base64,aaa",
          "doctorSignatureBase64": "data:image/png;base64,bbb"
        }
        """).RootElement.Clone();

        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["draft"] = true,
            ["skeleton"] = true,
            ["signedByName"] = "should-be-ignored",
            ["patientSignatureBase64"] = "data:image/png;base64,ccc",
        });

        Assert.True(result.GetProperty("skeletonExample").GetBoolean());
        Assert.Equal("จอร์จ วิสลีย์", result.GetProperty("patientName").GetString());
        Assert.Equal("6529635", result.GetProperty("patientHn").GetString());
        Assert.Equal("", result.GetProperty("signedByName").GetString());
        Assert.False(result.GetProperty("isRepresentative").GetBoolean());
        Assert.Equal("", result.GetProperty("relationship").GetString());
        Assert.Equal("", result.GetProperty("doctorName").GetString());
        Assert.Equal("", result.GetProperty("nurseName").GetString());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("patientSignatureBase64").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("doctorSignatureBase64").ValueKind);
    }

    [Fact]
    public void Apply_ThaiLanguage_UsesBuddhistYear()
    {
        var data = JsonDocument.Parse("""
        {
          "language": "th",
          "patientName": "ผู้ป่วย",
          "expiryMonths": 6
        }
        """).RootElement.Clone();

        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["draft"] = true,
            ["signedDate"] = "2026-08-11",
        });

        Assert.Equal("11", result.GetProperty("signedDate").GetProperty("day").GetString());
        Assert.Equal("สิงหาคม", result.GetProperty("signedDate").GetProperty("month").GetString());
        Assert.Equal("2569", result.GetProperty("signedDate").GetProperty("year").GetString());
    }

    [Fact]
    public void Apply_PatientSigner_ClearsRelationship()
    {
        var data = JsonDocument.Parse("""
        {
          "language": "th",
          "patientName": "จินนี วิสลีย์",
          "signedByName": "มอลลี่ วิสลีย์",
          "isRepresentative": true,
          "relationship": "มารดา"
        }
        """).RootElement.Clone();

        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["draft"] = true,
            ["signedByName"] = "จินนี วิสลีย์",
            ["relationship"] = "มารดา",
            ["reasonUnconscious"] = true,
        });

        Assert.False(result.GetProperty("isRepresentative").GetBoolean());
        Assert.Equal("", result.GetProperty("relationship").GetString());
        Assert.False(result.GetProperty("reasonMinor").GetBoolean());
        Assert.False(result.GetProperty("reasonUnconscious").GetBoolean());
        Assert.Equal("", result.GetProperty("representativeReasonOther").GetString());
    }

    [Fact]
    public void Apply_OverlaysRepresentativeReasonFlags()
    {
        var data = JsonDocument.Parse("""
        {
          "language": "th",
          "patientName": "จินนี วิสลีย์",
          "signedByName": "จินนี วิสลีย์",
          "isRepresentative": false
        }
        """).RootElement.Clone();

        var result = ConsentDraftOverlay.Apply(data, new Dictionary<string, object?>
        {
            ["draft"] = true,
            ["signedByName"] = "มอลลี่ วิสลีย์",
            ["reasonMinor"] = false,
            ["reasonUnconscious"] = true,
            ["reasonOther"] = false,
            ["representativeReasonOther"] = "",
        });

        Assert.True(result.GetProperty("isRepresentative").GetBoolean());
        Assert.False(result.GetProperty("reasonMinor").GetBoolean());
        Assert.True(result.GetProperty("reasonUnconscious").GetBoolean());
        Assert.False(result.GetProperty("reasonOther").GetBoolean());
    }
}
