using NUnit.Framework;
using System.Data.Entity;

namespace EFMappingChecker
{

    class DbContextFinderTests
    {

        private string _connectionString;
        private DbContextFinder _dbContextFinderToTest;
        private string _dllPath;

        [SetUp]
        public void SetupTest()
        {
            _connectionString = "Data Source=localhost;Initial Catalog=invoset.za;user id=me;pwd=mylogin;MultipleActiveResultSets=True;App=InvosetLocal";
            _dllPath = @"C:\Users\magdalena.leisegang\Documents\Visual Studio 2017\Projects\EFMappingChecker\EFMappingChecker\Debug\Invoset.Data.dll";
            _dbContextFinderToTest = new DbContextFinder();
        }

        [Test]
        public void GivenAnAssemblyWithTwoClassesInheritingFromDbContext_WhenGettingAllDbContextTypes_ItShouldReturnAnEnumerableOfLength2()
        {
            _dbContextFinderToTest.LoadDbContextTypes(_dllPath);

            Assert.AreEqual(2, _dbContextFinderToTest.DbContextCount(), "Not all db contexts were found");
        }

        [Test]
        public void GivenAnEnumeratorWithTypesInheritingFromDbContext_WhenGettingADbContext_ItShouldReturnADbContext()
        {
            _dbContextFinderToTest.LoadDbContextTypes(_dllPath);

            DbContext resultContext;

            var result = _dbContextFinderToTest.GetNextDbContext(_connectionString, out resultContext);
            Assert.True(result);
            Assert.NotNull(resultContext);

        }

    }
}
