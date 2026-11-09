using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---------- account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+ ()-]{7,20}$", ErrorMessage = "Phone number can only contain digits, spaces, + ( ) and - (7 to 20 characters).")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a role.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    [Display(Name = "I am registering as")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+ ()-]{7,20}$", ErrorMessage = "Phone number can only contain digits, spaces, + ( ) and - (7 to 20 characters).")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // read-only details, always filled from the API
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

// ---------- participant pages ----------

public class EventListViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Location { get; set; }
    public string? EventType { get; set; }
    public DateTime From { get; set; } = DateTime.Today;
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public EnrolmentDto? Enrolment { get; set; }
}

// ---------- organiser pages ----------

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrolments { get; set; }
    public List<EventDto> Upcoming { get; set; } = new();
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and time.")]
    [Display(Name = "Date and time")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30).AddHours(6);

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Required]
    [RegularExpression("^(Draft|Published|Cancelled|Completed)$", ErrorMessage = "Please choose a valid status.")]
    public string Status { get; set; } = "Published";

    public IFormFile? Banner { get; set; }
    public string? CurrentBanner { get; set; }
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 100000, ErrorMessage = "Entry fee must be between 0 and 100000.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoriesPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? Status { get; set; }
    public string? RowError { get; set; }
}

public class ResultsPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public List<ResultRowViewModel> Rows { get; set; } = new();
}

public class EventPickerViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int? SelectedId { get; set; }
}
