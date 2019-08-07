using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMappingChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["dbToCheck"].ConnectionString;
            Console.WriteLine("Checking mapping for: " + args[0]);
            Console.WriteLine("Comparing to db: " + connectionString);
            var checker = new MappingChecker(new List<string> { "" }, connectionString);
            
            checker.TestAssemblyDbContexts(args[0]);
        }
    }
}
