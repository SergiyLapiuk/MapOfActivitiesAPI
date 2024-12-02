using System;

namespace MapOfActivitiesAPI.Models
{  
        public class Type
        {
            public int Id { get; set; }
            public int ParentTypeId { get; set; }
            public string Name { get; set; }
            public string? ImageURL { get; set; }
            public TimeSpan? MaxDuration { get; set; }
            public List<Event>? Events { get; set; }
            public List<Type>? Children { get; set; }
    }    

}
