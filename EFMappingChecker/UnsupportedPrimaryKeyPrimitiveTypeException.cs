using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Runtime.Serialization;

namespace EFMappingChecker
{
    [Serializable]
    internal class UnsupportedPrimaryKeyPrimitiveTypeException : Exception
    {
        List<UnsupportedPrimaryKeyPrimitiveType> errors;


        public UnsupportedPrimaryKeyPrimitiveTypeException(List<UnsupportedPrimaryKeyPrimitiveType> errors)
        {
            this.errors = errors;
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var item in errors)
            {
                sb.AppendLine(item.ToString());
            }
            return sb.ToString();
        }
    }

    internal class UnsupportedPrimaryKeyPrimitiveType
    {
        private PrimitiveTypeKind primitiveType;
        private string keyName;

        public UnsupportedPrimaryKeyPrimitiveType(string key, PrimitiveTypeKind type)
        {
            keyName = key;
            primitiveType = type;
        }

        public override string ToString()
        {
            return string.Format("Unable to initialise a value for {0} of type {1}", keyName, primitiveType);
        }
    }
}