namespace Viewer
{
    using Azure;
    using Azure.Data.Tables;
    using Azure.Data.Tables.Models;
    using System.Threading;
    using System.Threading.Tasks;
    using Viewer.Models;

    public class GridEventTableRepository : IGridEventRepository
    {
        private readonly TableClient _tableClient;

        public GridEventTableRepository(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public Task AddAsync<T>(CloudEvent<T> cloudEvent, CancellationToken cancellationToken = default)
            where T : class

        {
            GridEventTableEntity tableEntity = new()
            {
                PartitionKey = "event",
                RowKey = cloudEvent.Id,
                Subject = cloudEvent.Subject,
                EventTimestamp = cloudEvent.Time,
                EventType = cloudEvent.Type,
                Payload = cloudEvent.RawEvent,
                Schema = "cloudevent",
            };

            return AddAsync(tableEntity, cancellationToken);
        }

        public Task AddAsync<T>(GridEvent<T> gridEvent, CancellationToken cancellationToken = default)
            where T : class
        {
            GridEventTableEntity tableEntity = new()
            {
                PartitionKey = "event",
                RowKey = gridEvent.Id,
                Subject = gridEvent.Subject,
                EventTimestamp = gridEvent.EventTime,
                EventType = gridEvent.EventType,
                Payload = gridEvent.RawEvent,
                Schema = "eventgrid",
            };

            return AddAsync(tableEntity, cancellationToken);
        }

        private async Task AddAsync(GridEventTableEntity tableEntity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _tableClient.AddEntityAsync(tableEntity, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == TableErrorCode.EntityAlreadyExists)
            {
                throw new DuplicateResourceException("GridEvent", tableEntity.RowKey, ex);
            }
        }
    }
}
