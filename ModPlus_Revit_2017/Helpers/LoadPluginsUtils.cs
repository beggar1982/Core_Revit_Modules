/* Функция из файла конфигурации читаю в том виде, в каком они там сохранены
 * А вот получение локализованных значений (имя, описание, полное описание)
 * происходит при построении ленты */

namespace ModPlus_Revit.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ModPlusAPI.Interfaces;

    /// <summary>
    /// Вспомогательные методы загрузки плагинов
    /// </summary>
    public static class LoadPluginsUtils
    {
        /// <summary>
        /// Список загруженных файлов в виде специального класса для последующего использования при построения ленты и меню
        /// </summary>
        public static List<LoadedFunction> LoadedFunctions = new List<LoadedFunction>();

        public static void GetDataFromFunctionInterface(Assembly loadedFuncAssembly, string fileName)
        {
            var types = GetLoadableTypes(loadedFuncAssembly);
            foreach (var type in types)
            {
                var interf = type.GetInterface(typeof(IModPlusFunctionInterface).Name);
                if (interf != null)
                {
                    if (Activator.CreateInstance(type) is IModPlusFunctionInterface function)
                    {
                        var lf = new LoadedFunction
                        {
                            Name = function.Name,
                            LName = function.LName,
                            Description = function.Description,
                            CanAddToRibbon = function.CanAddToRibbon,
                            ClassName = function.FullClassName,
                            SmallIconUrl = "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                           ";component/Resources/" + function.Name +
                                           "_16x16.png",
                            BigIconUrl = "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                         ";component/Resources/" + function.Name +
                                         "_32x32.png",
                            AvailProductExternalVersion = MpVersionData.CurrentRevitVersion,
                            FullDescription = function.FullDescription,
                            ToolTipHelpImage = !string.IsNullOrEmpty(function.ToolTipHelpImage)
                            ? "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName + ";component/Resources/Help/" + function.ToolTipHelpImage
                            : string.Empty,
                            SubFunctionsNames = function.SubFunctionsNames,
                            SubFunctionsLNames = function.SubFunctionsLames,
                            SubDescriptions = function.SubDescriptions,
                            SubFullDescriptions = function.SubFullDescriptions,
                            SubBigIconsUrl = new List<string>(),
                            SubSmallIconsUrl = new List<string>(),
                            SubHelpImages = new List<string>(),
                            SubClassNames = function.SubClassNames,
                            Location = fileName
                        };
                        if (function.SubFunctionsNames != null)
                        {
                            foreach (var subFunctionsName in function.SubFunctionsNames)
                            {
                                lf.SubSmallIconsUrl.Add("pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                                        ";component/Resources/" + subFunctionsName +
                                                        "_16x16.png");
                                lf.SubBigIconsUrl.Add("pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                                      ";component/Resources/" + subFunctionsName +
                                                      "_32x32.png");
                            }
                        }

                        if (function.SubHelpImages != null)
                        {
                            foreach (var helpImage in function.SubHelpImages)
                            {
                                lf.SubHelpImages.Add(
                                    !string.IsNullOrEmpty(helpImage)
                                    ? "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName + ";component/Resources/Help/" + helpImage
                                    : string.Empty
                                );
                            }
                        }

                        LoadedFunctions.Add(lf);
                    }

                    break;
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Поиск файла функции, если в файле конфигурации вдруг нет атрибута
        /// </summary>
        /// <param name="pluginName">Plugin uniq name</param>
        /// <returns></returns>
        public static string FindFile(string pluginName)
        {
            var fileName = string.Empty;
            var funcDir = Path.Combine(ModPlusAPI.Constants.CurrentDirectory, "Functions", "Revit", pluginName);
            if (Directory.Exists(funcDir))
            {
                foreach (var file in Directory.GetFiles(funcDir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Name.Equals(pluginName + "_" + MpVersionData.CurrentRevitVersion + ".dll"))
                    {
                        fileName = file;
                        break;
                    }
                }
            }

            return fileName;
        }
    }

    public class LoadedFunction
    {
        public string Location { get; set; }
        public string Name { get; set; }
        public string LName { get; set; }
        public string AvailProductExternalVersion { get; set; }
        public string ClassName { get; set; }
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
    }
}
