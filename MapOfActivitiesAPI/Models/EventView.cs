namespace MapOfActivitiesAPI.Models
{
    public class EventView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public Type? Type { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Description { get; set; }
        public string Coordinates { get; set; }
        public string DataUrl { get; set; }
    }
}
