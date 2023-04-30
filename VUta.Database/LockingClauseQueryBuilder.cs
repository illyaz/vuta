namespace VUta.Database
{
    using Microsoft.EntityFrameworkCore;

    using System.Linq;

    public class LockingClauseQueryBuilder<T>
    {
        public IQueryable<T> Query { get; }

        public LockingClauseQueryBuilder(IQueryable<T> query)
        {
            Query = query;
        }

        public LockStrengthQueryBuilder<T> Update()
            => new(this, "UPDATE");

        public LockStrengthQueryBuilder<T> NoKeyUpdate()
            => new(this, "NO KEY UPDATE");

        public LockStrengthQueryBuilder<T> Share()
            => new(this, "SHARE");

        public LockStrengthQueryBuilder<T> KeyShare()
            => new(this, "KEY SHARE");

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
                => clauseBuilder.Query.TagWith($"__FOR:{Clause}");

            public IQueryable<T> NoWait()
                => clauseBuilder.Query.TagWith($"__FOR:{Clause} NOWAIT");

            public IQueryable<T> SkipLocked()
                => clauseBuilder.Query.TagWith($"__FOR:{Clause} SKIP LOCKED");
        }
    }
}
