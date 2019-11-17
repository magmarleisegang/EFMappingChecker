using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMappingChecker.Tests
{
    class StoredProcTesterTests
    {
        private string _connectionString;
        private StoredProcedureTester _testerToTest;
        private DbContextFinder _dbContextFinderToTest;
        private string _dllPath;

        [SetUp]
        public void SetupTest()
        {
            _connectionString = "Data Source=localhost;Initial Catalog=eAuction_UAT;Integrated Security=SSPI;pwd=mylogin;MultipleActiveResultSets=True;App=EFMappingChecker";
            _dllPath = @"C:\Entelect\ediamond-auctionplatform\src\EDiamond.DAL\bin\Debug eDiamond\EDiamond.DAL.dll";
            _testerToTest = new StoredProcedureTester(new List<string> { "" }, _connectionString);
            _dbContextFinderToTest = new DbContextFinder();
        }

        [Test]
        public void GivenADbContext_RunDbContextStoredProcedureTests()
        {
            _dbContextFinderToTest.LoadDbContextTypes(_dllPath);
            DbContext resultContext;
            var result = _dbContextFinderToTest.GetNextDbContext(_connectionString, out resultContext);

            _testerToTest.TestDbContext(resultContext);
        }


    }
}
