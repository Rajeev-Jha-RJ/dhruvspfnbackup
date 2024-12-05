using System;

namespace DatabaseObjectExtractor.Exceptions
{
    public class DatabaseExtractionException : Exception
    {
        public string DatabaseName { get; }

        public DatabaseExtractionException(string databaseName, string message, Exception? innerException = null) 
            : base(message, innerException)
        {
            DatabaseName = databaseName;
        }
    }
}