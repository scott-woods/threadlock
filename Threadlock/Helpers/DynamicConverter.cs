using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public static class DynamicConverter
    {
        public static object ConvertValue(object value, Type fieldType)
        {
            if (value == null)
                return null;

            if (fieldType.IsAssignableFrom(value.GetType()))
                return value;

            if (fieldType.IsEnum)
                return Enum.Parse(fieldType, value.ToString());

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = fieldType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                foreach (var item in (IEnumerable)value)
                    list.Add(ConvertValue(item, elementType));
                return list;
            }

            if (IsNumericType(fieldType))
                return Convert.ChangeType(value, fieldType);

            return value;
        }

        static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
