using System.Collections.Concurrent;
using ImportStudio.Api.Models;

namespace ImportStudio.Api.Services;

public sealed class ImportJobStore
{
    private readonly ConcurrentDictionary<Guid, ImportJob> _jobs = new();

    public ImportJob Add(ImportJob job)
    {
        _jobs[job.Id] = job;
        return job;
    }

    public bool TryGet(Guid id, out ImportJob? job)
    {
        return _jobs.TryGetValue(id, out job);
    }
}
