namespace JWST_ClassLib
{
    public interface ICalendarItem
    {
        string ScheduledStartTime { get; set; }
        string Duration { get; set; }

        string Category { get; set; }
        string Keywords { get; set; }
        public List<string> KeywordList { get; set; }
    }
}