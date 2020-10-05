namespace ModPlus_Revit.Utils
{
    using System;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.ExtensibleStorage;

    /// <summary>
    /// Расширения для работы с ExtensibleStorage
    /// </summary>
    public static class ExtensibleStorageExtensions
    {
        /// <summary>
        /// Записывает строку value в поле parameterName в ExtensibleStorage элемента.  Вызывать необходимо внутри транзакции
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        public static void SetExtParameter(this Element element, string parameterName, string value)
        {
            var schema = GetSchema(parameterName);
            var entity = new Entity(schema);
            var field = schema.GetField(parameterName);
            entity.Set(field, value);
            element.SetEntity(entity);
        }

        /// <summary>
        /// Удалить данные из ExtensibleStorage элемента
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static void DeleteExtParameter(this Element element, string parameterName)
        {
            var schema = GetSchema(parameterName);
            if (schema != null)
                element.DeleteEntity(schema);
        }

        /// <summary>
        /// Возвращает строковое значение из поля parameterName в ExtensibleStorage элемента
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static string GetExtParameter(this Element element, string parameterName)
        {
            try
            {
                Schema schema = GetSchema(parameterName);
                Entity entity = element.GetEntity(schema);
                if (!entity.IsValid())
                    return string.Empty;
                Field f = schema.GetField(parameterName);
                return entity.Get<string>(f);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Schema GetSchema(string parameterName)
        {
            var schemas = Schema.ListSchemas();
            var schema = schemas.FirstOrDefault(x => x.ReadAccessLevel == AccessLevel.Public &&
                                                     x.ListFields().Count == 1 &&
                                                     x.ListFields()[0].FieldName == parameterName);
            if (schema != null) 
                return schema;
            return CreateSchema(parameterName);
        }

        private static Schema CreateSchema(string parameterName)
        {
            SchemaBuilder builder = new SchemaBuilder(Guid.NewGuid());
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(parameterName, typeof(string));
            builder.SetSchemaName("ExtExtension");
            return builder.Finish();
        }
    }
}
