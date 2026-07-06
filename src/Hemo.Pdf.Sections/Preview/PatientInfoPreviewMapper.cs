using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Preview;

public static class PatientInfoPreviewMapper
{
    public static PatientInfoReportBlock? Map(object viewModel)
    {
        if (viewModel is not IPatientInfoSource source)
        {
            return null;
        }

        var info = source.PatientInfo;

        return new PatientInfoReportBlock
        {
            Title = "ข้อมูลผู้ป่วย",
            Columns =
            [
                [
                    new LabelValue { Label = "ชื่อ-สกุล", Value = info.Name ?? "—" },
                    new LabelValue { Label = "HN", Value = info.HospitalNumber ?? "—" },
                    new LabelValue { Label = "เลขบัตรประชาชน", Value = info.IdentityNumber ?? "—" },
                ],
                [
                    new LabelValue { Label = "วันเกิด", Value = info.DateOfBirth ?? "—" },
                    new LabelValue { Label = "เพศ", Value = info.Gender ?? "—" },
                    new LabelValue { Label = "หน่วย", Value = info.Unit ?? "—" },
                ],
            ],
        };
    }
}
