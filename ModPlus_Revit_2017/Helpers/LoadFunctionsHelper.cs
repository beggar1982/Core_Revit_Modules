using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ModPlusAPI.Interfaces;
using ModPlusAPI.Windows;

namespace ModPlus_Revit.Helpers
{
    public static class LoadFunctionsHelper
    {
        /// <summary>
        /// Список загруженных файлов в виде специального класса для последующего использования при построения ленты и меню
        /// </summary>
        public static List<LoadedFunction> LoadedFunctions = new List<LoadedFunction>();

        public static void GetDataFromFunctionIntrface(Assembly loadedFuncAssembly, string fileName)
        {
            var types = GetLoadableTypes(loadedFuncAssembly);
            foreach (var type in types)
            {
                var interf = type.GetInterface(typeof(IModPlusFunctionInterface).Name);
                if (interf != null)
                {
                    var function = Activator.CreateInstance(type) as IModPlusFunctionInterface;
                    if (function != null)
                    {
                        var lf = new LoadedFunction
                        {
                            Name = function.Name,
                            LName = function.LName,
                            Description = function.Description,
                            CanAddToRibbon = function.CanAddToRibbon,
                            ClassName = function.ClassName,
                            SmallIconUrl = "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                           ";component/Resources/" + function.Name +
                                           "_16x16.png",
                            BigIconUrl = "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                         ";component/Resources/" + function.Name +
                                         "_32x32.png",
                            AvailProductExternalVersion = MpVersionData.CurRevitVers,
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
                            foreach (var subFunctionsName in function.SubFunctionsNames)
                            {
                                lf.SubSmallIconsUrl.Add("pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                                        ";component/Resources/" + subFunctionsName +
                                                        "_16x16.png");
                                lf.SubBigIconsUrl.Add("pack://application:,,,/" + loadedFuncAssembly.GetName().FullName +
                                                      ";component/Resources/" + subFunctionsName +
                                                      "_32x32.png");
                            }
                        if (function.SubHelpImages != null)
                            foreach (var helpImage in function.SubHelpImages)
                            {
                                lf.SubHelpImages.Add(
                                    !string.IsNullOrEmpty(helpImage)
                                    ? "pack://application:,,,/" + loadedFuncAssembly.GetName().FullName + ";component/Resources/Help/" + helpImage
                                    : string.Empty
                                );
                            }
                        LoadedFunctions.Add(lf);
                    }
                    break;
                }
            }
        }
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
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
        /// Проверка того, что функция в АПИ присутсвует физически
        /// </summary>
        /// <param name="cuiFunction">Функция в АПИ</param>
        /// <param name="confFunctions">Функции в файле конфигурации</param>
        /// <param name="configFunction">Возвращаем функцию в списке функций из файла конфигурации для последующего извлечения данных</param>
        /// <param name="fileVersion">Версия файла. Это значение должно быть точнее, чем в файле конфигурации</param>
        /// <returns></returns>
        public static bool HasFunction(XElement cuiFunction, XContainer confFunctions, out XElement configFunction, out string fileVersion)
        {
            configFunction = null;
            fileVersion = string.Empty;
            var cuiFuncNameAttr = cuiFunction.Attribute("Name");
            if (cuiFuncNameAttr != null)
            {
                var cuiFunctionName = cuiFuncNameAttr.Value;

                foreach (var conFunc in confFunctions.Elements("function"))
                {
                    /* Так как после обновления добавится значение 
                     * ProductFor, то нужно проверять по нем, при наличии
                     */
                    var productForAttr = conFunc.Attribute("ProductFor");
                    if (productForAttr != null)
                        if (!productForAttr.Value.Equals("Revit"))
                            continue;

                    var confFuncNameAttr = conFunc.Attribute("Name");
                    if (confFuncNameAttr != null)
                    {
                        /* Так как значение AvailCad будет являться устаревшим, НО
                         * пока не будет удалено, делаем двойной вариант проверки
                         */
                        var conFuncAvailCad = string.Empty;
                        var confFuncAvailCadAttr = conFunc.Attribute("AvailCad");
                        if (confFuncAvailCadAttr != null)
                            conFuncAvailCad = confFuncAvailCadAttr.Value;
                        var availProductExternalVersionAttr = conFunc.Attribute("AvailProductExternalVersion");
                        if (availProductExternalVersionAttr != null)
                            conFuncAvailCad = availProductExternalVersionAttr.Value;

                        if (!string.IsNullOrEmpty(conFuncAvailCad))
                        {
                            // Проверяем по названию и версии автокада
                            if (confFuncNameAttr.Value.Equals(cuiFunctionName) &
                                conFuncAvailCad.Equals(MpVersionData.CurRevitVers))
                            {
                                // Добавляем если только функция включена и есть физически на диске!!!
                                var conFuncOnOff = bool.TryParse(conFunc.Attribute("OnOff")?.Value, out bool b) && b; // false
                                var conFuncFileAttr = conFunc.Attribute("File");
                                // Т.к. атрибута File может не быть
                                if (conFuncOnOff)
                                {
                                    if (conFuncFileAttr != null)
                                    {
                                        if (File.Exists(conFuncFileAttr.Value))
                                        {
                                            configFunction = conFunc;
                                            fileVersion =
                                                FileVersionInfo.GetVersionInfo(conFuncFileAttr.Value).FileVersion;
                                            return true;
                                        }
                                    }
                                    var findedFile = FindFile(confFuncNameAttr.Value);
                                    if (!string.IsNullOrEmpty(findedFile))
                                        if (File.Exists(findedFile))
                                        {
                                            configFunction = conFunc;
                                            fileVersion = FileVersionInfo.GetVersionInfo(findedFile).FileVersion;
                                            return true;
                                        }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static string GetBigIconUriString(string functionName, string functionVersion)
        {
            var returnedUriString = string.Empty;
            if (!string.IsNullOrEmpty(functionName))
                returnedUriString = "pack://application:,,,/" + functionName + "_" + MpVersionData.CurRevitVers +
                                    ", Version=" + functionVersion +
                                    ", Culture=neutral, PublicKeyToken=null;component/Resources/" +
                                    functionName + "_32x32.png";
            return returnedUriString;
        }
        public static string GetSmallIconUriString(string functionName, string functionVersion)
        {
            var returnedUriString = string.Empty;
            if (!string.IsNullOrEmpty(functionName))
                returnedUriString = "pack://application:,,,/" + functionName + "_" + MpVersionData.CurRevitVers +
                                    ", Version=" + functionVersion +
                                    ", Culture=neutral, PublicKeyToken=null;component/Resources/" +
                                    functionName + "_16x16.png";
            return returnedUriString;
        }

        /// <summary>
        /// Определение версии функции, путем сравнения
        /// </summary>
        /// <param name="fileVersion">Текстовое значение версии файла. Может быть пустым</param>
        /// <param name="f">XElement функции из файла конфигурации. Скорее всего будет исключен со временем</param>
        /// <returns></returns>
        public static string GetFunctionVersion(string fileVersion, XElement f)
        {
            var versionFromFile = new System.Version(fileVersion);
            var versionFromConfigAttr = f.Attribute("Version");
            string functionVersion;
            if (versionFromConfigAttr != null)
            {
                var versionFromConfig = new System.Version(versionFromConfigAttr.Value);
                if (!string.IsNullOrEmpty(fileVersion))
                    functionVersion = versionFromFile >= versionFromConfig
                        ? fileVersion
                        : versionFromConfigAttr.Value;
                else functionVersion = versionFromConfigAttr.Value;
            }
            else functionVersion = fileVersion;

            return functionVersion;
        }
        /// <summary>
        /// Поиск файла функции, если в файле конфигурации вдруг нет атрибута
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        public static string FindFile(string functionName)
        {
            var fileName = string.Empty;
            var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus");
            using (regKey)
            {
                if (regKey != null)
                {
                    var funcDir = Path.Combine(regKey.GetValue("TopDir").ToString(), "Functions", "Revit", functionName);
                    if (Directory.Exists(funcDir))
                        foreach (var file in Directory.GetFiles(funcDir, "*.dll", SearchOption.TopDirectoryOnly))
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Name.Equals(functionName + "_" + MpVersionData.CurRevitVers + ".dll"))
                            {
                                fileName = file;
                                break;
                            }
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
