namespace ModPlus_Revit.Utils
{
    using System;
    using Autodesk.Revit.DB;
    using JetBrains.Annotations;

    /// <summary>
    /// Утилиты для работы с размерами
    /// </summary>
    [PublicAPI]
    public static class Dimensions
    {
        /// <summary>
        /// Удаление нулей из размерной цепочки. В случае, если нулей не найдено, возвращает False. Иначе - True и
        /// массив <see cref="Reference"/> для пересоздания размерной цепочки без нулей
        /// </summary>
        /// <param name="dimension">Проверяемая размерная цепочка</param>
        /// <param name="referenceArray">Массив <see cref="Reference"/> для пересоздания размерной цепочки</param>
        /// <returns>True - размерная цепочка имела нули и требуется пересоздать её. Иначе false</returns>
        public static bool TryRemoveZeroes(Dimension dimension, out ReferenceArray referenceArray)
        {
            referenceArray = new ReferenceArray();
            
            if (dimension.Segments.IsEmpty)
                return false;

            var doc = dimension.Document;

            var reCreate = false;
            for (var i = 0; i < dimension.NumberOfSegments; i++)
            {
                var segment = dimension.Segments.get_Item(i);
                var value = segment.Value;
                if (value.HasValue && Math.Abs(value.Value) < 0.0001)
                {
                    reCreate = true;
                }
                else
                {
                    if (i == 0)
                        referenceArray.Append(dimension.References.get_Item(i).FixReference(doc));
                    referenceArray.Append(dimension.References.get_Item(i + 1).FixReference(doc));
                }
            }

            return reCreate;
        }

        private static Reference FixReference(this Reference reference, Document doc)
        {
            if (reference.ElementReferenceType != ElementReferenceType.REFERENCE_TYPE_LINEAR &&
                doc.GetElement(reference.ElementId) is Grid grid)
            {
                foreach (var geometryObject in grid.get_Geometry(new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true,
                    View = doc.ActiveView
                }))
                {
                    if (geometryObject is Line line && line.Reference != null)
                    {
                        return line.Reference;
                    }
                }
            }

            return reference;
        }
    }
}
