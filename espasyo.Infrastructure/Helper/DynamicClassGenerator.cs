using espasyo.Application.Common.Models.ML;
using System.Reflection.Emit;
using System.Reflection;

namespace espasyo.Infrastructure.Helper
{
    public static class DynamicClassGenerator
    {
        public static object? CreateDynamicClass(string[] fieldNames, TrainerModel source)
        {
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeBuilder = moduleBuilder.DefineType("DynamicClass", TypeAttributes.Public);

            foreach (var fieldName in fieldNames)
            {
                var sourceProperty = source.GetType().GetProperty(fieldName);
                if (sourceProperty != null)
                {
                    typeBuilder.DefineProperty(fieldName, PropertyAttributes.HasDefault, sourceProperty.PropertyType, null);
                }
            }

            var dynamicType = typeBuilder.CreateType();
            if (dynamicType == null)
            {
                return null;
            }

            var dynamicObject = Activator.CreateInstance(dynamicType);
            if (dynamicObject == null)
            {
                return null;
            }

            foreach (var fieldName in fieldNames)
            {
                var property = dynamicType.GetProperty(fieldName);
                if (property != null)
                {
                    var value = source.GetType().GetProperty(fieldName)?.GetValue(source, null);
                    property.SetValue(dynamicObject, value);
                }
            }

            return dynamicObject;
        }
    }
}
