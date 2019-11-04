using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Runtime.Serialization;

namespace EFMappingChecker
{
    [Serializable]
    internal class UnsupportedPrimaryKeyPrimitiveTypeException : Exception
    {
        private PrimitiveTypeKind primitiveTypeKind;

        public UnsupportedPrimaryKeyPrimitiveTypeException(PrimitiveTypeKind primitiveTypeKind)
        {
            this.primitiveTypeKind = primitiveTypeKind;
        }

        public override string ToString()
        {
            return "Unable to initialise a value for primitive type: " + primitiveTypeKind.ToString();
        }
    }
}