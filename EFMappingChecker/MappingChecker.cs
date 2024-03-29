﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
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
        {
            _exclusionList = new List<string>();
            _dbContextFinder = new DbContextFinder();
        }

        public string ConnectionString { get { return _connectionString; } set { _connectionString = value; } }
        public string DllToTest { get { return _dllToTest; } set { _dllToTest = value; } }

        public string ResultFilePath { get; internal set; }

        public MappingChecker(List<string> dllFiles, string testDbConnectionString)
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

        public void TestSpecificDbContext(string dllFile, string dbContextName)
        {
            _dbContextFinder.LoadDbContextTypes(dllFile);
            DbContext dbContext = _dbContextFinder.GetSpecificDbContext(dbContextName, _connectionString);
            TestDbContext(dbContext);
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

                var properties = dbContextType.GetProperties();
                var objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)dbContext).ObjectContext;

                var dbSetPropertyInfos = properties.Where(p => p.PropertyType.Name == "DbSet`1");
                foreach (var dbSetPropertyInfo in dbSetPropertyInfos)
                {
                    dbSetCount++;
                    Debug.WriteLine(dbSetPropertyInfo.Name);
                    if (InExclusionList(dbSetPropertyInfo))
                    {
                        excludedCount++;
                        Debug.WriteLine(dbSetPropertyInfo.Name + " excluded");

                        continue;
                    }

                    var dbSetValue = dbSetPropertyInfo.GetValue(dbContext);


                    var findMethod = dbSetValue.GetType().GetMethod("Find");
                    try
                    {
                        var parametersArray = GetPrimaryKeys(objectContext, dbSetValue);

                        findMethod.Invoke(dbSetValue, new object[] { parametersArray });
                    }
                    catch (UnsupportedPrimaryKeyPrimitiveTypeException te)
                    {
                        errorCount++;
                        StringBuilder errorMessage = new StringBuilder();
                        errorMessage.AppendLine(dbSetPropertyInfo.Name);

                        errorMessage.AppendLine(te.ToString());

                        errorMessage.AppendLine(" ");
                        var byteArray = ASCIIEncoding.ASCII.GetBytes(errorMessage.ToString());
                        textOutputWriter.Write(byteArray);
                    }
                    catch (TargetInvocationException ex)
                    {
                        errorCount++;
                        StringBuilder errorMessage = new StringBuilder();
                        errorMessage.AppendLine(dbSetPropertyInfo.Name);

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
                var resultSummary = string.Format("Tested {0} DbSets. {1} passed, {2} exluced, {3} failed", dbSetCount, dbSetCount - excludedCount - errorCount, excludedCount, errorCount);
                var resultSummaryBytes = ASCIIEncoding.ASCII.GetBytes(resultSummary);
                textOutputWriter.Write(resultSummaryBytes);
                textOutputWriter.Flush();
            }
        }

        private object[] GetPrimaryKeys(ObjectContext objectContext, object dbSetValue)
        {
            var dbSetType = dbSetValue.GetType();
            var entityType = dbSetType.GenericTypeArguments[0];


            var primaryKeyTypes = objectContext.MetadataWorkspace
                .GetType(entityType.Name, entityType.Namespace, System.Data.Entity.Core.Metadata.Edm.DataSpace.OSpace)
                .MetadataProperties
                .Where(mp => mp.Name == "KeyMembers")
                .SelectMany(mp => mp.Value as ReadOnlyMetadataCollection<EdmMember>)
                .OfType<EdmProperty>().Select(ep => new { ep.Name, ep.PrimitiveType });

            var parameterList = new object[primaryKeyTypes.Count()];
            var i = 0;
            var errors = new List<UnsupportedPrimaryKeyPrimitiveType>();
            foreach (var pkType in primaryKeyTypes)
            {
                switch (pkType.PrimitiveType.PrimitiveTypeKind)
                {
                    case PrimitiveTypeKind.Double:
                        parameterList[i] = 1.00;
                        break;
                    case PrimitiveTypeKind.Guid:
                        parameterList[i] = System.Guid.NewGuid();
                        break;
                    case PrimitiveTypeKind.Int16:
                        parameterList[i] = (short)1;

                        break;
                    case PrimitiveTypeKind.Int32:
                        parameterList[i] = 1;

                        break;
                    case PrimitiveTypeKind.Int64:
                        parameterList[i] = (long)1;

                        break;
                    case PrimitiveTypeKind.String:
                        parameterList[i] = "1";
                        break;
                    case PrimitiveTypeKind.Boolean:
                        parameterList[i] = true;
                        break;
                    case PrimitiveTypeKind.DateTime:
                        parameterList[i] = DateTime.Now;
                        break;
                    case PrimitiveTypeKind.Decimal:
                        parameterList[i] = 1M;
                        break;
                    default:
                        errors.Add(new UnsupportedPrimaryKeyPrimitiveType(pkType.Name, pkType.PrimitiveType.PrimitiveTypeKind));
                        break;
                }
                i++;
            }
            if (errors.Count > 0)
                throw new UnsupportedPrimaryKeyPrimitiveTypeException(errors);
            return parameterList;
        }

        private void SetResultFilePath(object dbContextName)
        {
            var defaultFileName = System.IO.Path.Combine("c:\\test", string.Format("EF test output {0}_{1:yyMMddHHmm}.txt", dbContextName, DateTime.Now));
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


        private bool InExclusionList(PropertyInfo set)
        {
            return _exclusionList.Contains(set.Name);
        }
    }
}
