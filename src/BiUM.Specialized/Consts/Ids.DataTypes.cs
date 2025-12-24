using BiUM.Core.Common.Utils;
using System;

namespace BiUM.Specialized.Consts;

public partial class Ids
{
    public static class DataType
    {
        public static Guid Guid = GuidGenerator.NewGuid("DataType-Guid");
        public static Guid String = GuidGenerator.NewGuid("DataType-String");
        public static Guid Integer = GuidGenerator.NewGuid("DataType-Integer");
        public static Guid Decimal = GuidGenerator.NewGuid("DataType-Decimal");
        public static Guid Boolean = GuidGenerator.NewGuid("DataType-Boolean");
        public static Guid DateTime = GuidGenerator.NewGuid("DataType-DateTime");
        public static Guid DateOnly = GuidGenerator.NewGuid("DataType-DateOnly");
        public static Guid TimeOnly = GuidGenerator.NewGuid("DataType-TimeOnly");
        public static Guid Object = GuidGenerator.NewGuid("DataType-Object");
    }
}