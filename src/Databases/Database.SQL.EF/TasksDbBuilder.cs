using Database.SQL.EF.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace Database.SQL.EF
{
    public class TasksDbBuilder : ICanCreateTables, ICanPopulateTables
    {
        private readonly DbContextOptions<TasksDbContext> _dbContextOptions;

        private TasksDbBuilder(DbConnection dbConnection)
        {
            _dbContextOptions = new DbContextOptionsBuilder<TasksDbContext>()
                .UseSqlite(dbConnection)
                .Options;

            // Schema hack to use generated SQL Server SQL with SQL Lite
            RunSql(new RawSqlString("ATTACH DATABASE ':memory:' AS dbo"));
        }

        public static ICanCreateTables UsingConnection(DbConnection dbConnection)
        {
            return new TasksDbBuilder(dbConnection);
        }

        public ICanPopulateTables CreateTables()
        {
            if (!File.Exists(@"Tables\Tasks.sql")) return this;

            RunSql(new RawSqlString(File.ReadAllText(@"Tables\Tasks.sql")));

            return this;
        }

        private void RunSql(RawSqlString sqlString)
        {
            using (var context = new TasksDbContext(_dbContextOptions))
            {
                context.Database.ExecuteSqlCommand(sqlString);
                context.SaveChanges();
            }
        }

        public DbContextOptions<TasksDbContext> PopulateTables()
        {
            using (var context = new TasksDbContext(_dbContextOptions))
            {
                if (!File.Exists(@"Data\TasksSeedData.json")) return _dbContextOptions;

                var jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
                var tasks = JsonConvert.DeserializeObject<IEnumerable<Task>>(jsonString);

                context.Tasks.AddRange(tasks);
                context.SaveChanges();
            }

            return _dbContextOptions;
        }
    }

    public interface ICanPopulateTables
    {
        DbContextOptions<TasksDbContext> PopulateTables();
    }

    public interface ICanCreateTables
    {
        ICanPopulateTables CreateTables();
    }
}
