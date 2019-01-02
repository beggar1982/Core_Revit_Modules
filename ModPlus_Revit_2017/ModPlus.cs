using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using ModPlusAPI;
using ModPlusAPI.Windows;
using ModPlus_Revit.Helpers;

namespace ModPlus_Revit
{
    using System.Net;
    using System.Xml.Linq;
    using ModPlusAPI.LicenseServer;

    public class ModPlus : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // inint lang
                if (!Language.Initialize()) return Result.Cancelled;
                // statistic
                Statistic.SendPluginStarting("Revit", MpVersionData.CurRevitVers);
                // Принудительная загрузка сборок
                LoadAssemblies();
                UserConfigFile.InitConfigFile();
                LoadFunctions();
                // check adaptation
                CheckAdaptation();

                // Load ribbon
                App.RibbonBuilder.CreateRibbon(application);
                // проверка загруженности модуля автообновления
                CheckAutoUpdaterLoaded();

                // license server client
                ClientStarter.StartConnection(ProductLicenseType.Revit);

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
            ClientStarter.StopConnection();
            return Result.Succeeded;
        }
        
        // Принудительная загрузка сборок
        // необходимых для работы
        private static void LoadAssemblies()
        {
            try
            {
                foreach (var fileName in Constants.ExtensionsLibraries)
                {
                    var extDll = Path.Combine(Constants.ExtensionsDirectory, fileName);
                    if (File.Exists(extDll))
                    {
                        Assembly.LoadFrom(extDll);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        
        // Загрузка функций
        private static void LoadFunctions()
        {
            try
            {
                var funtionsKey = Registry.CurrentUser.OpenSubKey("ModPlus\\Functions");
                if (funtionsKey == null) return;
                using (funtionsKey)
                {
                    foreach (var functionKeyName in funtionsKey.GetSubKeyNames())
                    {
                        var functionKey = funtionsKey.OpenSubKey(functionKeyName);
                        if (functionKey == null) continue;
                        foreach (var availPrVersKeyName in functionKey.GetSubKeyNames())
                        {
                            // Если версия продукта не совпадает, то пропускаю
                            if (!availPrVersKeyName.Equals(MpVersionData.CurRevitVers)) continue;
                            var availPrVersKey = functionKey.OpenSubKey(availPrVersKeyName);
                            if (availPrVersKey == null) continue;
                            // беру свойства функции из реестра
                            var file = availPrVersKey.GetValue("File") as string;
                            var onOff = availPrVersKey.GetValue("OnOff") as string;
                            var productFor = availPrVersKey.GetValue("ProductFor") as string;
                            if (string.IsNullOrEmpty(onOff) || string.IsNullOrEmpty(productFor)) continue;
                            if (!productFor.Equals("Revit")) continue;
                            var isOn = !bool.TryParse(onOff, out var b) || b; // default - true
                            // Если "Продукт для" подходит, файл существует и функция включена - гружу
                            if (isOn)
                            {
                                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                                {
                                    // load
                                    var localFuncAssembly = Assembly.LoadFrom(file);
                                    LoadFunctionsHelper.GetDataFromFunctionIntrface(localFuncAssembly, file);
                                }
                                else
                                {
                                    var findedFile = LoadFunctionsHelper.FindFile(functionKeyName);
                                    if (!string.IsNullOrEmpty(findedFile) && File.Exists(findedFile))
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
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        
        /// <summary>Проверка загруженности модуля автообновления</summary>
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
                        var fileToStart = Path.Combine(Constants.CurrentDirectory, "mpAutoUpdater.exe");
                        if (File.Exists(fileToStart))
                        {
                            Process.Start(fileToStart);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Statistic.SendException(exception);
            }
        }

        private static void CheckAdaptation()
        {
            var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("Revit");

            // Проходим по группам
            if (confCuiXel == null || confCuiXel.IsEmpty)
            {
                if (ModPlusAPI.Web.Connection.CheckForInternetConnection())
                {
                    // Грузим файл
                    try
                    {
                        var url = "http://www.modplus.org/Downloads/StandardCUIRevit.xml";
                        if (string.IsNullOrEmpty(url))
                            return;
                        string xmlStr;
                        using (var wc = new WebClientWithTimeout { Proxy = ModPlusAPI.Web.Proxy.GetWebProxy() })
                            xmlStr = wc.DownloadString(url);
                        var xmlDocument = XElement.Parse(xmlStr);

                        ModPlusAPI.RegistryData.Adaptation.SaveCuiFromXElement("Revit", xmlDocument);
                    }
                    catch
                    {
                    }
                }
            }
        }

        internal class WebClientWithTimeout : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 3000;
                return w;
            }
        }
    }
}
