namespace ModPlus_Revit.App
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Helpers;

    public static class RibbonBuilder
    {
        private static string _tabName = "ModPlus";
        private static string _langItem = "RevitDlls";
        
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
        
        public static string GetHelpUrl(string functionName)
        {
            var lang = Language.RusWebLanguages.Contains(Language.CurrentLanguageName) ? "ru" : "en";
            return $"https://modplus.org/{lang}/help/{functionName.ToLower()}";
        }

        private static void AddPanels(UIControlledApplication application)
        {
            try
            {
                var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("Revit");
                // Проходим по группам
                if (confCuiXel != null)
                    foreach (var group in confCuiXel.Elements("Group"))
                    {
                        var groupNameAttr = group.Attribute("GroupName");
                        if (groupNameAttr == null) continue;
                        // create the panel
                        RibbonPanel panel = application.CreateRibbonPanel(
                            _tabName,
                            Language.TryGetCuiLocalGroupName(groupNameAttr.Value));

                        // Проходим по функциям группы
                        foreach (var item in group.Elements())
                        {
                            if (item.Name == "Function")
                            {
                                var func = item;
                                if (LoadFunctionsHelper.LoadedFunctions.Any(
                                    x => x.Name.Equals(func.Attribute("Name")?.Value)))
                                {
                                    var loadedFunction =
                                        LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x =>
                                            x.Name.Equals(func.Attribute("Name")?.Value));
                                    if (loadedFunction == null)
                                        continue;

                                    // Если функция имеет "подфункции", то делаем SplitButton
                                    if (func.Elements("SubFunction").Any())
                                    {
                                        SplitButtonData splitButtonData = new SplitButtonData(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName)
                                        );
                                        // add top function
                                        var firstButton = CreatePushButtonData(loadedFunction);
                                        var help = firstButton.GetContextualHelp();
                                        splitButtonData.SetContextualHelp(help);
                                        SplitButton sb = (SplitButton)panel.AddItem(splitButtonData);
                                        sb.AddPushButton(firstButton);
                                        sb.SetContextualHelp(help);

                                        // Затем добавляем подфункции
                                        foreach (var subFunc in func.Elements("SubFunction"))
                                        {
                                            var loadedSubFunction =
                                                LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x =>
                                                    x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                            if (loadedSubFunction == null)
                                                continue;
                                            sb.AddPushButton(CreatePushButtonData(
                                                loadedSubFunction.Name,
                                                Language.GetFunctionLocalName(loadedFunction.Name,
                                                    loadedFunction.LName),
                                                Language.GetFunctionShortDescrition(loadedFunction.Name,
                                                    loadedFunction.Description),
                                                loadedSubFunction.SmallIconUrl,
                                                loadedSubFunction.BigIconUrl,
                                                Language.GetFunctionFullDescription(loadedFunction.Name,
                                                    loadedFunction.FullDescription),
                                                loadedSubFunction.ToolTipHelpImage,
                                                loadedSubFunction.Location, loadedSubFunction.ClassName,
                                                GetHelpUrl(loadedFunction.Name)
                                            ));
                                        }
                                    }
                                    else if (loadedFunction.SubFunctionsNames.Any())
                                    {
                                        SplitButtonData splitButtonData = new SplitButtonData(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName)
                                        );
                                        // add top function
                                        var firstButton = CreatePushButtonData(loadedFunction);
                                        var help = firstButton.GetContextualHelp();
                                        splitButtonData.SetContextualHelp(help);
                                        SplitButton sb = (SplitButton)panel.AddItem(splitButtonData);
                                        sb.AddPushButton(firstButton);
                                        sb.SetContextualHelp(help);

                                        // internal sub functions
                                        for (int i = 0; i < loadedFunction.SubClassNames.Count; i++)
                                        {
                                            sb.AddPushButton(CreatePushButtonData(loadedFunction, i));
                                        }
                                    }
                                    else
                                    {
                                        AddPushButton(panel, loadedFunction);
                                    }
                                }
                            }
                            else if (item.Name == "StackedPanel")
                            {
                                List<RibbonItemData> stackedItems = new List<RibbonItemData>();

                                foreach (XElement func in item.Elements("Function"))
                                {
                                    var loadedFunction =
                                        LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x =>
                                            x.Name.Equals(func.Attribute("Name")?.Value));
                                    if (loadedFunction == null) continue;

                                    stackedItems.Add(CreatePushButtonData(loadedFunction));
                                }

                                if (stackedItems.Count == 2)
                                    panel.AddStackedItems(stackedItems[0], stackedItems[1]);
                                if (stackedItems.Count == 3)
                                    panel.AddStackedItems(stackedItems[0], stackedItems[1], stackedItems[2]);
                            }
                        }
                    }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        
        private static void AddHelpPanel(UIControlledApplication application)
        {
            // create the panel
            RibbonPanel panel = application.CreateRibbonPanel(_tabName, _tabName);

            // user info
            PushButtonData userInfoButton = new PushButtonData(
                "mpUserInfo",
                ConvertLName(Language.GetItem(_langItem, "h13")),
                Assembly.GetExecutingAssembly().Location,
                "ModPlus_Revit.App.UserInfoCommand");
            userInfoButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/Modplus_Revit_" + MpVersionData.CurRevitVers + ";component/Resources/UserInfo_32x32.png"));
            panel.AddItem(userInfoButton);

            // settings
            PushButtonData settingsButton = new PushButtonData(
                "mpSettings",
                Language.GetItem(_langItem, "h12"),
                Assembly.GetExecutingAssembly().Location,
                "ModPlus_Revit.App.MpMainSettingsFunction");
            settingsButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/Modplus_Revit_" + MpVersionData.CurRevitVers + ";component/Resources/HelpBt.png"));
            panel.AddItem(settingsButton);
        }


        private static void AddPushButton(RibbonPanel panel, LoadedFunction loadedFunction)
        {
            var pushButton = panel.AddItem(CreatePushButtonData(loadedFunction)) as PushButton;
        }

        private static PushButtonData CreatePushButtonData(LoadedFunction loadedFunction)
        {
            return CreatePushButtonData(
                loadedFunction.Name,
                Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                Language.GetFunctionShortDescrition(loadedFunction.Name,
                    loadedFunction.Description),
                loadedFunction.SmallIconUrl,
                loadedFunction.BigIconUrl,
                Language.GetFunctionFullDescription(loadedFunction.Name,
                    loadedFunction.FullDescription),
                loadedFunction.ToolTipHelpImage, loadedFunction.Location,
                loadedFunction.ClassName,
                GetHelpUrl(loadedFunction.Name)
            );
        }

        private static PushButtonData CreatePushButtonData(LoadedFunction loadedFunction, int i)
        {
            return CreatePushButtonData(
                    loadedFunction.SubFunctionsNames[i],
                    Language.GetFunctionLocalName(loadedFunction.Name,
                        loadedFunction.SubFunctionsLNames[i], i + 1),
                    Language.GetFunctionShortDescrition(loadedFunction.Name,
                        loadedFunction.SubDescriptions[i], i + 1),
                    loadedFunction.SubSmallIconsUrl[i], loadedFunction.SubBigIconsUrl[i],
                    Language.GetFunctionFullDescription(loadedFunction.Name,
                        loadedFunction.SubFullDescriptions[i], i + 1),
                    loadedFunction.SubHelpImages[i], loadedFunction.Location,
                    loadedFunction.SubClassNames[i],
                    GetHelpUrl(loadedFunction.Name)
                );
        }

        private static PushButtonData CreatePushButtonData(
            string name,
            string lName,
            string description,
            string img16,
            string img32,
            string fullDescription, 
            string helpImage,
            string location, 
            string className,
            string helpUrl)
        {
            var pshBtn = new PushButtonData(name, ConvertLName(lName), location, className)
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

            // help
            if (!string.IsNullOrEmpty(helpUrl))
                pshBtn.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, helpUrl));
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
