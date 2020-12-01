namespace ModPlus_Revit.View.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Autodesk.Revit.DB;
    using Models.Parameters;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Логика взаимодействия для EditParametersControl.xaml
    /// </summary>
    public partial class EditParametersControl
    {
        private const string RebarParameters = "RebarParameters";
        private List<RebarParameterHolder> _cachedParameters;

        /// <summary>
        /// Параметры типа армирования в виде <see cref="RebarParameters"/>
        /// </summary>
        public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
            "Parameters", typeof(RebarParameters), typeof(EditParametersControl), new PropertyMetadata(default(RebarParameters), ParametersPropertyChangedCallback));

        /// <summary>
        /// Используется для <see cref="AreaReinforcement"/>
        /// </summary>
        public static readonly DependencyProperty IsAreaReinforcementProperty = DependencyProperty.Register(
            "IsAreaReinforcement", typeof(bool), typeof(EditParametersControl), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Используется для <see cref="PathReinforcement"/>
        /// </summary>
        public static readonly DependencyProperty IsPathReinforcementProperty = DependencyProperty.Register(
            "IsPathReinforcement", typeof(bool), typeof(EditParametersControl), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Используется для <see cref="Rebar"/>
        /// </summary>
        public static readonly DependencyProperty IsRebarProperty = DependencyProperty.Register(
            "IsRebar", typeof(bool), typeof(EditParametersControl), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Переопределение заголовка "Параметры"
        /// </summary>
        public static readonly DependencyProperty HeaderOverrideProperty = DependencyProperty.Register(
            "HeaderOverride", typeof(string), typeof(EditParametersControl), new PropertyMetadata(string.Empty, HeaderOverridePropertyChangedCallback));

        /// <summary>
        /// Initializes a new instance of the <see cref="EditParametersControl"/> class.
        /// </summary>
        public EditParametersControl()
        {
            InitializeComponent();

            // change theme
            ModPlusAPI.Windows.Helpers.WindowHelpers.ChangeStyleForResourceDictionary(Resources);

            // change lang
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
        }

        /// <summary>
        /// Параметры типа армирования в виде <see cref="RebarParameters"/>
        /// </summary>
        public RebarParameters Parameters
        {
            get => (RebarParameters)GetValue(ParametersProperty);
            set => SetValue(ParametersProperty, value);
        }

        /// <summary>
        /// Используется для <see cref="AreaReinforcement"/>
        /// </summary>
        public bool IsAreaReinforcement
        {
            get => (bool)GetValue(IsAreaReinforcementProperty);
            set => SetValue(IsAreaReinforcementProperty, value);
        }

        /// <summary>
        /// Используется для <see cref="PathReinforcement"/>
        /// </summary>
        public bool IsPathReinforcement
        {
            get => (bool)GetValue(IsPathReinforcementProperty);
            set => SetValue(IsPathReinforcementProperty, value);
        }

        /// <summary>
        /// Используется для <see cref="Rebar"/>
        /// </summary>
        public bool IsRebar
        {
            get => (bool)GetValue(IsRebarProperty);
            set => SetValue(IsRebarProperty, value);
        }

        /// <summary>
        /// Переопределение заголовка "Параметры"
        /// </summary>
        public string HeaderOverride
        {
            get => (string)GetValue(HeaderOverrideProperty);
            set => SetValue(HeaderOverrideProperty, value);
        }

        /// <summary>
        /// Проверка параметров на допустимость текущему документу, кэширование и обновление таблицы
        /// </summary>
        /// <param name="updateDisplay">Обновлять ли строку с представлением значений</param>
        public void ValidateAndSetParametersToDataGrid(bool updateDisplay)
        {
            DgParameters.ItemsSource = null;
            if (Parameters == null)
                return;
            var allowableParameters = GetAllowableParametersFromCurrentDocument();
            _cachedParameters = new List<RebarParameterHolder>();
            foreach (var parameterHolder in Parameters.Parameters.Where(p => !string.IsNullOrEmpty(p.Value)))
            {
                _cachedParameters.Add((RebarParameterHolder)parameterHolder.Clone());
            }

            Parameters.Parameters.Clear();
            foreach (var allowableParameter in allowableParameters)
            {
                allowableParameter.Value =
                    _cachedParameters.FirstOrDefault(p => p.IsEqualTo(allowableParameter))?.Value ?? string.Empty;
                Parameters.Parameters.Add(allowableParameter);
            }

            DgParameters.ItemsSource = Parameters.Parameters;

            if (updateDisplay)
                TbParametersDisplay.Text = GetDisplayParametersString();
        }

        private static void HeaderOverridePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditParametersControl editParametersControl && e.NewValue != null)
                editParametersControl.TbHeader.Text = e.NewValue.ToString();
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = !Popup.IsOpen;
        }

        private void Popup_OnOpened(object sender, EventArgs e)
        {
            BtPaste.IsEnabled = Clipboard.ContainsData(RebarParameters);
            ValidateAndSetParametersToDataGrid(false);
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = false;
        }

        private void BtCancel_OnClick(object sender, RoutedEventArgs e)
        {
            if (_cachedParameters != null)
            {
                foreach (var cachedParameter in _cachedParameters)
                {
                    var parameter = Parameters.Parameters.FirstOrDefault(p => p.IsEqualTo(cachedParameter));
                    if (parameter == null)
                        continue;

                    parameter.Value = cachedParameter.Value;
                }
            }

            Popup.IsOpen = false;
        }

        private IEnumerable<RebarParameterHolder> GetAllowableParametersFromCurrentDocument()
        {
            // Общие параметры, присущие всем элементам
            var systemParameters = new List<BuiltInParameter>
            {
                //// Раздел
                BuiltInParameter.NUMBER_PARTITION_PARAM,
                //// Марка
                BuiltInParameter.ALL_MODEL_MARK,
                //// Комментарии
                BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
            };

            foreach (var builtInParameter in systemParameters)
            {
                yield return new RebarParameterHolder
                {
                    Name = LabelUtils.GetLabelFor(builtInParameter),
                    BuiltInParameter = builtInParameter.ToString()
                };
            }

            if (IsAreaReinforcement)
            {
                var category = RevitInterop.Document.Settings.Categories.get_Item(BuiltInCategory.OST_AreaRein);
                foreach (var parameterHolder in GetSharedParameters(category))
                    yield return parameterHolder;
            }

            if (IsPathReinforcement)
            {
                var category = RevitInterop.Document.Settings.Categories.get_Item(BuiltInCategory.OST_PathRein);
                foreach (var parameterHolder in GetSharedParameters(category))
                    yield return parameterHolder;
            }

            if (IsRebar)
            {
                systemParameters.AddRange(new[]
                {
                    //// Марка спецификации
                    BuiltInParameter.REBAR_ELEM_SCHEDULE_MARK,
                    //// Номер арматурного стержня
                    BuiltInParameter.REBAR_NUMBER,
                });

                var category = RevitInterop.Document.Settings.Categories.get_Item(BuiltInCategory.OST_Rebar);
                foreach (var parameterHolder in GetSharedParameters(category))
                    yield return parameterHolder;
            }
        }

        private static IEnumerable<RebarParameterHolder> GetSharedParameters(Category category)
        {
            var definitionBindingMapIterator = RevitInterop.Document.ParameterBindings.ForwardIterator();
            definitionBindingMapIterator.Reset();
            while (definitionBindingMapIterator.MoveNext())
            {
                if (definitionBindingMapIterator.Current is InstanceBinding binding &&
                    binding.Categories.Contains(category))
                {
                    if (definitionBindingMapIterator.Key is InternalDefinition definition &&
                        definition.ParameterType == ParameterType.Text &&
                        RevitInterop.Document.GetElement(definition.Id) is SharedParameterElement)
                    {
                        yield return new RebarParameterHolder
                        {
                            Name = definition.Name,
                            Value = string.Empty,
                            BuiltInParameter = string.Empty
                        };
                    }
                }
            }
        }

        private void Popup_OnClosed(object sender, EventArgs e)
        {
            TbParametersDisplay.Text = GetDisplayParametersString();
        }

        private static void ParametersPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditParametersControl control)
                control.ValidateAndSetParametersToDataGrid(true);
        }

        private string GetDisplayParametersString()
        {
            if (Parameters is RebarParameters rebarParameters)
            {
                var parameters = rebarParameters.Parameters
                    .Where(p => !string.IsNullOrEmpty(p.Value))
                    .Select(parameterHolder => $"{parameterHolder.Name}: {parameterHolder.Value}").ToList();

                return string.Join(", ", parameters);
            }

            return string.Empty;
        }

        private void BtCopy_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(RebarParameters, Parameters.ToStringRepresent());
        }

        private void BtPaste_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = Clipboard.GetData(RebarParameters);
                var rebarParameters = Models.Parameters.RebarParameters.GetFromStringRepresent(data.ToString());

                foreach (var parameterHolder in Parameters.Parameters)
                {
                    var parameter = rebarParameters.Parameters.FirstOrDefault(p => p.IsEqualTo(parameterHolder));
                    if (parameter == null || string.IsNullOrEmpty(parameter.Value))
                        continue;
                    parameterHolder.Value = parameter.Value;
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}
