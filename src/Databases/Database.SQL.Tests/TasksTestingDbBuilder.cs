using Database.SQL.EF;
using Database.SQL.EF.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Database.SQL.Tests
{
    public class TasksDbBuilder : ICanCreateTables, ICanPopulateTables, ICanSaveChanges
    {
        private readonly TasksDbContext _context;

        private TasksDbBuilder(TasksDbContext context)
        {
            _context = context;

            if (_context.Database.GetDbConnection().State == ConnectionState.Closed)
            {
                _context.Database.OpenConnection();

                // Schema hack to use generated SQL Server SQL with SQL Lite
                RunSql(new RawSqlString("ATTACH DATABASE ':memory:' AS dbo"));
            }
        }

        public static ICanCreateTables UsingDbContext(TasksDbContext context)
        {
            return new TasksDbBuilder(context);
        }

        public ICanPopulateTables DeleteAllRowData()
        {

            if (_context.Database.GetDbConnection().State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"Database connection closed. Did you mean to call {nameof(CreateTables)}?");
            }

            _context.Tasks.RemoveRange(_context.Tasks.ToList());
            return this;
        }

        public ICanPopulateTables CreateTables()
        {
            if (!File.Exists(@"Tables\Tasks.sql")) return this;

            RunSql(new RawSqlString(File.ReadAllText(@"Tables\Tasks.sql")));

            return this;
        }

        private void RunSql(RawSqlString sqlString)
        {
            // Not ideal mixing SQL with EF
            _context.Database.ExecuteSqlCommand(sqlString);
            SaveChanges();
        }

        public ICanSaveChanges PopulateTables()
        {

            if (!File.Exists(@"Data\TasksSeedData.json")) return this;

            var jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<Task>>(jsonString);

            _context.Tasks.AddRange(tasks);

            return this;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }

    public interface ICanSaveChanges
    {
        void SaveChanges();
    }

    public interface ICanPopulateTables
    {
        ICanSaveChanges PopulateTables();
    }

    public interface ICanCreateTables
    {
        ICanPopulateTables CreateTables();
        ICanPopulateTables DeleteAllRowData();
    }
}
