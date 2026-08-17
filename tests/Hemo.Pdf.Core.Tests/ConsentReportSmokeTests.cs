using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class ConsentReportSmokeTests
{
    private const string SampleJson = """
        {
          "language": "th",
          "type": "Treatment",
          "title": "หนังสือแสดงความยินยอมทำการรักษาโดยการฟอกเลือดด้วยเครื่องไตเทียม",
          "centerName": "Hemodialysis Unit",
          "patientName": "เดรโก มัลฟอย",
          "patientHn": "184706",
          "coverageScheme": "Social Security",
          "patientAge": 31,
          "identityNumber": "3120600881117",
          "diagnosis": "-",
          "allergies": ["ไม่มีแพ้ยา"],
          "signedByName": "เดรโก มัลฟอย",
          "isRepresentative": false,
          "patientGender": "M",
          "expiryMonths": 6,
          "signedDate": { "day": "17", "month": "สิงหาคม", "year": "2569" },
          "bodyParagraphs": [
            { "text": "ข้าพเจ้าได้รับทราบและเข้าใจถึงวิธีการรักษาโดยการฟอกเลือดด้วยเครื่องไตเทียม ดังต่อไปนี้", "sub": false },
            { "text": "1. การฟอกเลือดด้วยเครื่องไตเทียม เป็นการกำจัดของเสียและน้ำส่วนเกินออกจากร่างกาย", "sub": false },
            { "text": "2. ภาวะแทรกซ้อนที่อาจเกิดขึ้นได้ระหว่างหรือหลังการรักษา ได้แก่", "sub": false },
            { "text": "2.1 การติดเชื้อ", "sub": true },
            { "text": "2.2 เลือดออก", "sub": true },
            { "text": "2.3 ความดันโลหิตต่ำ", "sub": true },
            { "text": "2.4 ตะคริว", "sub": true },
            { "text": "2.5 อาการแพ้สารที่ใช้ในกระบวนการฟอกเลือด", "sub": true }
          ]
        }
        """;

    static ConsentReportSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_BuildsThaiUrHeaderWithoutDateHdNo()
    {
        var model = await LoadAsync(ClinicalReportCatalog.ConsentTh);
        var vm = Assert.IsType<ConsentReportViewModel>(model);

        Assert.Equal("th", vm.Language);
        Assert.Equal("Treatment", vm.Type);
        Assert.Equal("เดรโก มัลฟอย", vm.Header.Patient.Name);
        Assert.Equal("184706", vm.Header.Patient.Hn);
        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowDateAndHdNo);
        Assert.False(vm.Header.LayoutContext.ReportSettings.ShowHdPerWeek);
        Assert.Equal(8, vm.BodyParagraphs.Count);
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.ConsentTh)]
    [InlineData(ClinicalReportCatalog.ConsentEn)]
    public async Task Render_ProducesPdfBytes(string templateId)
    {
        var renderer = new ConsentReportRenderer(
            new ConsentReportDataProvider(),
            new ConsentReportComposer(),
            new QuestPdfRenderer());

        var context = new PdfReportContext
        {
            ReportTemplateId = templateId,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = "Treatment Consent" },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);

        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    private static Task<object> LoadAsync(string templateId)
    {
        var provider = new ConsentReportDataProvider();
        var context = new PdfReportContext
        {
            ReportTemplateId = templateId,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = "Treatment Consent" },
            Data = JsonDocument.Parse(SampleJson).RootElement.Clone(),
        };
        return provider.GetDataAsync(context, CancellationToken.None);
    }
}
