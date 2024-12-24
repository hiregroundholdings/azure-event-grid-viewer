namespace Viewer
{
    using System.Threading.Tasks;
    using Viewer.Models;

    public interface IGridEventRepository
    {
        Task AddAsync<T>(CloudEvent<T> cloudEvent, CancellationToken cancellationToken = default)
            where T : class;

        Task AddAsync<T>(GridEvent<T> gridEvent, CancellationToken cancellationToken = default)
            where T : class;
    }
}
