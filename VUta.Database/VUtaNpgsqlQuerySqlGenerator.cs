using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace VUta.Database;

public class VUtaNpgsqlQuerySqlGenerator(
    QuerySqlGeneratorDependencies dependencies,
    IRelationalTypeMappingSource typeMappingSource,
    bool reverseNullOrderingEnabled,
    Version postgresVersion)
    : NpgsqlQuerySqlGenerator(dependencies, typeMappingSource, reverseNullOrderingEnabled, postgresVersion)
{
    private string? forQuery;


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