using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseObjectExtractor.Services
{
    public class Logger
    {
        private readonly List<string> _errorLog = new();
        private readonly List<string> _successLog = new();

        public void LogError(string message)
        {
            _errorLog.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            Console.WriteLine($"ERROR: {message}");
        }

        public void LogSuccess(string message)
        {
            _successLog.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            Console.WriteLine(message);
        }

        public async Task WriteSummary()
        {
            Console.WriteLine("\n=== Extraction Summary ===");
            Console.WriteLine($"Total Successful Operations: {_successLog.Count}");
            Console.WriteLine($"Total Errors: {_errorLog.Count}");

            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(logDir);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            
            if (_errorLog.Count > 0)
            {
                string errorLogPath = Path.Combine(logDir, $"errors_{timestamp}.log");
                await File.WriteAllLinesAsync(errorLogPath, _errorLog);
                Console.WriteLine($"Error log written to: {errorLogPath}");
            }

            string successLogPath = Path.Combine(logDir, $"success_{timestamp}.log");
            await File.WriteAllLinesAsync(successLogPath, _successLog);
            Console.WriteLine($"Success log written to: {successLogPath}");
        }
    }
}