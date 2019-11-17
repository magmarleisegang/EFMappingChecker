using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace EFMappingChecker
{

    class MappingCheckerTests
    {

        private string _connectionString;
        private MappingChecker _checkerToTest;
        private DbContextFinder _dbContextFinderToTest;
        private string _dllPath;

        [SetUp]
        public void SetupTest()
        {
            _connectionString = "Data Source=localhost;Initial Catalog=invoset.za;user id=me;pwd=mylogin;MultipleActiveResultSets=True;App=InvosetLocal";
            _dllPath = @"C:\Users\magdalena.leisegang\Documents\Visual Studio 2017\Projects\EFMappingChecker\EFMappingChecker\Debug\Invoset.Data.dll";
            _checkerToTest = new MappingChecker(new List<string> { "" }, _connectionString);
            _dbContextFinderToTest = new DbContextFinder();
        }

   
        [Test]
        public void GivenADbContext_RunDbContextTests()
        {
            _dbContextFinderToTest.LoadDbContextTypes(_dllPath);

            var xclusionList = new string[] {
                "OfferPayments",
                "BuyerAdminAllowances"
            };
            DbContext resultContext;
            _checkerToTest.SetupExclusionList(xclusionList);
            var result = _dbContextFinderToTest.GetNextDbContext(_connectionString, out resultContext);
            
            _checkerToTest.TestDbContext(resultContext);
        }

        [Test]
        public void GivenADbContextAndASpecificDbContextName_RunDbContextTests()
        {
            var xclusionList = new string[] {
                "OfferPayments",
                "BuyerAdminAllowances"
            };
            _checkerToTest.SetupExclusionList(xclusionList);
            
            _checkerToTest.TestSpecificDbContext(_dllPath, "ApiDatabaseContext");            
        }
    }
}
