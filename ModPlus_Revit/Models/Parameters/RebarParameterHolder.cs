namespace ModPlus_Revit.Models.Parameters
{
    using System;
    using System.Xml.Linq;
    using JetBrains.Annotations;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Объект хранения параметра. Используется совместно с <see cref="RebarParameters"/> и <see cref="View.Controls.EditParametersControl"/>
    /// </summary>
    public class RebarParameterHolder : VmBase, ICloneable
    {
        private string _value;

        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Значение параметра
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// BuiltInParameter for system parameters
        /// </summary>
        public string BuiltInParameter { get; set; }

        /// <summary>
        /// Get instance of <see cref="RebarParameterHolder"/> from <see cref="XElement"/>
        /// </summary>
        /// <param name="xElement"><see cref="XElement"/></param>
        [NotNull]
        public static RebarParameterHolder GetFromXElement(XElement xElement)
        {
            return new RebarParameterHolder
            {
                Name = xElement.Element(nameof(Name))?.Value ?? string.Empty,
                BuiltInParameter = xElement.Element(nameof(BuiltInParameter))?.Value ?? string.Empty,
                Value = xElement.Element(nameof(Value))?.Value ?? string.Empty
            };
        }

        /// <summary>
        /// Get instance of <see cref="RebarParameterHolder"/> from string represent
        /// </summary>
        /// <param name="stringRepresent">String represent</param>
        [NotNull]
        public static RebarParameterHolder GetFromStringRepresent(string stringRepresent)
        {
            var split = stringRepresent.Split('|');
            return new RebarParameterHolder
            {
                Name = split[0],
                BuiltInParameter = split[1],
                Value = split[2]
            };
        }

        /// <summary>
        /// Get string represent of current instance
        /// </summary>
        public string ToStringRepresent()
        {
            return $"{Name}|{BuiltInParameter}|{Value}";
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new RebarParameterHolder
            {
                Name = Name,
                BuiltInParameter = BuiltInParameter,
                Value = Value
            };
        }

        /// <summary>
        /// Сравнивает два экземпляра <see cref="RebarParameterHolder"/> по значению <see cref="BuiltInParameter"/> или <see cref="Name"/>
        /// </summary>
        /// <param name="rebarParameterHolder">Сравниваемый экземпляр <see cref="RebarParameterHolder"/></param>
        public bool IsEqualTo(RebarParameterHolder rebarParameterHolder)
        {
            if (!string.IsNullOrEmpty(BuiltInParameter) &&
                !string.IsNullOrEmpty(rebarParameterHolder.BuiltInParameter))
            {
                if (BuiltInParameter == rebarParameterHolder.BuiltInParameter)
                    return true;
            }

            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(rebarParameterHolder.Name))
            {
                if (Name == rebarParameterHolder.Name)
                    return true;
            }

            return false;
        }
    }
}
