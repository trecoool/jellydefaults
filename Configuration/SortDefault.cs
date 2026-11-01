namespace JellyDefaults.Configuration;

// One library tab's sort. Tab: movies, series, musicalbums, or Folder (mixed-content libraries).
public class SortDefault
{
    public Guid LibraryId { get; set; }

    public string Tab { get; set; } = string.Empty;

    public string SortBy { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;
}
