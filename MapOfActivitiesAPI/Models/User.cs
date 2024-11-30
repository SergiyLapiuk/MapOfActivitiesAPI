namespace MapOfActivitiesAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }

        public List<Event>? CreatedEvents { get; set; }
    }
}
