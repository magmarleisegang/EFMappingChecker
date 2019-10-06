﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EFMappingChecker
{
    public class MappingChecker
    {
        public MappingChecker()
        { }

        public string ConnectionString { get { return _connectionString; } set { _connectionString = value; } }
        public string DllToTest { set { _dllToTest = value; } }

        public string ResultFilePath { get; internal set; }

        public MappingChecker(List<string> dllFiles, string testDbConnectionString)
        {
            _dllFiles = dllFiles;
            _connectionString = testDbConnectionString;
            _exclusionList = new List<string>();
        }

        private List<string> _exclusionList;
        private readonly List<string> _dllFiles;
        private string _connectionString;
        private string _dllToTest;

        public void SetupExclusionList(params string[] classesToExclude)
        {
            foreach (var className in classesToExclude)
            {
                _exclusionList.Add(className);
            }
        }

        //public void QuickTest()
        //{
        //    DatabaseContext dbContext = GetDbContext();

        //    var it = dbContext.DocumentFieldOrigins.Find(128);
        //}

        public void TestAssemblyDbContexts(string dllFile)
        {
            IEnumerable<Type> dbContextTypes = GetDbContextTypes(dllFile);
            var dbContextEnumerator = dbContextTypes.GetEnumerator();
            DbContext dbContext;
            while (GetNextDbContext(dbContextEnumerator, out dbContext))
            {
                TestDbContext(dbContext);
            }
        }

        internal void TestAssemblyDbContext()
        {
            TestAssemblyDbContexts(_dllToTest);
        }

        public void TestSpecificDbContext(string dllFile, string dbContextName)
        {
            IEnumerable<Type> dbContextTypes = GetDbContextTypes(dllFile);
            DbContext dbContext = GetSpecificDbContext(dbContextTypes, dbContextName);
            TestDbContext(dbContext);
        }

        private DbContext GetSpecificDbContext(IEnumerable<Type> dbContextTypes, string dbContextName)
        {
            var enumerator = dbContextTypes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Name.Equals(dbContextName))
                {
                    var constructor = enumerator.Current.GetConstructor(new[] { typeof(string) });
                    return (DbContext)constructor.Invoke(new object[] { _connectionString });
                }
            }
            throw new Exception(string.Format("Failed to get specific DbContext: {0}", dbContextName));
        }

        public void TestDbContext(DbContext dbContext)
        {
            var dbContextName = dbContext.GetType().FullName;
            string reportPath = GetResultFilePath(dbContextName);

            int dbSetCount = 0;
            int errorCount = 0;
            int excludedCount = 0;

            using (FileStream textOutput = System.IO.File.Create(reportPath))
            using (BinaryWriter textOutputWriter = new BinaryWriter(textOutput))
            {
                var resultFileHeader = ASCIIEncoding.ASCII.GetBytes(string.Format("Testing {0}\n\n", dbContextName));
                textOutputWriter.Write(resultFileHeader);
                var dbContextType = dbContext.GetType();

                var properties = dbContextType.GetProperties();
                var dbSets = properties.Where(p => p.PropertyType.Name == "DbSet`1");
                foreach (var set in dbSets)
                {
                    dbSetCount++;
                    Debug.WriteLine(set.Name);
                    if (InExclusionList(set))
                    {
                        excludedCount++;
                        Debug.WriteLine(set.Name + " excluded");

                        continue;
                    }
                    var setType = set.GetType();
                    var value = set.GetValue(dbContext);
                    var findMethod = value.GetType().GetMethod("Find");
                    //var getMethod = set.GetMethod;
                    //var returnParameter = getMethod.ReturnParameter;
                    //var parameterType = returnParameter.ParameterType;
                    //var baseType = parameterType.GenericTypeArguments.First();
                    //var name = baseType.FullName;
                    object[] parametersArray = new object[1] { 1 };
                    try
                    {
                        findMethod.Invoke(value, new object[] { parametersArray });
                    }
                    catch (TargetInvocationException ex)
                    {
                        errorCount++;
                        StringBuilder errorMessage = new StringBuilder();
                        errorMessage.AppendLine(set.Name);

                        if (ex.InnerException != null && ex.InnerException.InnerException != null)
                        {
                            errorMessage.AppendLine(ex.InnerException.InnerException.Message);
                        }
                        else if (ex.InnerException != null)
                        {
                            errorMessage.AppendLine(ex.InnerException.Message);
                        }
                        else
                        {
                            errorMessage.AppendLine(ex.Message);
                        }

                        errorMessage.AppendLine(" ");
                        var byteArray = ASCIIEncoding.ASCII.GetBytes(errorMessage.ToString());
                        textOutputWriter.Write(byteArray);

                        //Debug.WriteLine(set.Name + " failed");
                        //Debug.WriteLine(set.Name+": "+ex.InnerException.Message);
                        //throw ex.InnerException;
                    }
                }
                var resultSummary = string.Format("Tested {0} DbSets. {1} passed, {2} exluced, {3} failed", dbSetCount, dbSetCount - excludedCount - errorCount, excludedCount, errorCount);
                var resultSummaryBytes = ASCIIEncoding.ASCII.GetBytes(resultSummary);
                textOutputWriter.Write(resultSummaryBytes);
                textOutputWriter.Flush();
            }
        }

        private string GetResultFilePath(object dbContextName)
        {
            var defaultFileName = System.IO.Path.Combine("c:\\test", string.Format("EF test output {0}_{1:yyMMddHHmm}.txt", dbContextName, DateTime.Now));
            if (string.IsNullOrEmpty(ResultFilePath))
                return  defaultFileName;
            else if (Directory.Exists(Path.GetDirectoryName(ResultFilePath)) == false)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ResultFilePath));
                }
                catch
                {
                    return defaultFileName;
                }
            }
            return ResultFilePath;
        }

        public bool GetNextDbContext(IEnumerator<Type> dbContextTypes, out DbContext nextDbContext)
        {
            if (dbContextTypes.MoveNext())
            {
                var currentType = dbContextTypes.Current;
                var constructor = currentType.GetConstructor(new[] { typeof(string) });
                nextDbContext = (DbContext)constructor.Invoke(new object[] { _connectionString });
                return true;
            }
            else
            {
                nextDbContext = null;
                return false;
            }
        }

        public IEnumerable<Type> GetDbContextTypes(string dllFile)
        {
            if (File.Exists(dllFile))
            {
                Assembly asm = Assembly.LoadFrom(dllFile);
                var dbContextTypes = asm.ExportedTypes.Where(type => type.IsSubclassOf(typeof(DbContext)));
                return dbContextTypes;
            }
            throw new Exception("No classes inheriting System.Data.Entity.DbContext found in the assembly");
        }

        private bool InExclusionList(PropertyInfo set)
        {
            return _exclusionList.Contains(set.Name);
        }
    }
}
