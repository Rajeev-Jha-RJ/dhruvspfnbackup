using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DatabaseObjectExtractor.Models;

namespace DatabaseObjectExtractor.Services
{
    public class ConfigurationService
    {
        public void ValidateEnvironment()
        {
            if (!File.Exists("appsettings.json"))
            {
                throw new FileNotFoundException("Configuration file 'appsettings.json' not found in the current directory.");
            }

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "OutputScripts");
            try
            {
                Directory.CreateDirectory(outputDir);
                string testFile = Path.Combine(outputDir, ".test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Unable to write to output directory: {outputDir}", ex);
            }
        }

        public List<DatabaseConfig> LoadConfiguration()
        {
            try
            {
                IConfiguration config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var databases = config.GetSection("Databases").Get<List<DatabaseConfig>>();

                if (databases == null || databases.Count == 0)
                {
                    throw new InvalidOperationException("No databases configured in appsettings.json");
                }

                ValidateDatabaseConfigs(databases);
                return databases;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load configuration", ex);
            }
        }

        private void ValidateDatabaseConfigs(List<DatabaseConfig> databases)
        {
            foreach (var db in databases)
            {
                if (string.IsNullOrEmpty(db.Name))
                {
                    throw new InvalidOperationException("Database name cannot be empty");
                }

                if (string.IsNullOrEmpty(db.ConnectionString))
                {
                    throw new InvalidOperationException($"Connection string for database '{db.Name}' cannot be empty");
                }

                try
                {
                    var builder = new SqlConnectionStringBuilder(db.ConnectionString);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Invalid connection string for database '{db.Name}'", ex);
                }
            }
        }
    }
}