using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Autodesk.Revit.UI;
using ModPlusAPI.Windows;
using ModPlus_Revit.Helpers;
using RibbonPanel = Autodesk.Revit.UI.RibbonPanel;


namespace ModPlus_Revit.App
{
    public static class RibbonBuilder
    {
        private static string _tabName = "ModPlus";

        public static void CreateRibbon(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(_tabName);
                // create and fill panels
                AddPanels(application);

                AddHelpPanel(application);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        private static void AddPanels(UIControlledApplication application)
        {
            try
            {
                // Расположение файла конфигурации
                var confF = ModPlusAPI.UserConfigFile.FullFileName;
                // Грузим
                var configFile = XElement.Load(confF);
                // Проверяем есть ли группа Config
                if (configFile.Element("Config") == null)
                {
                    MessageBox.Show("Файл конфигурации поврежден! Невозможно построить ленту", MessageBoxIcon.Close);
                    return;
                }
                var element = configFile.Element("Config");
                // Проверяем есть ли подгруппа Cui
                if (element?.Element("CUIRevit") == null)
                {
                    MessageBox.Show("Файл конфигурации поврежден! Невозможно построить ленту", MessageBoxIcon.Close);
                    return;
                }
                var confCuiXel = element.Element("CUIRevit");
                // Проходим по группам
                if (confCuiXel != null)
                    foreach (var group in confCuiXel.Elements("Group"))
                    {
                        // create the panel
                        RibbonPanel panel = application.CreateRibbonPanel(_tabName, group.Attribute("GroupName")?.Value);

                        // Проходим по функциям группы
                        foreach (var func in group.Elements("Function"))
                        {
                            if (LoadFunctionsHelper.LoadedFunctions.Any(x => x.Name.Equals(func.Attribute("Name")?.Value)))
                            {
                                var loadedFunction = LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(func.Attribute("Name")?.Value));
                                if(loadedFunction == null) continue;
                                // Если функция имеет "подфункции", то делаем SplitButton
                                if (func.Elements("SubFunction").Any())
                                {
                                    SplitButtonData splitButtonData = new SplitButtonData(loadedFunction.Name, loadedFunction.LName);
                                    SplitButton sb = panel.AddItem(splitButtonData) as SplitButton;
                                    // add top function
                                    sb?.AddPushButton(CreatePushButtonData(
                                        loadedFunction.Name, loadedFunction.LName, loadedFunction.Description,
                                        loadedFunction.SmallIconUrl, loadedFunction.BigIconUrl,
                                        loadedFunction.FullDescription,
                                        loadedFunction.ToolTipHelpImage, loadedFunction.Location, loadedFunction.ClassName
                                    ));
                                    // Затем добавляем подфункции
                                    foreach (var subFunc in func.Elements("SubFunction"))
                                    {
                                        var loadedSubFunction = LoadFunctionsHelper.LoadedFunctions.FirstOrDefault( x => x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                        if(loadedSubFunction == null) continue;
                                        sb?.AddPushButton(CreatePushButtonData(
                                            loadedSubFunction.Name, loadedSubFunction.LName, loadedSubFunction.Description,
                                            loadedSubFunction.SmallIconUrl, loadedSubFunction.BigIconUrl,
                                            loadedSubFunction.FullDescription,
                                            loadedSubFunction.ToolTipHelpImage,
                                            loadedSubFunction.Location, loadedSubFunction.ClassName
                                        ));
                                    }
                                }
                                else if (loadedFunction.SubFunctionsNames.Any())
                                {
                                    SplitButtonData splitButtonData = new SplitButtonData(loadedFunction.Name, loadedFunction.LName);
                                    SplitButton sb = panel.AddItem(splitButtonData) as SplitButton;
                                    // add top function
                                    sb?.AddPushButton(CreatePushButtonData(
                                        loadedFunction.Name, loadedFunction.LName, loadedFunction.Description,
                                        loadedFunction.SmallIconUrl, loadedFunction.BigIconUrl,
                                        loadedFunction.FullDescription,
                                        loadedFunction.ToolTipHelpImage, 
                                        loadedFunction.Location, loadedFunction.ClassName
                                    ));
                                    for (int i = 0; i < loadedFunction.SubClassNames.Count; i++)
                                    {
                                        sb?.AddPushButton(CreatePushButtonData(
                                            loadedFunction.SubFunctionsNames[i], loadedFunction.SubFunctionsLNames[i],
                                            loadedFunction.SubDescriptions[i],
                                            loadedFunction.SubSmallIconsUrl[i], loadedFunction.SubBigIconsUrl[i],
                                            loadedFunction.SubFullDescriptions[i],
                                            loadedFunction.SubHelpImages[i], loadedFunction.Location,
                                            loadedFunction.SubClassNames[i]
                                        ));
                                    }
                                }
                                else
                                {
                                    AddPushButton(panel, loadedFunction.Name, loadedFunction.LName, loadedFunction.Description,
                                        loadedFunction.SmallIconUrl, loadedFunction.BigIconUrl,
                                        loadedFunction.FullDescription,
                                        loadedFunction.ToolTipHelpImage, loadedFunction.Location, loadedFunction.ClassName);
                                }
                            }
                        }
                    }
            }
            catch (Exception exception) { ExceptionBox.Show(exception); }
        }
        private static void AddHelpPanel(UIControlledApplication application)
        {
            // create the panel
            RibbonPanel panel = application.CreateRibbonPanel(_tabName, _tabName);
            PushButtonData rid = new PushButtonData(
                "mpSettings",
                "Настройки",
                Assembly.GetExecutingAssembly().Location,
                "ModPlus_Revit.App.MpMainSettingsFunction");
            rid.LargeImage = new BitmapImage(new Uri("pack://application:,,,/Modplus_Revit_" + MpVersionData.CurRevitVers + ";component/Resources/HelpBt.png"));
            panel.AddItem(rid);
        }

        private static void AddPushButton(RibbonPanel panel, string name, string lName, string description, string img16, 
            string img32, string fullDescription, string helpImage, string location, string className)
        {
            var pushButton = panel.AddItem(CreatePushButtonData(name, lName, description, img16, img32, fullDescription, helpImage, location, className)) as PushButton;
        }

        private static PushButtonData CreatePushButtonData(string name, string lName, string description, string img16, 
            string img32, string fullDescription, string helpImage, string location, string className)
        {
            var pshBtn = new PushButtonData(name,ConvertLName(lName), location, className)
            {
                ToolTip = description,
            };
            if (!string.IsNullOrEmpty(fullDescription))
                pshBtn.LongDescription = fullDescription;
            // tool tip
            try
            {
                if (!string.IsNullOrEmpty(helpImage))
                    pshBtn.ToolTipImage = new BitmapImage(new Uri(helpImage, UriKind.RelativeOrAbsolute));
            }
            catch 
            {
                // ignored
            }
            try
            {
                pshBtn.Image = new BitmapImage(new Uri(img16, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // ignored
            }
            try
            {
                pshBtn.LargeImage = new BitmapImage(new Uri(img32, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // ignored
            }
            return pshBtn;
        }

        private static string ConvertLName(string lName)
        {
            if (!lName.Contains(" ")) return lName;
            if (lName.Length <= 8) return lName;
            if (lName.Count(x => x == ' ') == 1)
            {
                return lName.Split(' ')[0] + Environment.NewLine + lName.Split(' ')[1];
            }
            var center = lName.Length * 0.5;
            var nearestDelta = lName.Select((c, i) => new { index = i, value = c }).Where(w => w.value == ' ')
                .OrderBy(x => Math.Abs(x.index - center)).First().index;
            return lName.Substring(0, nearestDelta) + Environment.NewLine + lName.Substring(nearestDelta + 1);
        }
    }
}
