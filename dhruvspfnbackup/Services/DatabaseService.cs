using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using DatabaseObjectExtractor.Models;
using DatabaseObjectExtractor.Exceptions;

namespace DatabaseObjectExtractor.Services
{
    public class DatabaseService
    {
        private readonly Logger _logger;

        public DatabaseService(Logger logger)
        {
            _logger = logger;
        }

        public async Task ProcessDatabase(DatabaseConfig dbConfig)
        {
            var objectsProcessed = 0;
            var objectsFailed = 0;

            try
            {
                await using var connection = new SqlConnection(dbConfig.ConnectionString);
                await connection.OpenAsync();

                await ValidateDatabaseAccess(connection, dbConfig.Name);

                string query = @"
                    SELECT 
                        ISNULL(OBJECT_SCHEMA_NAME(object_id), 'dbo') as SchemaName,
                        name as ObjectName,
                        type_desc as ObjectType,
                        OBJECT_DEFINITION(object_id) as Definition
                    FROM sys.objects
                    WHERE type in ('P', 'FN', 'TF', 'IF')
                        AND OBJECT_DEFINITION(object_id) IS NOT NULL
                    ORDER BY type_desc, name";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
                        var objectName = reader.GetString(reader.GetOrdinal("ObjectName"));
                        var objectType = reader.GetString(reader.GetOrdinal("ObjectType"));
                        var definition = reader.GetString(reader.GetOrdinal("Definition"));

                        await SaveObjectDefinition(dbConfig.Name, schemaName, objectName, objectType, definition);
                        objectsProcessed++;
                        
                        _logger.LogSuccess($"Exported {objectType}: {schemaName}.{objectName}");
                    }
                    catch (Exception ex)
                    {
                        objectsFailed++;
                        _logger.LogError($"Failed to process object in {dbConfig.Name}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Database {dbConfig.Name} processing completed:");
                Console.WriteLine($"Objects processed successfully: {objectsProcessed}");
                Console.WriteLine($"Objects failed: {objectsFailed}");
            }
            catch (SqlException sqlEx)
            {
                throw new DatabaseExtractionException(dbConfig.Name, 
                    $"SQL Error: {sqlEx.Message} (Error Number: {sqlEx.Number})", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseExtractionException(dbConfig.Name, 
                    "Failed to process database", ex);
            }
        }

        private async Task ValidateDatabaseAccess(SqlConnection connection, string dbName)
        {
            try
            {
                await using var command = new SqlCommand(
                    "SELECT DB_ID(@DatabaseName)", connection);
                command.Parameters.AddWithValue("@DatabaseName", dbName);
                
                var result = await command.ExecuteScalarAsync();
                if (result == DBNull.Value)
                {
                    throw new DatabaseExtractionException(dbName, "Database does not exist");
                }

                await using var permCommand = new SqlCommand(
                    "SELECT HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'VIEW DEFINITION')", connection);
                var hasPermission = (bool?)await permCommand.ExecuteScalarAsync();
                
                if (hasPermission != true)
                {
                    throw new DatabaseExtractionException(dbName, 
                        "User does not have necessary permissions to view object definitions");
                }
            }
            catch (Exception ex) when (ex is not DatabaseExtractionException)
            {
                throw new DatabaseExtractionException(dbName, 
                    "Failed to validate database access", ex);
            }
        }

        private static async Task SaveObjectDefinition(
            string dbName, string schemaName, string objectName, 
            string objectType, string definition)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string fileName = $"{dbName}_{schemaName}_{objectName}_{timestamp}.sql";
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "OutputScripts", dbName);
            
            try
            {
                Directory.CreateDirectory(outputDir);
                string fullPath = Path.Combine(outputDir, fileName);
                await File.WriteAllTextAsync(fullPath, definition);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Failed to save definition for {objectType} {schemaName}.{objectName}", ex);
            }
        }
    }
}