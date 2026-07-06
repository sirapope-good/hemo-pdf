namespace Hemo.Pdf.Core.Constants;

public static class PdfStyleDefaults
{
    public static class Fonts
    {
        public const string PrimaryFamily = "Sarabun";
        public const string PrimaryLightFamily = "Sarabun Light";
        public const string PrimaryExtraLightFamily = "Sarabun ExtraLight";
        public const string PrimarySemiBoldFamily = "Sarabun SemiBold";
    }

    public static class Header
    {
        public const float LogoWidth = 90f;
        public const float LogoHeight = 48f;

        public const float CompanyNameFontSize = 12f;
        public const string CompanyNameFontFamily = Fonts.PrimaryLightFamily;

        public const float CompanyDetailFontSize = 8f;
        public const string CompanyDetailFontFamily = Fonts.PrimaryExtraLightFamily;

        public const float TitleFontSize = 14f;
        public const string TitleFontFamily = Fonts.PrimarySemiBoldFamily;

        public const float SubtitleFontSize = 10f;
        public const string SubtitleFontFamily = Fonts.PrimaryFamily;

        public const float ReportCodeFontSize = 8f;
        public const string ReportCodeFontFamily = Fonts.PrimaryFamily;

        public const float MetadataFontSize = 8f;
        public const string MetadataFontFamily = Fonts.PrimaryFamily;
    }

    public static class Body
    {
        public const float BaseFontSize = 7.5f;
        public const string BaseFontFamily = Fonts.PrimaryFamily;

        public const float SectionTitleFontSize = 10f;
        public const string SectionTitleFontFamily = Fonts.PrimarySemiBoldFamily;

        public const float DataFontSize = 7.5f;
        public const string DataFontFamily = Fonts.PrimaryFamily;
    }

    public static class Footer
    {
        public const float TextFontSize = 6f;
        public const string TextFontFamily = Fonts.PrimaryLightFamily;
    }
}
