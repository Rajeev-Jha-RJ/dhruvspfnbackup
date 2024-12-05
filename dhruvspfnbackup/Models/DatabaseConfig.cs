using System;

namespace DatabaseObjectExtractor.Models
{
    public class DatabaseConfig
    {
        public required string Name { get; set; }
        public required string ConnectionString { get; set; }
    }
}