namespace MapOfActivitiesAPI.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public string Text { get; set; }
        public int CommentId { get; set; }
    }
}
