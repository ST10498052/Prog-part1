using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>Display helpers used by the views (formatting only, no business rules).</summary>
public static class Ui
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : "R" + amount.ToString("N2", Culture);

    public static string Distance(decimal? km) =>
        km.HasValue ? km.Value.ToString("0.##", Culture) + " km" : "-";

    public static string Date(DateTime value) => value.ToString("dd MMM yyyy", Culture);

    public static string DateTimeText(DateTime value) => value.ToString("ddd, dd MMM yyyy 'at' HH:mm", Culture);

    public static string Time(TimeSpan? value)
    {
        if (!value.HasValue) return "-";
        var t = value.Value;
        return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public static string Ordinal(int number)
    {
        var lastTwo = Math.Abs(number) % 100;
        if (lastTwo >= 11 && lastTwo <= 13) return number + "th";

        return (Math.Abs(number) % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    /// <summary>For example: "87th out of 512 finishes".</summary>
    public static string PositionText(ResultDto result)
    {
        if (result.Status == "DNF") return "Did not finish";
        if (result.Status == "DSQ") return "Disqualified";
        if (!result.Position.HasValue) return "Awaiting position";

        var text = Ordinal(result.Position.Value);
        return result.TotalFinishers > 0 ? $"{text} out of {result.TotalFinishers} finishes" : text;
    }

    public static string AgeRange(int? min, int? max)
    {
        if (min.HasValue && max.HasValue) return $"{min}-{max} years";
        if (min.HasValue) return $"{min}+ years";
        if (max.HasValue) return $"Up to {max} years";
        return "All ages";
    }

    /// <summary>Images are always loaded through this app's /files route, which asks the API for them.</summary>
    public static string ImageUrl(string blobName) => "/files/" + blobName;

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
        return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
    }

    /// <summary>Colour coding for statuses: green, amber, red, blue, grey.</summary>
    public static string StatusClass(string? status) =>
        (status ?? string.Empty).ToLowerInvariant() switch
        {
            "confirmed" or "published" => "rd-status-green",
            "pending" or "draft" => "rd-status-amber",
            "cancelled" or "dsq" => "rd-status-red",
            "completed" or "finished" => "rd-status-blue",
            _ => "rd-status-grey"
        };

    public static IHtmlContent StatusBadge(string? status)
    {
        var tag = new TagBuilder("span");
        tag.AddCssClass("rd-status");
        tag.AddCssClass(StatusClass(status));
        tag.InnerHtml.Append(status ?? string.Empty);
        return tag;
    }

    public static IHtmlContent TypeBadge(string? type)
    {
        var tag = new TagBuilder("span");
        tag.AddCssClass("rd-type");
        tag.InnerHtml.Append((type ?? string.Empty).ToUpperInvariant());
        return tag;
    }
}
