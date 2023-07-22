namespace JWST_ClassLib
{
    public class Observation : JWST_ClassLib.ICalendarItem
    {
        public string VisitId { get; set; } = string.Empty;
        public string PCSMode { get; set; } = string.Empty;
        public string VisitType { get; set; } = string.Empty;
        public string ScheduledStartTime { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ScienceInstrumentAndMode { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public List<string> KeywordList { get; set; }
    }
}
