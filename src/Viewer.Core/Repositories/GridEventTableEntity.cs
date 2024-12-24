namespace Viewer
{
    using Azure;
    using Azure.Data.Tables;
    using System;

    internal class GridEventTableEntity : ITableEntity
    {
        public required string PartitionKey { get; set; }
        
        public required string RowKey { get; set; }
        
        public DateTimeOffset? Timestamp { get; set; }
        
        public ETag ETag { get; set; }

        public required string Subject { get; set; }

        public required string EventType { get; set; }

        public required DateTimeOffset EventTimestamp { get; set; }

        public required string Payload { get; set; }

        public required string Schema { get; set; }
    }
}
