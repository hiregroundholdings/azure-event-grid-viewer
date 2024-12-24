namespace Viewer.Models
{
    using System.Text.Json.Serialization;

    public class GridEvent<T> where T: class
    {
        public string Id { get; set;}
        
        public string EventType { get; set;}
        
        public string Subject {get; set;}
        
        public DateTimeOffset EventTime { get; set; } 
        
        public T Data { get; set; } 

        public string Topic { get; set; }

        [JsonIgnore]
        public string RawEvent { get; set; }
    }
}
