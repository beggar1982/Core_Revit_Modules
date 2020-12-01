namespace ModPlus_Revit.Models.Parameters
{
    using System;
    using Autodesk.Revit.DB;

    /// <summary>
    /// Parameter value holder
    /// </summary>
    public class ParameterHolder
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="parameter">Revit parameter</param>
        public ParameterHolder(Parameter parameter)
        {
            Name = parameter.Definition.Name;
            StorageType = parameter.StorageType;
            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    DoubleValue = parameter.AsDouble();
                    break;
                case StorageType.Integer:
                    IntValue = parameter.AsInteger();
                    break;
                case StorageType.String:
                    StringValue = parameter.AsString() ?? string.Empty;
                    break;
                case StorageType.ElementId:
                    ElementIdValue = parameter.AsElementId().IntegerValue;
                    break;
            }
        }

        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Storage type
        /// </summary>
        public StorageType StorageType { get; }

        /// <summary>
        /// Double value
        /// </summary>
        public double DoubleValue { get; }

        /// <summary>
        /// String value
        /// </summary>
        public string StringValue { get; }

        /// <summary>
        /// Integer value
        /// </summary>
        public int IntValue { get; }

        /// <summary>
        /// ElementId IntegerValue
        /// </summary>
        public int ElementIdValue { get; }

        /// <summary>
        /// Совпадает ли значение текущего параметра с проверяемым
        /// </summary>
        /// <param name="other">Проверяемый параметр</param>
        public bool MatchValue(ParameterHolder other)
        {
            if (StorageType != other.StorageType)
                return false;

            switch (StorageType)
            {
                case StorageType.Integer:
                    return IntValue == other.IntValue;
                case StorageType.Double:
                    return Math.Abs(DoubleValue - other.DoubleValue) < 0.0001;
                case StorageType.ElementId:
                    return ElementIdValue == other.ElementIdValue;
                case StorageType.String:
                    return StringValue.Equals(other.StringValue, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }
    }
}