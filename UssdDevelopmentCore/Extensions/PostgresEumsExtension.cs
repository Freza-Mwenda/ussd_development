using System.Reflection;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UssdDevelopmentCore.Common;

namespace UssdDevelopmentCore.Extensions;

public static class PostgresEumsExtension
{
    public static void MapNpgsqlEnums(this ModelBuilder builder)
    {
        var postgresEnums = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<PostgresEnumAttribute>() != null)
            .ToList();
        
        foreach (var postgresEnum in postgresEnums)
        {
            builder.HasPostgresEnum(postgresEnum.Name, postgresEnum.GetEnumNames().Select(x => x.Underscore()).ToArray());
        }
    }


    public static void RegisterEnumTypeConversion(this NpgsqlDataSourceBuilder builder)
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttribute<PostgresEnumAttribute>() != null)
            .ToList();

        foreach (var type in types)
        {
            builder.MapEnum(type, type.Name);
        }
    }
}