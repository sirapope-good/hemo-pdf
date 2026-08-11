using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Helpers;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;

/// <summary>
/// Consent body under the shared ThaiUr clinical header (clinical-01…03 standard).
/// Treatment intro/roles match thaiur-report #08 paper layout.
/// New-consent <see cref="ConsentReportViewModel.SkeletonExample"/> shows "..." in fill/sign zones.
/// </summary>
public sealed class ConsentReportComposer : ILayoutComposer
{
    private const string Ellipsis = "...";

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (ConsentReportViewModel)dataModel;
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => ComposeContent(c, vm),
            Footer = null,
        };
    }

    private static void ComposeContent(IContainer container, ConsentReportViewModel vm)
    {
        var labels = ConsentReportLabels.For(vm.Language);
        var isEn = string.Equals(vm.Language, "en", StringComparison.OrdinalIgnoreCase);
        var isTreatment = !string.Equals(vm.Type, "PDPA", StringComparison.OrdinalIgnoreCase);
        var bodyFont = isEn ? 10.5f : 11f;

        container.Column(col =>
        {
            col.Spacing(0);

            // Standard ThaiUr header (logo | title | Name/CN/Age/Coverage/ID + Diagnosis/Drug Allergy)
            col.Item().Element(c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));

            if (isTreatment)
            {
                col.Item().PaddingTop(8).Element(c => ComposeTreatmentIntro(c, vm, labels, bodyFont));
            }
            else
            {
                col.Item().PaddingTop(8).Element(c => ComposePdpaIntro(c, vm, labels, bodyFont));
            }

            col.Item().PaddingTop(8).Element(c => ComposeBody(c, vm, bodyFont));
            col.Item().PaddingTop(14).Element(c => ComposeSignatures(c, vm, labels, isTreatment));

            if (isTreatment && vm.ExpiryMonths > 0)
            {
                col.Item().PaddingTop(12).Column(note =>
                {
                    note.Spacing(2);
                    note.Item().Text(labels.ValidityNote(vm.ExpiryMonths)).FontSize(9.5f);
                    note.Item().Text(
                        vm.SkeletonExample
                            ? labels.SkeletonValidityRangeLine()
                            : labels.ValidityRangeLine(vm.SignedDate, vm.ExpiryDate))
                        .FontSize(9.5f);
                });
            }
        });
    }

    /// <summary>Paper #08 blocks A–D: signer line, roles, patient-of-rep, reasons.</summary>
    private static void ComposeTreatmentIntro(
        IContainer container,
        ConsentReportViewModel vm,
        ConsentReportLabels labels,
        float bodyFont)
    {
        var skeleton = vm.SkeletonExample;
        var signerName = skeleton
            ? null
            : (string.IsNullOrWhiteSpace(vm.SignedByName) ? vm.PatientName : vm.SignedByName);
        var signerAge = skeleton || vm.IsRepresentative
            ? null
            : vm.PatientAge?.ToString();
        var gender = skeleton ? null : vm.PatientGender;
        var signerTitleGender = vm.IsRepresentative || skeleton ? null : gender;
        var asPatient = !skeleton && !vm.IsRepresentative;
        var asRep = !skeleton && vm.IsRepresentative;

        container.Column(col =>
        {
            // A) ข้าพเจ้า นาย / นาง / นางสาว … อายุ … ปี
            col.Item().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(bodyFont).LineHeight(1.45f));
                t.Span(labels.IAm + " ");
                ComposeAdultTitles(t, labels, signerTitleGender);
                t.Span(" ");
                t.Span(Blank(signerName, Ellipsis)).SemiBold();
                t.Span($" {labels.AgePrefix} {Blank(signerAge, Ellipsis)} {labels.AgeUnit}");
            });

            // B) เป็นผู้ป่วย / ผู้มีอำนาจ…เกี่ยวข้องเป็น … ของผู้ป่วยชื่อ
            col.Item().PaddingTop(6).Row(row =>
            {
                row.AutoItem().Element(c => ComposeChoice(c, labels.AsPatient, asPatient, bodyFont));
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.AutoItem().Element(c =>
                    ComposeChoice(c, labels.AsRepresentative, asRep, bodyFont));
                row.ConstantItem(6);
                row.RelativeItem().AlignMiddle().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(bodyFont).LineHeight(1.35f));
                    t.Span(Blank(skeleton ? null : vm.Relationship, "........................"));
                    t.Span($" {labels.OfPatientNamed}");
                });
            });

            // C) patient name line
            col.Item().PaddingTop(4).Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(bodyFont).LineHeight(1.45f));
                ComposeChildAndAdultTitles(t, labels, asRep ? gender : null);
                t.Span(" ");
                if (asRep)
                {
                    t.Span(Blank(vm.PatientName, Ellipsis)).SemiBold();
                    t.Span($" {labels.AgePrefix} {Blank(vm.PatientAge?.ToString(), Ellipsis)} {labels.AgeUnit}");
                }
                else
                {
                    t.Span($"{Ellipsis} {labels.AgePrefix} {Ellipsis} {labels.AgeUnit}");
                }
            });

            // D) reasons for representation
            col.Item().PaddingTop(6).Text(labels.RepresentativeReasonIntro).FontSize(bodyFont).LineHeight(1.4f);

            var reasonMinor = asRep
                && vm.PatientAge is int age
                && age < 18;
            col.Item().PaddingTop(3).PaddingLeft(12)
                .Element(c => ComposeChoice(c, labels.ReasonMinor, reasonMinor, bodyFont));
            col.Item().PaddingTop(2).PaddingLeft(12)
                .Element(c => ComposeChoice(c, labels.ReasonUnconscious, false, bodyFont));
            col.Item().PaddingTop(2).PaddingLeft(12).Row(row =>
            {
                row.AutoItem().Element(c => ComposeChoice(c, labels.ReasonOther, false, bodyFont));
                row.ConstantItem(6);
                row.RelativeItem().AlignMiddle()
                    .Text(Blank(skeleton ? null : vm.RepresentativeReasonOther, "................................................"))
                    .FontSize(bodyFont);
            });
        });
    }

    private static void ComposePdpaIntro(
        IContainer container,
        ConsentReportViewModel vm,
        ConsentReportLabels labels,
        float bodyFont)
    {
        var skeleton = vm.SkeletonExample;
        var age = skeleton ? Ellipsis : (vm.PatientAge?.ToString() ?? Ellipsis);
        var name = skeleton
            ? Ellipsis
            : (string.IsNullOrWhiteSpace(vm.SignedByName) ? vm.PatientName : vm.SignedByName);
        var asPatient = !skeleton && !vm.IsRepresentative;
        var asRep = !skeleton && vm.IsRepresentative;

        container.Column(col =>
        {
            col.Item().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(bodyFont).LineHeight(1.4f));
                t.Span(labels.IAm + " ");
                t.Span(Blank(name, Ellipsis)).SemiBold();
                t.Span($" {age} {labels.AgeUnit}");
            });

            col.Item().PaddingTop(5).Row(row =>
            {
                row.AutoItem().Element(c => ComposeChoice(c, labels.AsPatient, asPatient, bodyFont));
                row.ConstantItem(14);
                row.AutoItem().Element(c => ComposeChoice(c, labels.AsRepresentative, asRep, bodyFont));
                if (asRep && !string.IsNullOrWhiteSpace(vm.PatientName))
                {
                    row.ConstantItem(8);
                    row.RelativeItem().AlignMiddle()
                        .Text($"({labels.PatientPrefix} {vm.PatientName})")
                        .FontSize(bodyFont);
                }
            });
        });
    }

    private static void ComposeAdultTitles(TextDescriptor t, ConsentReportLabels labels, string? gender)
    {
        var male = IsMale(gender);
        var female = IsFemale(gender);
        SpanTitle(t, labels.TitleMr, male);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMrs, false);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMiss, female);
    }

    private static void ComposeChildAndAdultTitles(TextDescriptor t, ConsentReportLabels labels, string? gender)
    {
        var male = IsMale(gender);
        var female = IsFemale(gender);
        SpanTitle(t, labels.TitleMaster, male);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMissChild, female);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMr, male);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMrs, false);
        t.Span(" / ");
        SpanTitle(t, labels.TitleMiss, female);
    }

    private static void SpanTitle(TextDescriptor t, string title, bool selected)
    {
        if (selected)
        {
            t.Span(title).SemiBold().Underline();
        }
        else
        {
            t.Span(title);
        }
    }

    private static bool IsMale(string? gender) =>
        !string.IsNullOrWhiteSpace(gender)
        && gender.Trim().StartsWith("M", StringComparison.OrdinalIgnoreCase);

    private static bool IsFemale(string? gender) =>
        !string.IsNullOrWhiteSpace(gender)
        && gender.Trim().StartsWith("F", StringComparison.OrdinalIgnoreCase);

    private static void ComposeBody(IContainer container, ConsentReportViewModel vm, float bodyFont)
    {
        container.Column(col =>
        {
            col.Spacing(5);
            foreach (var paragraph in vm.BodyParagraphs)
            {
                col.Item().Element(c =>
                {
                    var text = c.DefaultTextStyle(x => x.FontSize(bodyFont).LineHeight(1.4f));
                    if (paragraph.Sub)
                    {
                        text.PaddingLeft(16).Text(paragraph.Text);
                    }
                    else
                    {
                        text.Text(paragraph.Text);
                    }
                });
            }
        });
    }

    private static void ComposeChoice(IContainer container, string label, bool checkedState, float fontSize)
    {
        container.Row(row =>
        {
            PdfCheckbox.Render(row, checkedState, 10f);
            row.ConstantItem(5);
            row.AutoItem().AlignMiddle().Text(label).FontSize(fontSize).LineHeight(1.3f);
        });
    }

    private static void ComposeSignatures(
        IContainer container,
        ConsentReportViewModel vm,
        ConsentReportLabels labels,
        bool isTreatment)
    {
        var skeleton = vm.SkeletonExample;
        var signerName = skeleton
            ? null
            : (string.IsNullOrWhiteSpace(vm.SignedByName) ? vm.PatientName : vm.SignedByName);
        var originalPatient = !skeleton && vm.IsRepresentative ? vm.PatientName : null;
        var patientSig = skeleton ? null : vm.PatientSignatureBase64;
        var doctorName = skeleton ? null : vm.DoctorName;
        var doctorSig = skeleton ? null : vm.DoctorSignatureBase64;
        var nurseName = skeleton ? null : vm.NurseName;
        var nurseSig = skeleton ? null : vm.NurseSignatureBase64;
        var witnessName = skeleton ? null : vm.WitnessName;
        var witnessSig = skeleton ? null : vm.WitnessSignatureBase64;
        var signDate = skeleton ? new ConsentDateParts() : vm.SignedDate;

        if (!isTreatment)
        {
            container.AlignCenter().Width(200).Element(c =>
                ComposeSignBlock(c, labels, signerName, originalPatient, patientSig, labels.RoleSigner, signDate, skeleton));
            return;
        }

        container.Column(col =>
        {
            col.Spacing(12);
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c =>
                    ComposeSignBlock(c, labels, signerName, originalPatient, patientSig, labels.RoleSigner, signDate, skeleton));
                row.ConstantItem(16);
                row.RelativeItem().Element(c =>
                    ComposeSignBlock(c, labels, doctorName, null, doctorSig, labels.RoleDoctor, signDate, skeleton));
            });
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c =>
                    ComposeSignBlock(c, labels, witnessName, null, witnessSig, labels.RoleWitness, signDate, skeleton));
                row.ConstantItem(16);
                row.RelativeItem().Element(c =>
                    ComposeSignBlock(c, labels, nurseName, null, nurseSig, labels.RoleNurse, signDate, skeleton));
            });
        });
    }

    private static void ComposeSignBlock(
        IContainer container,
        ConsentReportLabels labels,
        string? name,
        string? originalPatientName,
        string? signatureBase64,
        string role,
        ConsentDateParts signedDate,
        bool skeleton)
    {
        container.Column(col =>
        {
            col.Item().Height(40).AlignCenter().AlignMiddle().Element(box =>
            {
                if (skeleton)
                {
                    box.AlignCenter().Text(Ellipsis).FontSize(14).FontColor(Colors.Grey.Medium);
                    return;
                }

                var bytes = PdfImageHelpers.LoadLogoFromDataUrl(signatureBase64);
                if (bytes is { Length: > 0 })
                {
                    box.MaxHeight(40).Image(bytes).FitHeight();
                }
            });
            col.Item().PaddingTop(2).LineHorizontal(0.7f).LineColor(Colors.Grey.Darken2);
            col.Item().PaddingTop(4).AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8.5f).LineHeight(1.2f));
                t.Span(labels.SignPrefix + " ");
                t.Span(string.IsNullOrWhiteSpace(name) ? labels.PlaceholderName : name!).SemiBold();
                if (!string.IsNullOrWhiteSpace(originalPatientName))
                {
                    t.Span($" ({labels.PatientPrefix} {originalPatientName})");
                }
            });
            col.Item().AlignCenter().Text($"({role})").FontSize(8);
            col.Item().AlignCenter().Text(
                skeleton ? labels.SkeletonSignDateLine() : labels.SignDateLine(signedDate)).FontSize(8);
        });
    }

    private static string Blank(string? value, string placeholder = "......") =>
        string.IsNullOrWhiteSpace(value) ? placeholder : value!;
}
