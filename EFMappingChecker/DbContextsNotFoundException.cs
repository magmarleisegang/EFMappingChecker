using System;
using System.Runtime.Serialization;

namespace EFMappingChecker
{
    [Serializable]
    internal class DbContextsNotFoundException : Exception
    {
        public DbContextsNotFoundException()
            : base("No classes inheriting System.Data.Entity.DbContext found in the assembly")
        {
        }

        public DbContextsNotFoundException(string dll)
            : base($"No classes inheriting System.Data.Entity.DbContext found in the assembly \"{dll}\"")

        {
        }

        public DbContextsNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DbContextsNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}