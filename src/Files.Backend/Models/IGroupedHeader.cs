namespace Files.Backend.Models
{
    public interface IGroupedHeader
    {
        string Key { get; set; }

        bool Initialized { get; set; }

        int SortIndexOverride { get; set; }

        string Text { get; set; }

        string Subtext { get; set; }

        string CountText { get; set; }

        bool ShowCountTextBelow { get; set; }

        string Icon { get; set; }

        void PausePropertyChangedNotifications();

        void ResumePropertyChangedNotifications(bool triggerUpdates = true);
    }
}
