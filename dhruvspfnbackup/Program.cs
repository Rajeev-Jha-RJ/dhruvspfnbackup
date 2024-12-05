using System;
using System.Threading.Tasks;
using DatabaseObjectExtractor.Exceptions;
using DatabaseObjectExtractor.Services;

namespace DatabaseObjectExtractor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new Logger();
            var configService = new ConfigurationService();
            var dbService = new DatabaseService(logger);

            try
            {
                Console.WriteLine("Database Object Extractor Starting...");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

                configService.ValidateEnvironment();
                var databases = configService.LoadConfiguration();

                foreach (var db in databases)
                {
                    try
                    {
                        Console.WriteLine($"\nProcessing database: {db.Name}");
                        await dbService.ProcessDatabase(db);
                    }
                    catch (DatabaseExtractionException dbEx)
                    {
                        logger.LogError($"Failed to process database {db.Name}: {dbEx.Message}");
                        if (dbEx.InnerException != null)
                        {
                            logger.LogError($"Caused by: {dbEx.InnerException.Message}");
                        }
                    }
                }

                await logger.WriteSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nCRITICAL ERROR:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Caused by: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
        }
    }
}