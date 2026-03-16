namespace AOLTv1.Models
{
    public class WaypointMarker
    {
        public int EntryFrame { get; set; }
        public int ExitFrame { get; set; }
        public Color MarkerColor { get; set; }
        public string EntryTime { get; set; }
        public string ExitTime { get; set; }
        public int ObjectId { get; set; }
        public string Label { get; set; }
        public string InteractingObject { get; set; }
    }
}
