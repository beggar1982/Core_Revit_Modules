/* Плагины из файла конфигурации читаю в том виде, в каком они там сохранены
 * А вот получение локализованных значений (имя, описание, полное описание)
 * происходит при построении ленты */

namespace ModPlus_Revit.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ModPlusAPI.Abstractions;

    /// <summary>
    /// Вспомогательные методы загрузки плагинов
    /// </summary>
    internal static class LoadPluginsUtils
    {
        /// <summary>
        /// Список загруженных файлов в виде специального класса для последующего использования при построения ленты и меню
        /// </summary>
        public static List<LoadedPlugin> LoadedPlugins = new List<LoadedPlugin>();

        /// <summary>
        /// Загрузка данных о плагине из интерфейса
        /// </summary>
        /// <param name="loadedFuncAssembly">Загружаемая сборка</param>
        /// <param name="fileName">Имя файла</param>
        public static void LoadDataFromPluginInterface(Assembly loadedFuncAssembly, string fileName)
        {
            var types = GetLoadableTypes(loadedFuncAssembly);
            foreach (var type in types)
            {
                var functionInterface = type.GetInterface(nameof(IModPlusPlugin));
                if (functionInterface != null)
                {
                    if (Activator.CreateInstance(type) is IModPlusPlugin function)
                    {
                        var assemblyFullName = loadedFuncAssembly.GetName().FullName;
                        var lf = new LoadedPlugin
                        {
                            Name = function.Name,
                            LName = function.LName,
                            Description = function.Description,
                            CanAddToRibbon = function.CanAddToRibbon,
                            FullClassName = function.FullClassName,
                            AppFullClassName = function.AppFullClassName,
                            AddInId = function.AddInId,
                            SmallIconUrl =
                                $"pack://application:,,,/{assemblyFullName};component/Resources/{function.Name}_16x16.png",
                            BigIconUrl =
                                $"pack://application:,,,/{assemblyFullName};component/Resources/{function.Name}_32x32.png",
                            AvailProductExternalVersion = VersionData.CurrentRevitVersion,
                            FullDescription = function.FullDescription,
                            ToolTipHelpImage = !string.IsNullOrEmpty(function.ToolTipHelpImage)
                                ? $"pack://application:,,,/{assemblyFullName};component/Resources/Help/{function.ToolTipHelpImage}"
                                : string.Empty,
                            SubPluginsNames = function.SubPluginsNames,
                            SubPluginsLNames = function.SubPluginsLNames,
                            SubDescriptions = function.SubDescriptions,
                            SubFullDescriptions = function.SubFullDescriptions,
                            SubBigIconsUrl = new List<string>(),
                            SubSmallIconsUrl = new List<string>(),
                            SubHelpImages = new List<string>(),
                            SubClassNames = function.SubClassNames,
                            Location = fileName,
                            Assembly = loadedFuncAssembly
                        };
                        if (function.SubPluginsNames != null)
                        {
                            foreach (var subFunctionsName in function.SubPluginsNames)
                            {
                                lf.SubSmallIconsUrl.Add(
                                    $"pack://application:,,,/{assemblyFullName};component/Resources/{subFunctionsName}_16x16.png");
                                lf.SubBigIconsUrl.Add(
                                    $"pack://application:,,,/{assemblyFullName};component/Resources/{subFunctionsName}_32x32.png");
                            }
                        }

                        if (function.SubHelpImages != null)
                        {
                            foreach (var helpImage in function.SubHelpImages)
                            {
                                lf.SubHelpImages.Add(
                                    !string.IsNullOrEmpty(helpImage)
                                    ? $"pack://application:,,,/{assemblyFullName};component/Resources/Help/{helpImage}"
                                    : string.Empty);
                            }
                        }

                        LoadedPlugins.Add(lf);
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
                    if (fileInfo.Name.Equals($"{pluginName}_{VersionData.CurrentRevitVersion}.dll"))
                    {
                        fileName = file;
                        break;
                    }
                }
            }

            return fileName;
        }
    }
}
