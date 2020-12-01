namespace ModPlus_Revit.Models.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Autodesk.Revit.DB;
    using JetBrains.Annotations;

    /// <summary>
    /// Параметры для создаваемой арматуры
    /// </summary>
    public class RebarParameters : ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RebarParameters"/> class.
        /// </summary>
        public RebarParameters()
        {
            Parameters = new List<RebarParameterHolder>();
        }

        /// <summary>
        /// Параметры
        /// </summary>
        public List<RebarParameterHolder> Parameters { get; set; }

        /// <summary>
        /// Get instance of <see cref="RebarParameters"/> from <see cref="XElement"/>
        /// </summary>
        /// <param name="xElement"><see cref="XElement"/></param>
        [NotNull]
        public static RebarParameters GetFromXElement(XElement xElement)
        {
            var rebarParameters = new RebarParameters();

            var pXel = xElement.Element(nameof(Parameters));
            if (pXel != null)
            {
                foreach (var pHolderXel in pXel.Elements("ParameterHolder"))
                {
                    rebarParameters.Parameters.Add(RebarParameterHolder.GetFromXElement(pHolderXel));
                }
            }

            return rebarParameters;
        }

        /// <summary>
        /// Get instance of <see cref="RebarParameters"/> from string represent
        /// </summary>
        /// <param name="stringRepresent">String represent</param>
        [NotNull]
        public static RebarParameters GetFromStringRepresent(string stringRepresent)
        {
            var split = stringRepresent.Split(':');
            var rebarParameters = new RebarParameters();

            foreach (var s in split)
            {
                rebarParameters.Parameters.Add(RebarParameterHolder.GetFromStringRepresent(s));
            }

            return rebarParameters;
        }

        /// <summary>
        /// Get string represent of current instance
        /// </summary>
        public string ToStringRepresent()
        {
            var parameters = Parameters.Select(parameterHolder => parameterHolder.ToStringRepresent()).ToList();

            return string.Join(":", parameters);
        }

        /// <inheritdoc/>
        public object Clone()
        {
            var rebarParameters = new RebarParameters();
            foreach (var parameterHolder in Parameters)
            {
                rebarParameters.Parameters.Add((RebarParameterHolder)parameterHolder.Clone());
            }

            return rebarParameters;
        }

        /// <summary>
        /// Применить параметры к элементу
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Список параметров, которые не удалось установить</returns>
        public string ApplyToElement(Element element)
        {
            var errors = new List<string>();

            foreach (var parameterHolder in Parameters.Where(p => !string.IsNullOrEmpty(p.Value)))
            {
                try
                {
                    if (!string.IsNullOrEmpty(parameterHolder.BuiltInParameter) &&
                        Enum.TryParse(parameterHolder.Value, out BuiltInParameter builtInParameter))
                    {
                        element.get_Parameter(builtInParameter).Set(parameterHolder.Value);
                    }
                    else
                    {
                        element.LookupParameter(parameterHolder.Name).Set(parameterHolder.Value);
                    }
                }
                catch
                {
                    errors.Add($"{parameterHolder.Name} - {parameterHolder.Value}");
                }
            }

            var resultError = string.Empty;
            if (errors.Any())
                resultError = $"{element.Id.IntegerValue}: {string.Join(", ", errors)}";

            return resultError;
        }
    }
}
