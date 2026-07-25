using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Persistence;

public class SequenceGenerator(ApplicationDbContext dbContext, ITenantContext tenantContext) : ISequenceGenerator
{
    public async Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // FOR UPDATE row-locks the counter for the duration of the transaction, so two
        // concurrent employee creates can never be handed the same code.
        var sequence = await dbContext.TenantSequences
            .FromSqlInterpolated(
                $"SELECT * FROM TenantSequences WHERE TenantId = {tenantId} AND Name = {sequenceName} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (sequence is null)
        {
            sequence = new TenantSequence { TenantId = tenantId, Name = sequenceName, NextValue = 1 };
            dbContext.TenantSequences.Add(sequence);
        }

        var value = sequence.NextValue;
        sequence.NextValue++;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return value;
    }
}
