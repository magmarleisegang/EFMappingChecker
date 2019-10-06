using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MI.ConsoleArguments;

namespace EFMappingChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: -dll <ddl.name.dll> [-conn \"<connection string>\"] [-out \"<output file>.txt\"]");
            }
            else
            {
                Dictionary<string, Action<string, MappingChecker>> argsList = new Dictionary<string, Action<string, MappingChecker>>();
                argsList.Add("-dll", (val, result) => { result.DllToTest = val; });
                argsList.Add("-conn", (val, result) => { result.ConnectionString = val; });
                argsList.Add("-out", (val, result) => { result.ResultFilePath = val; });


                //var argsList = new Dictionary<string>
                var checker = MI.ConsoleArguments.ConsoleArgumentParser.ParseArgs<MappingChecker>(args, argsList);


                if (string.IsNullOrEmpty(checker.ConnectionString))
                    checker.ConnectionString = ConfigurationManager.ConnectionStrings["dbToCheck"].ConnectionString;
                Console.WriteLine("Checking mapping for: " + args[0]);
                Console.WriteLine("Comparing to db: " + checker.ConnectionString);
                //var checker = new MappingChecker(new List<string> { "" }, connectionString);

                checker.TestAssemblyDbContext();
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
