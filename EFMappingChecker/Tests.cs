using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace EFMappingChecker
{

    class Tests
    {

        private string _connectionString;
        private MappingChecker _checkerToTest;
        private string _dllPath;

        [SetUp]
        public void SetupTest()
        {
            _connectionString = "Data Source=localhost;Initial Catalog=invoset.za;user id=me;pwd=mylogin;MultipleActiveResultSets=True;App=InvosetLocal";
            _dllPath = @"C:\Users\magdalena.leisegang\Documents\Visual Studio 2017\Projects\EFMappingChecker\EFMappingChecker\Debug\Invoset.Data.dll";
            _checkerToTest = new MappingChecker(new List<string> { "" }, _connectionString);
        }

        [Test]
        public void GivenAnAssemblyWithTwoClassesInheritingFromDbContext_WhenGettingAllDbContextTypes_ItShouldReturnAnEnumerableOfLength2()
        {
             var result = _checkerToTest.GetDbContextTypes(_dllPath);

            Assert.IsInstanceOf<IEnumerable<Type>>(result, "result not an enumerable");
            Assert.AreEqual(2, result.Count(), "Not all db contexts were found");
        }

        [Test]
        public void GivenAnEnumeratorWithTypesInheritingFromDbContext_WhenGettingADbContext_ItShouldReturnADbContext()
        {
            var enumerator = _checkerToTest.GetDbContextTypes(_dllPath).GetEnumerator();

            DbContext resultContext;

            var result = _checkerToTest.GetNextDbContext(enumerator, out resultContext);
            Assert.True(result);
            Assert.NotNull(resultContext);

        }

        [Test]
        public void GivenADbContext_RunDbContextTests()
        {
            var enumerator = _checkerToTest.GetDbContextTypes(_dllPath).GetEnumerator();
            var xclusionList = new string[] {
                "OfferPayments",
                "BuyerAdminAllowances"
            };
            DbContext resultContext;
            _checkerToTest.SetupExclusionList(xclusionList);
            _checkerToTest.GetNextDbContext(enumerator, out resultContext);
            var result = _checkerToTest.GetNextDbContext(enumerator, out resultContext);
            _checkerToTest.TestDbContext(resultContext);
        }

        [Test]
        public void GivenADbContextANdASpecificDbContextName_RunDbContextTests()
        {
            var enumerator = _checkerToTest.GetDbContextTypes(_dllPath).GetEnumerator();
            var xclusionList = new string[] {
                "OfferPayments",
                "BuyerAdminAllowances"
            };
            _checkerToTest.SetupExclusionList(xclusionList);
            
            _checkerToTest.TestSpecificDbContext(_dllPath, "ApiDatabaseContext");            
        }
    }
}
