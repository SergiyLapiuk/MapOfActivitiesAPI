namespace MapOfActivitiesAPI.ModelsDTO
{
    public class EventDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TypeDTO Type { get; set; }
        public string Coordinates { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}