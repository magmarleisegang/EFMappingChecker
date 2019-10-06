using System;
using System.Collections.Generic;
using System.Configuration;

namespace EFMappingChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: -dll <ddl.name.dll> [-conn \"<connection string>\"] [-out \"<output file>.txt\"] [-i \"ignore dbset name, ignore dbset name,...\"]");
            }
            else
            {
                Dictionary<string, Action<string, MappingChecker>> argsList = new Dictionary<string, Action<string, MappingChecker>>();
                argsList.Add("-dll", (val, result) => { result.DllToTest = val; });
                argsList.Add("-conn", (val, result) => { result.ConnectionString = val; });
                argsList.Add("-out", (val, result) => { result.ResultFilePath = val; });

                bool promptForIgnoreList = false;
                argsList.Add("-i", (val, result) =>
                {
                    promptForIgnoreList = (val == "?" ? true : false);
                    if (val != "?")
                    {
                        result.SetupExclusionList(val.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                    }
                });
                var checker = MI.ConsoleArguments.ConsoleArgumentParser.ParseArgs<MappingChecker>(args, argsList);

                List<string> ignoreList = new List<string>();

                if (promptForIgnoreList)
                {
                    var toIgnore = MI.ConsoleArguments.ConsoleArgumentPrompter.PromptUser("Enter a DbSet name to ignore:");
                    while (string.IsNullOrEmpty(toIgnore) == false)
                    {
                        ignoreList.Add(toIgnore);
                        toIgnore = MI.ConsoleArguments.ConsoleArgumentPrompter.PromptUser("Enter a DbSet name to ignore:");
                    }
                }
                checker.SetupExclusionList(ignoreList.ToArray());
                if (string.IsNullOrEmpty(checker.ConnectionString))
                    checker.ConnectionString = ConfigurationManager.ConnectionStrings["dbToCheck"].ConnectionString;
                Console.WriteLine("Checking mapping for: " + checker.DllToTest);
                Console.WriteLine("Comparing to db: " + checker.ConnectionString);
                try
                {
                    checker.TestAssemblyDbContext();

                    Console.WriteLine("Test Complete.");
                    Console.WriteLine($"Please find results here: {checker.ResultFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
