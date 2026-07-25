namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Generates tenant-scoped, gap-free, never-reused sequence numbers (e.g. for employee codes)
/// using row-level locking so concurrent creates never race to the same value.
/// </summary>
public interface ISequenceGenerator
{
    Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default);
}
