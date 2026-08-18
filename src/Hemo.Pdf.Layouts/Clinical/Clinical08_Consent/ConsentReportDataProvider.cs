using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;

/// <summary>
/// Deserializes trusted clinical-08/09 consent report-data from Web.Api
/// and builds the shared ThaiUr header VM (clinical-01…03 chrome).
/// </summary>
public sealed class ConsentReportDataProvider : IReportDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };

    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ConsentReportViewModel result;
        if (context.Data is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            result = JsonSerializer.Deserialize<ConsentReportViewModel>(json.GetRawText(), JsonOptions)
                ?? new ConsentReportViewModel();
        }
        else
        {
            result = new ConsentReportViewModel();
        }

        var language = string.IsNullOrWhiteSpace(result.Language)
            ? (string.Equals(context.ReportTemplateId, ClinicalReportCatalog.ConsentEn, StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "th")
            : result.Language!;

        var title = string.IsNullOrWhiteSpace(result.Title)
            ? (ClinicalReportCatalog.TryGetDefinition(context.ReportTemplateId, out var def)
                ? def!.DisplayName
                : context.ReportTemplateId)
            : result.Title!;

        result = new ConsentReportViewModel
        {
            ConsentId = result.ConsentId ?? string.Empty,
            ReportTemplateId = string.IsNullOrWhiteSpace(result.ReportTemplateId)
                ? context.ReportTemplateId
                : result.ReportTemplateId!,
            Language = language,
            Type = string.IsNullOrWhiteSpace(result.Type) ? "Treatment" : result.Type!,
            Title = title,
            CenterName = result.CenterName ?? string.Empty,
            LogoBase64 = result.LogoBase64,
            PatientName = result.PatientName ?? string.Empty,
            PatientHn = result.PatientHn ?? string.Empty,
            CoverageScheme = result.CoverageScheme ?? string.Empty,
            PatientAge = result.PatientAge,
            IdentityNumber = result.IdentityNumber ?? string.Empty,
            Diagnosis = result.Diagnosis ?? string.Empty,
            Allergies = result.Allergies ?? [],
            SignedByName = result.SignedByName ?? string.Empty,
            IsRepresentative = result.IsRepresentative,
            PatientGender = result.PatientGender,
            Relationship = result.Relationship,
            ReasonMinor = result.ReasonMinor,
            ReasonUnconscious = result.ReasonUnconscious,
            ReasonOther = result.ReasonOther,
            RepresentativeReasonOther = result.RepresentativeReasonOther,
            SignedDate = result.SignedDate ?? new ConsentDateParts(),
            ExpiryDate = result.ExpiryDate,
            ExpiryMonths = result.ExpiryMonths,
            BodyParagraphs = result.BodyParagraphs ?? [],
            PatientSignatureBase64 = result.PatientSignatureBase64,
            DoctorName = result.DoctorName ?? string.Empty,
            DoctorSignatureBase64 = result.DoctorSignatureBase64,
            NurseName = result.NurseName ?? string.Empty,
            NurseSignatureBase64 = result.NurseSignatureBase64,
            WitnessName = result.WitnessName ?? string.Empty,
            WitnessSignatureBase64 = result.WitnessSignatureBase64,
            SkeletonExample = result.SkeletonExample,
        };

        result.Header = BuildThaiUrHeader(result);
        return Task.FromResult<object>(result);
    }

    /// <summary>
    /// Same header settings as clinical-01 for consent: no Date/HD NO. cell, no HD T/Wk.
    /// </summary>
    internal static HemosheetReportViewModel BuildThaiUrHeader(ConsentReportViewModel source) =>
        new()
        {
            LogoBase64 = source.LogoBase64,
            Patient = new HemosheetPatientViewModel
            {
                Name = source.PatientName,
                Hn = source.PatientHn,
                Age = source.PatientAge,
                Coverage = source.CoverageScheme,
                IdentityNumber = source.IdentityNumber,
                Diagnosis = source.Diagnosis,
                Underlying = source.Diagnosis,
                Allergies = source.Allergies?.ToList() ?? [],
            },
            Unit = new HemosheetUnitViewModel
            {
                FullName = source.CenterName,
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    ShowDateAndHdNo = false,
                    ShowHdPerWeek = false,
                },
            },
        };
}
