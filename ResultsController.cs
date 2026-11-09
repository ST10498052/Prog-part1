using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers capture finish times and positions. Saved results are visible to participants straight away.</summary>
[AppAuthorize(AppRoles.Organiser)]
public class ResultsController : Controller
{
    private static readonly Regex TimePattern = new(@"^\d{1,3}:[0-5]\d:[0-5]\d$", RegexOptions.Compiled);

    private readonly ApiClient _api;

    public ResultsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? eventId)
    {
        return View(await LoadPageAsync(eventId));
    }

    [HttpPost]
    public async Task<IActionResult> Save(int eventId, List<ResultRowViewModel> rows)
    {
        var errors = new Dictionary<int, string>();
        var saved = 0;

        foreach (var row in rows)
        {
            // "Not recorded" rows are skipped
            if (string.IsNullOrWhiteSpace(row.Status)) continue;

            TimeSpan? finishTime = null;
            if (!string.IsNullOrWhiteSpace(row.FinishTime))
            {
                var text = row.FinishTime.Trim();
                if (!TimePattern.IsMatch(text) || !TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var parsed))
                {
                    errors[row.EnrolmentId] = "Finish time must look like 01:52:30 (hours:minutes:seconds).";
                    continue;
                }
                finishTime = parsed;
            }

            var body = new { finishTime, position = row.Position, status = row.Status };

            ApiResponse<ResultDto> result = row.ResultId.HasValue
                ? await _api.PutAsync<ResultDto>($"api/results/{row.ResultId.Value}", body)
                : await _api.PostAsync<ResultDto>($"api/enrolments/{row.EnrolmentId}/result", body);

            if (result.Success) saved++;
            else errors[row.EnrolmentId] = result.Message;
        }

        if (errors.Count == 0)
        {
            TempData["Success"] = saved == 0
                ? "Nothing to save. Choose a status for at least one participant."
                : $"Saved {saved} result(s). They are now visible to participants.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        // Show the page again, keeping what the organiser typed for the rows that failed
        var model = await LoadPageAsync(eventId);
        foreach (var row in model.Rows.Where(r => errors.ContainsKey(r.EnrolmentId)))
        {
            var posted = rows.First(r => r.EnrolmentId == row.EnrolmentId);
            row.FinishTime = posted.FinishTime;
            row.Position = posted.Position;
            row.Status = posted.Status;
            row.RowError = errors[row.EnrolmentId];
        }

        if (saved > 0) TempData["Success"] = $"Saved {saved} result(s).";
        ViewBag.Error = "Some results could not be saved. Please check the rows marked below.";
        return View("Index", model);
    }

    private async Task<ResultsPageViewModel> LoadPageAsync(int? eventId)
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events/mine");
        var model = new ResultsPageViewModel { Events = events.Data ?? new List<EventDto>() };
        if (!events.Success) ViewBag.Error = events.Message;

        model.Selected = model.Events.FirstOrDefault(e => e.EventId == eventId) ?? model.Events.FirstOrDefault();
        if (model.Selected is null) return model;

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{model.Selected.EventId}/enrolments");
        var results = await _api.GetAsync<List<ResultDto>>($"api/events/{model.Selected.EventId}/results");

        var resultsByEnrolment = (results.Data ?? new List<ResultDto>())
            .ToDictionary(r => r.EnrolmentId, r => r);

        foreach (var enrolment in (enrolments.Data ?? new List<EnrolmentDto>()).OrderBy(e => e.ParticipantName))
        {
            resultsByEnrolment.TryGetValue(enrolment.EnrolmentId, out var existing);
            model.Rows.Add(new ResultRowViewModel
            {
                EnrolmentId = enrolment.EnrolmentId,
                ResultId = existing?.ResultId,
                ParticipantName = enrolment.ParticipantName,
                BibNumber = enrolment.BibNumber,
                CategoryName = enrolment.CategoryName,
                FinishTime = existing?.FinishTime.HasValue == true ? Ui.Time(existing.FinishTime) : null,
                Position = existing?.Position,
                Status = existing?.Status
            });
        }
        return model;
    }
}
