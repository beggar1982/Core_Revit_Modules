using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Autodesk.Revit.UI;
using ModPlusAPI;
using ModPlusAPI.Windows;
using ModPlus_Revit.Helpers;

namespace ModPlus_Revit
{
    public class ModPlus : IExternalApplication
    {
        //public static List<FunctionForCUI> modplusFunctionsForCui;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Принудительная загрузка сборок
                LoadAssms();
                UserConfigFile.InitConfigFile();
                LoadFunctions();
                // Load ribbon
                App.RibbonBuilder.CreateRibbon(application);
                // проверка загруженности модуля автообновления
                CheckAutoUpdaterLoaded();
                

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                // Тут отображение ошибки должно быть в обычном окне, т.к. сборки могли еще не загрузилится
                TaskDialog.Show("ModPlus", exception.Message + Environment.NewLine + exception.StackTrace,
                    TaskDialogCommonButtons.Ok);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        // проверка соответсвия версии автокада
        //private static bool CheckCadVersion()
        //{
        //    var cadVer = AcApp.Version;
        //    return (cadVer.Major + "." + cadVer.Minor).Equals(MpVersionData.CurCadInternalVersion);
        //}
        // Принудительная загрузка сборок
        // необходимых для работы
        private static void LoadAssms()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus");
                using (key)
                {
                    if (key != null)
                    {
                        var assemblies = key.GetValue("Dll").ToString().Split('/').ToList();

                        foreach (var file in Directory.GetFiles(key.GetValue("TopDir").ToString(), "*.dll", SearchOption.AllDirectories))
                        {
                            if (assemblies.Contains((new FileInfo(file)).Name))
                            {
                                Assembly.LoadFrom(file);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // Тут отображение ошибки должно быть в обычном окне, т.к. сборки еще не загрузились
                TaskDialog.Show("ModPlus", exception.Message + Environment.NewLine + exception.StackTrace,
                    TaskDialogCommonButtons.Ok);
            }
        }
        // Загрузка функций
        private static void LoadFunctions()
        {
            try
            {
                // Расположение файла конфигурации
                var confF = UserConfigFile.FullFileName;
                // Грузим
                var configFile = XElement.Load(confF);
                // Делаем итерацию по значениям в файле конфигурации
                var xElement = configFile.Element("Config");
                var el = xElement?.Element("Functions");
                if (el != null)
                {
                    //modplusFunctionsForCui = new List<FunctionForCUI>();

                    foreach (var conFunc in el.Elements("function"))
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
                            var availProductExternalVersionAttr = conFunc.Attribute("AvailProductExternalVersion");
                            if (availProductExternalVersionAttr != null)
                                conFuncAvailCad = availProductExternalVersionAttr.Value;
                            if (!string.IsNullOrEmpty(conFuncAvailCad))
                            {
                                // Проверяем по версии автокада
                                if (conFuncAvailCad.Equals(MpVersionData.CurRevitVers))
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
                                                var localFuncAssembly = Assembly.LoadFrom(conFuncFileAttr.Value);
                                                LoadFunctionsHelper.GetDataFromFunctionIntrface(localFuncAssembly, conFuncFileAttr.Value);
                                            }
                                        }
                                        else
                                        {
                                            var findedFile = LoadFunctionsHelper.FindFile(confFuncNameAttr.Value);
                                            if (!string.IsNullOrEmpty(findedFile))
                                                if (File.Exists(findedFile))
                                                {
                                                    var localFuncAssembly = Assembly.LoadFrom(findedFile);
                                                    LoadFunctionsHelper.GetDataFromFunctionIntrface(localFuncAssembly, findedFile);
                                                }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
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
        /// Проверка загруженности модуля автообновления
        /// </summary>
        private static void CheckAutoUpdaterLoaded()
        {
            try
            {
                var loadWithWindows = !bool.TryParse(Regestry.GetValue("AutoUpdater", "LoadWithWindows"), out bool b) || b;
                if (loadWithWindows)
                {
                    // Если "грузить с виндой", то проверяем, что модуль запущен
                    // если не запущен - запускаем
                    var isOpen = Process.GetProcesses().Any(t => t.ProcessName == "mpAutoUpdater");
                    if (!isOpen)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var curDir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        if (curDir != null)
                        {
                            var fileToStart = Path.Combine(curDir, "mpAutoUpdater.exe");
                            if (File.Exists(fileToStart))
                            {
                                Process.Start(fileToStart);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Statistic.SendException(exception);
            }
        }
    }
}
