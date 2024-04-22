using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace VUta.Database;

public class VUtaNpgsqlQuerySqlGenerator : NpgsqlQuerySqlGenerator
{
    private string? forQuery;

    public VUtaNpgsqlQuerySqlGenerator(
        QuerySqlGeneratorDependencies dependencies,
        bool reverseNullOrderingEnabled, Version postgresVersion)
        : base(dependencies, reverseNullOrderingEnabled, postgresVersion)
    {
    }

    protected override void GenerateTagsHeaderComment(ISet<string> tags)
    {
        forQuery = tags.FirstOrDefault(x => x.StartsWith("__FOR:"));
        if (forQuery != null)
        {
            tags.Remove(forQuery);
            forQuery = forQuery[6..];
        }

        base.GenerateTagsHeaderComment(tags);
    }

    protected override void GenerateLimitOffset(SelectExpression selectExpression)
    {
        base.GenerateLimitOffset(selectExpression);

        if (forQuery != null)
            Sql.AppendLine()
                .Append("FOR ")
                .Append(forQuery);
    }
}