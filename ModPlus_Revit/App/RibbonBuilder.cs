namespace ModPlus_Revit.App
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;
    using Autodesk.Revit.UI;
    using Autodesk.Windows;
    using Helpers;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using RibbonItem = Autodesk.Revit.UI.RibbonItem;
    using RibbonPanel = Autodesk.Revit.UI.RibbonPanel;

    public static class RibbonBuilder
    {
        private const string TabName = "ModPlus";
        private const string LangItem = "RevitDlls";

        public static void CreateRibbon(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);

                // create and fill panels
                AddPanels(application);

                AddHelpPanel(application);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Возвращает url справки для плагина
        /// </summary>
        /// <param name="functionName">Имя плагина</param>
        public static string GetHelpUrl(string functionName)
        {
            var lang = Language.RusWebLanguages.Contains(Language.CurrentLanguageName) ? "ru" : "en";
            return $"https://modplus.org/{lang}/revitplugins/{functionName.ToLower()}";
        }

        /// <summary>
        /// Возвращает url справки для плагина
        /// </summary>
        /// <param name="functionName">Имя плагина</param>
        /// <param name="section">Раздел</param>
        public static string GetHelpUrl(string functionName, string section)
        {
            var lang = Language.RusWebLanguages.Contains(Language.CurrentLanguageName) ? "ru" : "en";
            return $"https://modplus.org/{lang}/{section}/{functionName.ToLower()}";
        }

        /// <summary>
        /// Возвращает url справки для всех плагинов
        /// </summary>
        public static string GetHelpUrl()
        {
            var lang = Language.RusWebLanguages.Contains(Language.CurrentLanguageName) ? "ru" : "en";
            return $"https://modplus.org/{lang}/revitplugins";
        }

        /// <summary>
        /// Создать вкладку на ленте с указанным именем, если её не существует
        /// </summary>
        /// <param name="application">UI Controlled Application</param>
        /// <param name="tabName">Имя вкладки</param>
        public static void CreateTabIfNoExist(UIControlledApplication application, string tabName)
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon.Tabs.All(t => t.Name != tabName))
            {
                application.CreateRibbonTab(tabName);
            }
        }

        /// <summary>
        /// Создать вкладку ModPlus на ленте, если не существует
        /// </summary>
        /// <param name="application">UI Controlled Application</param>
        public static void CreateModPlusTabIfNoExist(UIControlledApplication application)
        {
            CreateTabIfNoExist(application, TabName);
        }

        /// <summary>
        /// Убрать текст с кнопок маленького размера, расположенных в StackedPanel
        /// </summary>
        /// <param name="tabName">Имя вкладки, в которой выполнить поиск</param>
        /// <param name="pluginsToHideText">Коллекция имен плагинов</param>
        public static void HideTextOfSmallButtons(string tabName, ICollection<string> pluginsToHideText)
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                foreach (var ribbonTab in ribbon.Tabs)
                {
                    if (ribbonTab.Name != tabName)
                        continue;

                    foreach (var ribbonTabPanel in ribbonTab.Panels)
                    {
                        foreach (var sourceItem in ribbonTabPanel.Source.Items)
                        {
                            if (!(sourceItem is RibbonRowPanel ribbonRowPanel))
                                continue;

                            foreach (var ribbonItem in ribbonRowPanel.Items)
                            {
                                var pluginName = ribbonItem.Id.Split('%').LastOrDefault();
                                if (pluginsToHideText.Contains(pluginName) &&
                                    ribbonItem.Size == RibbonItemSize.Standard)
                                    ribbonItem.ShowText = false;
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

        /// <summary>
        /// Конвертирование локализованного имени плагина: разбивка на две строки примерно по середине
        /// </summary>
        /// <param name="lName">Локализованное имя плагина</param>
        public static string ConvertLName(string lName)
        {
            if (!lName.Contains(" "))
                return lName;
            if (lName.Length <= 8)
                return lName;
            if (lName.Count(x => x == ' ') == 1)
            {
                return lName.Split(' ')[0] + Environment.NewLine + lName.Split(' ')[1];
            }

            var center = lName.Length * 0.5;
            var nearestDelta = lName.Select((c, i) => new { index = i, value = c }).Where(w => w.value == ' ')
                .OrderBy(x => Math.Abs(x.index - center)).First().index;
            return lName.Substring(0, nearestDelta) + Environment.NewLine + lName.Substring(nearestDelta + 1);
        }

        /// <summary>
        /// Возвращает ссылку на панель <see cref="RibbonPanel"/>, находящуюся в указанной вкладке и имеющую указанное имя.
        /// Если панель не существует, то она создается
        /// </summary>
        /// <remarks>Метод также создает вкладку, если её не существует</remarks>
        /// <param name="application"><see cref="UIControlledApplication"/></param>
        /// <param name="tabName">Имя вкладки</param>
        /// <param name="panelName">Имя панели</param>
        public static RibbonPanel GetOrCreateRibbonPanel(
            UIControlledApplication application, string tabName, string panelName)
        {
            RibbonPanel panel = null;
            CreateModPlusTabIfNoExist(application);
            var rPanels = application.GetRibbonPanels(tabName);
            foreach (var rPanel in rPanels)
            {
                if (rPanel.Name.Equals(panelName))
                {
                    panel = rPanel;
                    break;
                }
            }

            if (panel == null)
                panel = application.CreateRibbonPanel(tabName, panelName);

            return panel;
        }

        private static void AddPanels(UIControlledApplication application)
        {
            try
            {
                var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("Revit");
                var pluginsToHide = new List<string>();

                // Проходим по группам
                if (confCuiXel != null)
                {
                    foreach (var group in confCuiXel.Elements("Group"))
                    {
                        var groupNameAttr = group.Attribute("GroupName");
                        if (groupNameAttr == null)
                            continue;

                        // Так как панель нельзя удалить и нужно создать до заполнения, нужно сначала проверить группу
                        if (!IsAnyFunctionContains(group))
                            continue;

                        // create the panel
                        var panel = GetOrCreateRibbonPanel(
                            application,
                            TabName,
                            Language.TryGetCuiLocalGroupName(groupNameAttr.Value));

                        // Проходим по функциям группы
                        foreach (var item in group.Elements())
                        {
                            if (item.Name == "Function")
                            {
                                var func = item;
                                var fName = func.Attribute("Name")?.Value ?? string.Empty;
                                var loadedFunction =
                                    LoadPluginsUtils.LoadedPlugins.FirstOrDefault(x => x.Name.Equals(fName));
                                if (loadedFunction == null)
                                    continue;

                                // Если функция имеет "подфункции", то делаем SplitButton
                                if (func.Elements("SubFunction").Any())
                                {
                                    var splitButtonData = new SplitButtonData(
                                        loadedFunction.Name,
                                        Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName));

                                    // add top function
                                    var firstButton = CreatePushButtonData(loadedFunction);
                                    var help = firstButton.GetContextualHelp();
                                    var sb = (SplitButton)panel.AddItem(splitButtonData);
                                    sb.AddPushButton(firstButton);
                                    sb.SetContextualHelp(help);

                                    // Затем добавляем подфункции
                                    foreach (var subFunc in func.Elements("SubFunction"))
                                    {
                                        var subFName = subFunc.Attribute("Name")?.Value ?? string.Empty;
                                        var loadedSubFunction =
                                            LoadPluginsUtils.LoadedPlugins.FirstOrDefault(x => x.Name.Equals(subFName));
                                        if (loadedSubFunction == null)
                                            continue;
                                        sb.AddPushButton(CreatePushButtonData(loadedSubFunction));
                                    }
                                }
                                else if (loadedFunction.SubFunctionsNames.Any())
                                {
                                    var splitButtonData = new SplitButtonData(
                                        loadedFunction.Name,
                                        Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName));

                                    // add top function
                                    var firstButton = CreatePushButtonData(loadedFunction);
                                    var help = firstButton.GetContextualHelp();
                                    splitButtonData.SetContextualHelp(help);

                                    var sb = (SplitButton)panel.AddItem(splitButtonData);
                                    sb.AddPushButton(firstButton);
                                    sb.SetContextualHelp(help);

                                    // internal sub functions
                                    for (var i = 0; i < loadedFunction.SubClassNames.Count; i++)
                                    {
                                        sb.AddPushButton(CreatePushButtonData(loadedFunction, i));
                                    }
                                }
                                else
                                {
                                    AddPushButton(panel, loadedFunction);
                                }
                            }
                            else if (item.Name == "StackedPanel")
                            {
                                var stackedItems = new List<RibbonItemData>();

                                var functions = item.Elements("Function").ToList();
                                var indexesOfSplitButtons = new List<Tuple<string, int>>();
                                foreach (var func in functions)
                                {
                                    var fName = func.Attribute("Name")?.Value ?? string.Empty;
                                    var loadedFunction =
                                        LoadPluginsUtils.LoadedPlugins.FirstOrDefault(x => x.Name.Equals(fName));
                                    if (loadedFunction == null)
                                        continue;

                                    pluginsToHide.Add(loadedFunction.Name);

                                    if (loadedFunction.SubFunctionsNames.Any())
                                    {
                                        var splitButtonData = new SplitButtonData(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName));

                                        stackedItems.Add(splitButtonData);
                                        indexesOfSplitButtons.Add(new Tuple<string, int>(loadedFunction.Name, stackedItems.Count - 1));
                                    }
                                    else
                                    {
                                        stackedItems.Add(CreatePushButtonData(loadedFunction));
                                    }
                                }

                                IList<RibbonItem> ribbonItems = new List<RibbonItem>();
                                if (stackedItems.Count == 1)
                                {
                                    var ribbonItem = panel.AddItem(stackedItems.First());
                                    ribbonItems = new List<RibbonItem> { ribbonItem };
                                }
                                else if (stackedItems.Count == 2)
                                {
                                    ribbonItems = panel.AddStackedItems(stackedItems[0], stackedItems[1]);
                                }
                                else if (stackedItems.Count == 3)
                                {
                                    ribbonItems = panel.AddStackedItems(stackedItems[0], stackedItems[1], stackedItems[2]);
                                }

                                if (indexesOfSplitButtons.Any())
                                {
                                    foreach (var tuple in indexesOfSplitButtons)
                                    {
                                        var func = functions.FirstOrDefault(f => f.Attribute("Name")?.Value == tuple.Item1);
                                        if (func == null)
                                            continue;

                                        var loadedFunction =
                                            LoadPluginsUtils.LoadedPlugins.FirstOrDefault(x =>
                                                x.Name.Equals(func.Attribute("Name")?.Value));
                                        if (loadedFunction == null)
                                            continue;

                                        // add top function
                                        var firstButton = CreatePushButtonData(loadedFunction);
                                        var help = firstButton.GetContextualHelp();

                                        var sb = ribbonItems[tuple.Item2] as SplitButton;
                                        sb.AddPushButton(firstButton);
                                        sb.SetContextualHelp(help);

                                        // internal sub functions
                                        for (var i = 0; i < loadedFunction.SubClassNames.Count; i++)
                                        {
                                            sb.AddPushButton(CreatePushButtonData(loadedFunction, i));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                HideTextOfSmallButtons(TabName, pluginsToHide);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void AddHelpPanel(UIControlledApplication application)
        {
            // create the panel
            var panel = application.CreateRibbonPanel(TabName, TabName);

            // user info
            var userInfoButton = new PushButtonData(
                "mpUserInfo",
                ConvertLName(Language.GetItem(LangItem, "h13")),
                Assembly.GetExecutingAssembly().Location,
                "ModPlus_Revit.App.UserInfoCommand");
            userInfoButton.LargeImage =
                new BitmapImage(
                    new Uri(
                        $"pack://application:,,,/Modplus_Revit_{VersionData.CurrentRevitVersion};component/Resources/UserInfo_32x32.png"));
            userInfoButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, GetHelpUrl("userinfo", "help")));
            panel.AddItem(userInfoButton);

            // settings
            var settingsButton = new PushButtonData(
                "mpSettings",
                Language.GetItem(LangItem, "h12"),
                Assembly.GetExecutingAssembly().Location,
                "ModPlus_Revit.App.SettingsCommand");
            settingsButton.LargeImage =
                new BitmapImage(
                    new Uri(
                        $"pack://application:,,,/Modplus_Revit_{VersionData.CurrentRevitVersion};component/Resources/HelpBt.png"));
            settingsButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, GetHelpUrl("mpsettings", "help")));
            panel.AddItem(settingsButton);
        }

        private static void AddPushButton(RibbonPanel panel, LoadedPlugin loadedPlugin)
        {
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = panel.AddItem(CreatePushButtonData(loadedPlugin)) as PushButton;
        }

        private static PushButtonData CreatePushButtonData(LoadedPlugin loadedPlugin)
        {
            return CreatePushButtonData(
                loadedPlugin.Name,
                Language.GetFunctionLocalName(loadedPlugin.Name, loadedPlugin.LName),
                Language.GetFunctionShortDescription(loadedPlugin.Name, loadedPlugin.Description),
                loadedPlugin.SmallIconUrl,
                loadedPlugin.BigIconUrl,
                Language.GetFunctionFullDescription(loadedPlugin.Name, loadedPlugin.FullDescription),
                loadedPlugin.ToolTipHelpImage, loadedPlugin.Location,
                loadedPlugin.ClassName,
                GetHelpUrl(loadedPlugin.Name));
        }

        private static PushButtonData CreatePushButtonData(LoadedPlugin loadedPlugin, int i)
        {
            return CreatePushButtonData(
                    loadedPlugin.SubFunctionsNames[i],
                    Language.GetFunctionLocalName(
                        loadedPlugin.Name,
                        loadedPlugin.SubFunctionsLNames[i], i + 1),
                    Language.GetFunctionShortDescription(
                        loadedPlugin.Name,
                        loadedPlugin.SubDescriptions[i], i + 1),
                    loadedPlugin.SubSmallIconsUrl[i], loadedPlugin.SubBigIconsUrl[i],
                    Language.GetFunctionFullDescription(
                        loadedPlugin.Name,
                        loadedPlugin.SubFullDescriptions[i], i + 1),
                    loadedPlugin.SubHelpImages[i], loadedPlugin.Location,
                    loadedPlugin.SubClassNames[i],
                    GetHelpUrl(loadedPlugin.Name));
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

        /// <summary>
        /// Проверка, что группа не пуста
        /// </summary>
        /// <param name="group">Проверяемая группа</param>
        /// <returns></returns>
        private static bool IsAnyFunctionContains(XElement group)
        {
            foreach (var item in group.Elements())
            {
                if (item.Name == "Function")
                {
                    if (LoadPluginsUtils.LoadedPlugins.Any(x => x.Name.Equals(item.Attribute("Name")?.Value)))
                    {
                        return true;
                    }
                }
                else if (item.Name == "StackedPanel")
                {
                    foreach (var func in item.Elements("Function"))
                    {
                        var loadedFunction = LoadPluginsUtils
                            .LoadedPlugins.FirstOrDefault(x => x.Name.Equals(func.Attribute("Name")?.Value));

                        if (loadedFunction != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
