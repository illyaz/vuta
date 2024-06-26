﻿using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace VUta.Database;

public class VUtaNpgsqlQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
    private readonly QuerySqlGeneratorDependencies dependencies;
    private readonly INpgsqlSingletonOptions npgsqlSingletonOptions;

    public VUtaNpgsqlQuerySqlGeneratorFactory(
        QuerySqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlSingletonOptions)
    {
        this.dependencies = dependencies;
        this.npgsqlSingletonOptions = npgsqlSingletonOptions;
    }

    public virtual QuerySqlGenerator Create()
    {
        return new VUtaNpgsqlQuerySqlGenerator(
            dependencies,
            npgsqlSingletonOptions.ReverseNullOrderingEnabled,
            npgsqlSingletonOptions.PostgresVersion);
    }
}