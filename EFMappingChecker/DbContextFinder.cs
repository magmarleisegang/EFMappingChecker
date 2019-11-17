using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EFMappingChecker
{
    public class DbContextFinder
    {
        private IEnumerable<Type> _dbContextTypes;
        private IEnumerator<Type> _dbContextTypeEnumerator;

        public DbContext GetSpecificDbContext(string dbContextName, string connectionString)
        {
            var dbContextType = _dbContextTypes.FirstOrDefault(t => t.Name.Equals(dbContextName));
            if (dbContextType != null)
            {
                var constructor = dbContextType.GetConstructor(new[] { typeof(string) });
                return (DbContext)constructor.Invoke(new object[] { connectionString });
            }
            throw new Exception(string.Format("Failed to get specific DbContext: {0}", dbContextName));
        }

        public bool GetNextDbContext(string connectionString, out DbContext nextDbContext)
        {
            if (_dbContextTypeEnumerator == null)
                _dbContextTypeEnumerator = _dbContextTypes.GetEnumerator();

            if (_dbContextTypeEnumerator.MoveNext())
            {
                var currentType = _dbContextTypeEnumerator.Current;
                var constructor = currentType.GetConstructor(new[] { typeof(string) });
                nextDbContext = (DbContext)constructor.Invoke(new object[] { connectionString });
                return true;
            }
            else
            {
                nextDbContext = null;
                return false;
            }
        }

        public int DbContextCount()
        {
            if (_dbContextTypes == null)
                return 0;
            else
                return _dbContextTypes.Count();
        }

        public void LoadDbContextTypes(string dllFile)
        {
            if (File.Exists(dllFile))
            {
                Assembly asm = Assembly.LoadFrom(dllFile);
                if (asm.ExportedTypes.Any(type => type.IsSubclassOf(typeof(DbContext))))
                {
                    _dbContextTypes = asm.ExportedTypes.Where(type => type.IsSubclassOf(typeof(DbContext)));
                    return;
                }
                else
                {
                    throw new DbContextsNotFoundException(asm.FullName);
                }
            }
            throw new FileNotFoundException("Dll file not found", dllFile);
        }

    }
}
