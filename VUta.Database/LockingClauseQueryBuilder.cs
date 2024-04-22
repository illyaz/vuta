using Microsoft.EntityFrameworkCore;

namespace VUta.Database;

public class LockingClauseQueryBuilder<T>
{
    public LockingClauseQueryBuilder(IQueryable<T> query)
    {
        Query = query;
    }

    public IQueryable<T> Query { get; }

    public LockStrengthQueryBuilder<T> Update()
    {
        return new LockStrengthQueryBuilder<T>(this, "UPDATE");
    }

    public LockStrengthQueryBuilder<T> NoKeyUpdate()
    {
        return new LockStrengthQueryBuilder<T>(this, "NO KEY UPDATE");
    }

    public LockStrengthQueryBuilder<T> Share()
    {
        return new LockStrengthQueryBuilder<T>(this, "SHARE");
    }

    public LockStrengthQueryBuilder<T> KeyShare()
    {
        return new LockStrengthQueryBuilder<T>(this, "KEY SHARE");
    }

    public class LockStrengthQueryBuilder<T>
    {
        private readonly LockingClauseQueryBuilder<T> clauseBuilder;

        public LockStrengthQueryBuilder(LockingClauseQueryBuilder<T> clauseBuilder, string clause)
        {
            this.clauseBuilder = clauseBuilder;
            Clause = clause;
        }

        public string Clause { get; }

        public IQueryable<T> Wait()
        {
            return clauseBuilder.Query.TagWith($"__FOR:{Clause}");
        }

        public IQueryable<T> NoWait()
        {
            return clauseBuilder.Query.TagWith($"__FOR:{Clause} NOWAIT");
        }

        public IQueryable<T> SkipLocked()
        {
            return clauseBuilder.Query.TagWith($"__FOR:{Clause} SKIP LOCKED");
        }
    }
}