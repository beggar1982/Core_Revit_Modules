#pragma warning disable SA1600 // Elements should be documented
namespace ModPlus_Revit.Helpers
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Загруженный плагин. Свойства класса соответствуют свойствам интерфейса <see cref="ModPlusAPI.Interfaces.IModPlusFunctionInterface"/>
    /// </summary>
    public class LoadedFunction
    {
        /// <summary>
        /// Расположение файла плагина
        /// </summary>
        public string Location { get; set; }

        public string Name { get; set; }

        public string LName { get; set; }

        public string AvailProductExternalVersion { get; set; }

        public string ClassName { get; set; }

        public string AppFullClassName { get; set; }

        public string SmallIconUrl { get; set; }

        public string BigIconUrl { get; set; }

        public string Description { get; set; }

        public bool CanAddToRibbon { get; set; }

        public string FullDescription { get; set; }

        public string ToolTipHelpImage { get; set; }

        public List<string> SubFunctionsNames { get; set; }

        public List<string> SubFunctionsLNames { get; set; }

        public List<string> SubDescriptions { get; set; }

        public List<string> SubFullDescriptions { get; set; }

        public List<string> SubHelpImages { get; set; }

        public List<string> SubSmallIconsUrl { get; set; }

        public List<string> SubBigIconsUrl { get; set; }

        public List<string> SubClassNames { get; set; }

        public Assembly Assembly { get; set; }
    }
}
#pragma warning restore SA1600 // Elements should be documented