using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EFMappingChecker
{
    public class StoredProcedureTester
    {
        public StoredProcedureTester()
        {
            _exclusionList = new List<string>();
            _dbContextFinder = new DbContextFinder();
        }

        public string ConnectionString { get { return _connectionString; } set { _connectionString = value; } }
        public string DllToTest { get { return _dllToTest; } set { _dllToTest = value; } }

        public string ResultFilePath { get; internal set; }

        public StoredProcedureTester(List<string> dllFiles, string testDbConnectionString)
          : this()
        {
            _dllFiles = dllFiles;
            _connectionString = testDbConnectionString;
        }

        private List<string> _exclusionList;
        private readonly List<string> _dllFiles;
        private string _connectionString;
        private string _dllToTest;
        private DbContextFinder _dbContextFinder;

        //public void TestAllProcMethods()
        //{
        //    _dbContextFinder.LoadDbContextTypes(_dllFile);

        //    while (_dbContextFinder.GetNextDbContext(_connectionString, out DbContext dbContext))
        //    {
        //        TestDbContext(dbContext);
        //    }
        //}
        public void TestAssemblyDbContexts(string dllFile)
        {
            _dbContextFinder.LoadDbContextTypes(dllFile);

            while (_dbContextFinder.GetNextDbContext(_connectionString, out DbContext dbContext))
            {
                TestDbContext(dbContext);
            }
        }

        internal void TestAssemblyDbContext()
        {
            TestAssemblyDbContexts(_dllToTest);
        }


        public void TestDbContext(DbContext dbContext)
        {
            var dbContextName = dbContext.GetType().FullName;
            SetResultFilePath(dbContextName);
            string reportPath = ResultFilePath;

            int dbSetCount = 0;
            int errorCount = 0;
            int excludedCount = 0;

            using (FileStream textOutput = System.IO.File.Create(reportPath))
            using (BinaryWriter textOutputWriter = new BinaryWriter(textOutput))
            {
                var resultFileHeader = ASCIIEncoding.ASCII.GetBytes(string.Format("Testing {0}\n\n", dbContextName));
                textOutputWriter.Write(resultFileHeader);
                var dbContextType = dbContext.GetType();

                var methods = dbContextType
                    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                    .Where(x => !x.IsSpecialName);
                var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)dbContext).ObjectContext;


                foreach (var methodInfo in methods)
                {
                    dbSetCount++;
                    Debug.WriteLine(methodInfo.Name);
                    //if (InExclusionList(dbSetPropertyInfo))
                    //{
                    //    excludedCount++;
                    //    Debug.WriteLine(dbSetPropertyInfo.Name + " excluded");

                    //    continue;
                    //}

                    try
                    {
                        object[] parametersArray = GetParameterArray(methodInfo);

                        methodInfo.Invoke(dbContext, parametersArray);
                    }
                    catch (NotImplementedException te)
                    {
                        errorCount++;
                        StringBuilder errorMessage = new StringBuilder();
                        errorMessage.AppendLine(methodInfo.Name);

                        errorMessage.AppendLine(te.ToString());

                        errorMessage.AppendLine(" ");
                        var byteArray = ASCIIEncoding.ASCII.GetBytes(errorMessage.ToString());
                        textOutputWriter.Write(byteArray);
                    }
                    catch (TargetInvocationException ex)
                    {
                        errorCount++;
                        StringBuilder errorMessage = new StringBuilder();
                        errorMessage.AppendLine(methodInfo.Name);

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
                    }
                }
                var resultSummary = string.Format("Tested {0} methods. {1} passed, {2} exluced, {3} failed", dbSetCount, dbSetCount - excludedCount - errorCount, excludedCount, errorCount);
                var resultSummaryBytes = ASCIIEncoding.ASCII.GetBytes(resultSummary);
                textOutputWriter.Write(resultSummaryBytes);
                textOutputWriter.Flush();
            }
        }

        private object[] GetParameterArray(MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();
            var parameterArray = new object[parameterInfos.Length];
            int index = 0;
            foreach (var parameterInfo in parameterInfos)
            {
                var parameterType = parameterInfo.ParameterType;
                if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    parameterType = Nullable.GetUnderlyingType(parameterType);
                }

                object defaultValue = Activator.CreateInstance(parameterType);
                parameterArray[index++] = defaultValue;
            }
            return parameterArray;
        }


        private void SetResultFilePath(string dbContextName)
        {
            var defaultFileName = System.IO.Path.Combine("c:\\test", string.Format("EF storedProc test output {0}_{1:yyMMddHHmm}.txt", dbContextName, DateTime.Now));
            if (string.IsNullOrEmpty(ResultFilePath))
                ResultFilePath = defaultFileName;
            else if (Directory.Exists(Path.GetDirectoryName(ResultFilePath)) == false)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ResultFilePath));
                }
                catch
                {
                    ResultFilePath = defaultFileName;
                }
            }
        }
    }
}
