using System.Globalization;
using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Admin;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BazarKoto.Api.Services;

public class TrafficIntelligencePdfService : ITrafficIntelligencePdfService
{
    private const string DeepGreen = "#006B3F";
    private const string DarkGreen = "#073B25";
    private const string Orange = "#FF5A00";
    private const string LightGreen = "#EAF8EF";
    private const string SoftGreen = "#F4FBF6";
    private const string LightOrange = "#FFF3EA";
    private const string BorderGreen = "#B7DEC6";
    private const string BorderGray = "#E5E7EB";
    private const string TextDark = "#111827";
    private const string TextMuted = "#4B5563";
    private const string White = "#FFFFFF";
    private const float CardRadius = 14;
    private const float InnerRadius = 9;
    private const float ChipRadius = 8;

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TrafficIntelligencePdfService> _logger;

    public TrafficIntelligencePdfService(
        IWebHostEnvironment environment,
        ILogger<TrafficIntelligencePdfService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public byte[] Generate(TrafficIntelligenceReportDto report)
    {
        var brandMarkBytes = TryLoadBrandMark();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(30);
                page.MarginVertical(24);
                page.DefaultTextStyle(text => text.FontSize(9.2f).FontColor(TextDark));
                page.PageColor(Colors.White);

                page.Header().Element(container => ComposeHeader(container, report, brandMarkBytes));
                page.Content().PaddingTop(18).Element(container => ComposeContent(container, report));
                page.Footer().Element(container => ComposeFooter(container));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, TrafficIntelligenceReportDto report, byte[]? brandMarkBytes)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Height(84).Element(item => ComposeBrand(item, brandMarkBytes));

                row.ConstantItem(1).Height(76).Background(BorderGray);

                row.ConstantItem(258).PaddingLeft(20).Column(metadata =>
                {
                    metadata.Spacing(7);

                    metadata.Item()
                        .Text("TRAFFIC INTELLIGENCE REPORT")
                        .FontSize(12)
                        .Bold()
                        .FontColor(DeepGreen);

                    metadata.Item().Element(item =>
                        ComposeMetadataLine(
                            item,
                            SvgCalendar,
                            $"Generated: {report.GeneratedAt:MMM dd, yyyy} | {report.GeneratedAt:hh:mm tt}"));

                    metadata.Item().Element(item =>
                        ComposeMetadataLine(
                            item,
                            SvgFileText,
                            "Report Type: Admin Analytics Export"));

                    metadata.Item().Element(item =>
                        ComposeMetadataLine(
                            item,
                            SvgDatabase,
                            $"Data Source: {report.DataSourceLabel}"));
                });
            });

            column.Item().PaddingTop(12).BorderBottom(1.2f).BorderColor(DeepGreen);
        });
    }

    private static void ComposeBrand(IContainer container, byte[]? brandMarkBytes)
    {
        container
            .Height(76)
            .Row(row =>
            {
                row.ConstantItem(86)
                    .Height(76)
                    .AlignMiddle()
                    .Element(item =>
                    {
                        if (brandMarkBytes is not null)
                        {
                            item.Image(brandMarkBytes).FitArea();
                        }
                        else
                        {
                            item.Width(70)
                                .Height(70)
                                .CornerRadius(16)
                                .Background(LightGreen)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text("BK")
                                .FontSize(20)
                                .Bold()
                                .FontColor(DeepGreen);
                        }
                    });

                row.ConstantItem(12);

                row.RelativeItem()
                    .Height(76)
                    .AlignMiddle()
                    .PaddingBottom(3)
                    .Text(text =>
                    {
                        text.Span("Bazar").FontSize(34).Bold().FontColor(DarkGreen);
                        text.Span("Koto").FontSize(34).Bold().FontColor(Orange);
                    });
            });
    }

    private static void ComposeMetadataLine(IContainer container, string iconSvg, string text)
    {
        container.Row(row =>
        {
            row.ConstantItem(18)
                .Height(14)
                .AlignMiddle()
                .Element(icon => ComposePlainSvgIcon(icon, iconSvg, TextDark, 13, 13));

            row.ConstantItem(8);

            row.RelativeItem()
                .AlignMiddle()
                .Text(text)
                .FontSize(8.8f)
                .FontColor(TextDark);
        });
    }

    private static void ComposeContent(IContainer container, TrafficIntelligenceReportDto report)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            column.Item().PaddingBottom(4).Column(summary =>
            {
                summary.Spacing(8);

                summary.Item()
                    .Text("Website Traffic & Ad Readiness Summary")
                    .FontSize(18)
                    .Bold()
                    .FontColor(DarkGreen);

                summary.Item()
                    .Width(430)
                    .Text("This report summarizes key website traffic signals collected by BazarKoto, including total visits, daily activity, weekly reach, and peak traffic behavior. These metrics help evaluate platform growth, user engagement, and future advertisement placement readiness.")
                    .FontSize(9.6f)
                    .LineHeight(1.35f)
                    .FontColor(TextMuted);
            });

            column.Item().Row(row =>
            {
                row.Spacing(13);

                row.RelativeItem().Element(card => ComposeKpiCard(
                    card,
                    SvgEye,
                    "TOTAL TRAFFIC",
                    FormatNumber(report.TotalTraffic),
                    "All tracked website visits",
                    SvgTrendingUp,
                    report.MonthlyTraffic.HasValue
                        ? $"{FormatNumber(report.MonthlyTraffic.Value)} visits this month"
                        : "Monthly traffic not available",
                    true));

                row.RelativeItem().Element(card => ComposeKpiCard(
                    card,
                    SvgUsers,
                    "TODAY'S VISITORS",
                    FormatNumber(report.TodayVisitors),
                    "Website visits so far today",
                    SvgUsers,
                    report.UniqueVisitorsToday.HasValue
                        ? $"{FormatNumber(report.UniqueVisitorsToday.Value)} unique visitors"
                        : "Unique visitors not available",
                    false));
            });

            column.Item().Row(row =>
            {
                row.Spacing(13);

                row.RelativeItem().Element(card => ComposeKpiCard(
                    card,
                    SvgClock,
                    "PEAK HOUR",
                    report.PeakHourLabel,
                    "Highest traffic window",
                    SvgBarChart,
                    report.PeakHourVisits.HasValue
                        ? $"{FormatNumber(report.PeakHourVisits.Value)} visits"
                        : "Visit count not available",
                    true));

                row.RelativeItem().Element(card => ComposeKpiCard(
                    card,
                    SvgLineChart,
                    "WEEKLY TRAFFIC",
                    FormatNumber(report.WeeklyTraffic),
                    "Tracked visits this week",
                    SvgDatabase,
                    report.DataSourceLabel,
                    false));
            });

            column.Item().Element(ComposeAdReadinessPlaceholder);

            column.Item().Element(ComposePlacements);
        });
    }

    private static void ComposeKpiCard(
    IContainer container,
    string iconSvg,
    string title,
    string value,
    string helper,
    string secondaryIconSvg,
    string secondary,
    bool green)
    {
        var accent = green ? DeepGreen : Orange;
        var wash = green ? LightGreen : LightOrange;

        container
            .MinHeight(124)
            .CornerRadius(CardRadius)
            .Border(1)
            .BorderColor(BorderGray)
            .Background(White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);

                column.Item().Row(row =>
                {
                    row.ConstantItem(58)
                        .Height(58)
                        .Element(icon => ComposeIconBox(icon, iconSvg, accent, wash, 58, 31));

                    row.ConstantItem(12);

                    row.RelativeItem().Column(text =>
                    {
                        text.Spacing(4);

                        text.Item()
                            .Text(title)
                            .FontSize(9.5f)
                            .Bold()
                            .FontColor(accent);

                        text.Item()
                            .Text(value)
                            .FontSize(value.Length > 10 ? 18 : 29)
                            .Bold()
                            .FontColor(accent);

                        text.Item()
                            .Text(helper)
                            .FontSize(9)
                            .FontColor(TextMuted);
                    });
                });

                column.Item()
                    .CornerRadius(InnerRadius)
                    .Background(wash)
                    .PaddingVertical(8)
                    .PaddingHorizontal(10)
                    .Row(row =>
                    {
                        row.ConstantItem(34)
                            .Height(18)
                            .AlignMiddle()
                            .Element(icon => ComposePlainSvgIcon(icon, secondaryIconSvg, accent, 18, 18));

                        row.RelativeItem()
                            .AlignMiddle()
                            .Text(secondary)
                            .FontSize(9.3f)
                            .FontColor(TextDark);
                    });
            });
    }

    private static void ComposeAdReadinessPlaceholder(IContainer container)
    {
        container
            .CornerRadius(CardRadius)
            .Border(1)
            .BorderColor(BorderGray)
            .Background(White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);

                column.Item().Row(row =>
                {
                    row.ConstantItem(26)
                        .Height(24)
                        .Element(icon => ComposeIconBox(icon, SvgAnalytics, White, DeepGreen, 24, 15));

                    row.ConstantItem(8);

                    row.RelativeItem()
                        .AlignMiddle()
                        .Text("AD READINESS INSIGHTS")
                        .FontSize(10.5f)
                        .Bold()
                        .FontColor(DeepGreen);
                });

                column.Item().BorderBottom(1).BorderColor(BorderGreen);

                column.Item()
                    .CornerRadius(InnerRadius)
                    .Background(SoftGreen)
                    .Border(1)
                    .BorderColor("#D4EEDD")
                    .PaddingVertical(14)
                    .PaddingHorizontal(14)
                    .Row(row =>
                    {
                        row.ConstantItem(42)
                            .Height(42)
                            .Element(icon => ComposeIconBox(icon, SvgInfoCircle, DeepGreen, LightGreen, 42, 24));

                        row.ConstantItem(12);

                        row.RelativeItem().Column(text =>
                        {
                            text.Spacing(6);

                            text.Item()
                                .Text("Ad readiness insights are not available yet.")
                                .FontSize(10)
                                .Bold()
                                .FontColor(DeepGreen);

                            text.Item()
                                .Text("This section will be enabled after advertisement analytics and placement tracking are implemented.")
                                .FontSize(8.8f)
                                .LineHeight(1.25f)
                                .FontColor(TextMuted);

                            text.Item().Row(chips =>
                            {
                                chips.Spacing(8);

                                chips.AutoItem()
                                    .CornerRadius(ChipRadius)
                                    .Background(LightOrange)
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(8)
                                    .Text("Status: Not implemented")
                                    .FontSize(7.4f)
                                    .Bold()
                                    .FontColor(Orange);

                                chips.AutoItem()
                                    .CornerRadius(ChipRadius)
                                    .Background(LightGreen)
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(8)
                                    .Text("Data availability: Pending")
                                    .FontSize(7.4f)
                                    .Bold()
                                    .FontColor(DeepGreen);
                            });
                        });
                    });
            });
    }

    private static void ComposePlacements(IContainer container)
    {
        container
            .CornerRadius(CardRadius)
            .Border(1)
            .BorderColor(BorderGray)
            .Background(White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);

                column.Item().Row(row =>
                {
                    row.ConstantItem(26)
                        .Height(24)
                        .Element(icon => ComposeIconBox(icon, SvgTarget, White, DeepGreen, 24, 15));

                    row.ConstantItem(8);

                    row.RelativeItem()
                        .AlignMiddle()
                        .Text("RECOMMENDED ADMIN-CONTROLLED AD PLACEMENTS")
                        .FontSize(10.5f)
                        .Bold()
                        .FontColor(DeepGreen);
                });

                column.Item().BorderBottom(1).BorderColor(BorderGreen);

                column.Item().Row(row =>
                {
                    row.Spacing(8);

                    row.RelativeItem().Element(item => ComposePlacement(
                        item,
                        SvgHome,
                        "Home Page Hero / Top Banner",
                        "High visibility placement for brand promotions and campaign highlights.",
                        true));

                    row.RelativeItem().Element(item => ComposePlacement(
                        item,
                        SvgShoppingBag,
                        "Product / Price Browsing Pages",
                        "Promote relevant ads while users browse prices and categories.",
                        false));

                    row.RelativeItem().Element(item => ComposePlacement(
                        item,
                        SvgSearch,
                        "Search Result / Listing Sections",
                        "Target intent-driven users with contextual and relevant advertisements.",
                        true));

                    row.RelativeItem().Element(item => ComposePlacement(
                        item,
                        SvgSettings,
                        "Admin-Controlled Banner Slots",
                        "Keep ad slots backend-controlled for scalability and future flexibility.",
                        false));
                });

                column.Item()
                    .CornerRadius(InnerRadius)
                    .Background(SoftGreen)
                    .Border(1)
                    .BorderColor("#D4EEDD")
                    .PaddingVertical(8)
                    .PaddingHorizontal(12)
                    .Row(row =>
                    {
                        row.ConstantItem(28)
                            .Height(18)
                            .AlignMiddle()
                            .Element(icon => ComposePlainSvgIcon(icon, SvgShieldCheck, DeepGreen, 18, 18));

                        row.RelativeItem()
                            .AlignMiddle()
                            .Text("All ad placements should remain backend-controlled for better targeting, performance tracking, and scalability.")
                            .FontSize(8.8f)
                            .FontColor(DeepGreen);
                    });
            });
    }

    private static void ComposePlacement(
        IContainer container,
        string iconSvg,
        string title,
        string body,
        bool green)
    {
        var accent = green ? DeepGreen : Orange;
        var wash = green ? LightGreen : LightOrange;

        container.Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.ConstantItem(34)
                    .Height(34)
                    .Element(icon => ComposeIconBox(icon, iconSvg, accent, wash, 34, 20));

                row.ConstantItem(6);

                row.RelativeItem()
                    .AlignMiddle()
                    .Text(title)
                    .FontSize(7.8f)
                    .Bold()
                    .FontColor(accent);
            });

            column.Item()
                .Text(body)
                .FontSize(7.1f)
                .LineHeight(1.2f)
                .FontColor(TextDark);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(DeepGreen).PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Bazar").FontSize(10).Bold().FontColor(DarkGreen);
                    text.Span("Koto").FontSize(10).Bold().FontColor(Orange);
                });

                row.RelativeItem()
                    .AlignCenter()
                    .Text("Confidential Admin Report")
                    .FontSize(8.5f)
                    .FontColor(TextDark);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8.5f).FontColor(TextDark);
                    text.CurrentPageNumber().FontSize(8.5f).FontColor(TextDark);
                    text.Span(" of ").FontSize(8.5f).FontColor(TextDark);
                    text.TotalPages().FontSize(8.5f).FontColor(TextDark);
                });
            });

            column.Item()
                .PaddingTop(6)
                .AlignCenter()
                .Text("This report is generated from BazarKoto admin analytics data and is intended for internal administrative review only.")
                .FontSize(7)
                .FontColor(TextMuted);
        });
    }

    private static void ComposeIconBox(
    IContainer container,
    string iconSvg,
    string accentColor,
    string backgroundColor,
    float boxSize,
    float iconSize)
    {
        container
            .Width(boxSize)
            .Height(boxSize)
            .Layers(layers =>
            {
                layers.Layer()
                    .Width(boxSize)
                    .Height(boxSize)
                    .Svg(CreateCircleBackgroundSvg(backgroundColor));

                layers.PrimaryLayer()
                    .AlignCenter()
                    .AlignMiddle()
                    .Element(icon => ComposePlainSvgIcon(icon, iconSvg, accentColor, iconSize, iconSize));
            });
    }

    private static string CreateCircleBackgroundSvg(string fillColor)
    {
        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="50" fill="{fillColor}" />
        </svg>
        """;
    }

    private static void ComposePlainSvgIcon(
        IContainer container,
        string iconSvg,
        string color,
        float width,
        float height)
    {
        container
            .Width(width)
            .Height(height)
            .Svg(ColorizeSvg(iconSvg, color));
    }

    private byte[]? TryLoadBrandMark()
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_environment.ContentRootPath))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(
                _environment.ContentRootPath,
                "..",
                "..",
                "..",
                "ClientSide",
                "bazarKoto",
                "public",
                "images",
                "bazar-koto-mark-v2.png")));

            candidates.Add(Path.GetFullPath(Path.Combine(
                _environment.ContentRootPath,
                "..",
                "..",
                "ClientSide",
                "bazarKoto",
                "public",
                "images",
                "bazar-koto-mark-v2.png")));
        }

        candidates.Add(@"C:\Projects\BazarKoto\Src\ClientSide\bazarKoto\public\images\bazar-koto-mark-v2.png");

        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            candidates.Add(Path.Combine(_environment.WebRootPath, "images", "bazar-koto-mark-v2.png"));
            candidates.Add(Path.Combine(_environment.WebRootPath, "images", "bazar-koto-logo-horizontal-transparent.png"));
        }

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Traffic report brand mark could not be loaded from {LogoPath}.", path);
            }
        }

        _logger.LogWarning("Traffic report brand mark was not found. Checked paths: {LogoPaths}", string.Join(" | ", candidates));
        return null;
    }

    private static string FormatNumber(int value)
    {
        var absoluteValue = Math.Abs((long)value);
        var sign = value < 0 ? "-" : string.Empty;

        if (absoluteValue < 1000)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        var units = new[]
        {
            new CompactNumberUnit(1_000_000_000_000, 1_000_000_000_000m, "t"),
            new CompactNumberUnit(1_000_000_000, 1_000_000_000m, "b"),
            new CompactNumberUnit(10_000_000, 10_000_000m, "crore"),
            new CompactNumberUnit(100_000, 100_000m, "lakh"),
            new CompactNumberUnit(1000, 1000m, "k")
        };
        var unit = units.First(item => absoluteValue >= item.Threshold);
        var compactValue = absoluteValue / unit.Divisor;
        var precision = compactValue >= 100 ? 0 : 1;
        var formatted = compactValue
            .ToString($"F{precision}", CultureInfo.InvariantCulture)
            .TrimEnd('0')
            .TrimEnd('.');

        return $"{sign}{formatted}{unit.Suffix}";
    }

    private sealed record CompactNumberUnit(long Threshold, decimal Divisor, string Suffix);

    private static string ColorizeSvg(string svg, string color)
    {
        return svg.Replace("currentColor", color, StringComparison.OrdinalIgnoreCase);
    }

    private const string SvgCalendar =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="4" width="18" height="18" rx="2.5"/>
          <path d="M16 2v4M8 2v4M3 10h18"/>
          <path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01"/>
        </svg>
        """;

    private const string SvgFileText =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M14 2H7a3 3 0 0 0-3 3v14a3 3 0 0 0 3 3h10a3 3 0 0 0 3-3V8z"/>
          <path d="M14 2v6h6"/>
          <path d="M8 13h8M8 17h8M8 9h2"/>
        </svg>
        """;

    private const string SvgDatabase =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <ellipse cx="12" cy="5" rx="8" ry="3"/>
          <path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/>
          <path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6"/>
        </svg>
        """;

    private const string SvgEye =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z"/>
          <circle cx="12" cy="12" r="3.2"/>
        </svg>
        """;

    private const string SvgUsers =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2"/>
          <circle cx="9.5" cy="7" r="4"/>
          <path d="M22 21v-2a4 4 0 0 0-3-3.85"/>
          <path d="M16 3.15a4 4 0 0 1 0 7.7"/>
        </svg>
        """;

    private const string SvgClock =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="9"/>
          <path d="M12 7v5l3.5 2"/>
        </svg>
        """;

    private const string SvgLineChart =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 20h18"/>
          <path d="M5 16l4-4 3 3 6-8"/>
          <path d="M15 7h3v3"/>
        </svg>
        """;

    private const string SvgTrendingUp =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 17l6-6 4 4 7-8"/>
          <path d="M14 7h6v6"/>
        </svg>
        """;

    private const string SvgBarChart =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M4 20h16"/>
          <path d="M6 20V10"/>
          <path d="M12 20V5"/>
          <path d="M18 20v-7"/>
        </svg>
        """;

    private const string SvgAnalytics =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M4 19V5"/>
          <path d="M4 19h16"/>
          <path d="M7 15l3-3 3 2 5-7"/>
          <path d="M16 7h2v2"/>
        </svg>
        """;

    private const string SvgInfoCircle =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="9"/>
          <path d="M12 10v6"/>
          <path d="M12 7h.01"/>
        </svg>
        """;

    private const string SvgTarget =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="9"/>
          <circle cx="12" cy="12" r="5"/>
          <circle cx="12" cy="12" r="1.5"/>
        </svg>
        """;

    private const string SvgHome =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 11.5L12 4l9 7.5"/>
          <path d="M5 10.5V21h14V10.5"/>
          <path d="M9.5 21v-6h5v6"/>
        </svg>
        """;

    private const string SvgShoppingBag =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M6 8h12l1 13H5L6 8z"/>
          <path d="M9 8a3 3 0 0 1 6 0"/>
        </svg>
        """;

    private const string SvgSearch =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="7"/>
          <path d="M20 20l-4-4"/>
        </svg>
        """;

    private const string SvgSettings =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="3"/>
          <path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.05.05a2 2 0 1 1-2.83 2.83l-.05-.05A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 .6 1.7 1.7 0 0 0-.4 1.08V21a2 2 0 1 1-4 0v-.08A1.7 1.7 0 0 0 8.6 19.4a1.7 1.7 0 0 0-1.88.34l-.05.05a2 2 0 1 1-2.83-2.83l.05-.05A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-.6-1 1.7 1.7 0 0 0-1.08-.4H3a2 2 0 1 1 0-4h-.08A1.7 1.7 0 0 0 4.6 8.6a1.7 1.7 0 0 0-.34-1.88l-.05-.05a2 2 0 1 1 2.83-2.83l.05.05A1.7 1.7 0 0 0 9 4.6a1.7 1.7 0 0 0 1-.6 1.7 1.7 0 0 0 .4-1.08V3a2 2 0 1 1 4 0v-.08A1.7 1.7 0 0 0 15.4 4.6a1.7 1.7 0 0 0 1.88-.34l.05-.05a2 2 0 1 1 2.83 2.83l-.05.05A1.7 1.7 0 0 0 19.4 9c.2.34.5.62.86.78.23.1.48.16.74.16h.08a2 2 0 1 1 0 4H21a1.7 1.7 0 0 0-1.6 1.06z"/>
        </svg>
        """;

    private const string SvgShieldCheck =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
          <path d="M9 12l2 2 4-4"/>
        </svg>
        """;

    private const string SvgLeaves =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 32" fill="currentColor">
          <path d="M29 26C16 26 8 13 8 13s13-4 22 5c4 4 3 8 3 8s-1 0-4 0z"/>
          <path d="M35 26c13 0 21-13 21-13s-13-4-22 5c-4 4-3 8-3 8s1 0 4 0z"/>
        </svg>
        """;
}
