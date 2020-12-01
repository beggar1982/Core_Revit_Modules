namespace ModPlus_Revit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using JetBrains.Annotations;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using Utils;

    /// <summary>
    /// Сервис работы со списками типов арматуры в модели
    /// </summary>
    [PublicAPI]
    public class RebarTypeService : VmBase
    {
        private const double Tolerance = 0.0001;
        private const string LangItem = "RevitDlls";

        private static RebarTypeService _instance;

        private ObservableCollection<RebarBarType> _cachedRebarBarTypes;
        private ObservableCollection<string> _cachedRebarBarTypeNames;
        private ObservableCollection<string> _cachedDeformedRebarBarTypeNames;
        private ObservableCollection<string> _cachedPlainRebarBarTypeNames;

        private RebarTypeService()
        {
        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static RebarTypeService Instance => _instance ?? (_instance = new RebarTypeService());
        
        /// <summary>
        /// Список типов арматурных стержней в текущем документе
        /// </summary>
        public List<RebarBarType> RebarBarTypes
        {
            get
            {
                var collector = new FilteredElementCollector(RevitInterop.Document)
                    .OfClass(typeof(RebarBarType))
                    .Cast<RebarBarType>().ToList();
                var rebarBarTypes = new List<RebarBarType>();
                foreach (var barType in collector)
                {
                    rebarBarTypes.Add(barType);
                }

                rebarBarTypes.Sort((r1, r2) => string.Compare(r1.Name, r2.Name, StringComparison.Ordinal));

                return new List<RebarBarType>(rebarBarTypes);
            }
        }

        /// <summary>
        /// Кэшированный список типов арматурных стержней. Список создается при первом обращении к свойству.
        /// При последующих обращениях возвращается тот-же самый список
        /// <para>Данное свойство нужно использовать при инициализации начальных свойств классов,
        /// представляющих настройки армирования</para></summary>
        public ObservableCollection<RebarBarType> CachedRebarTypes
        {
            get
            {
                if (_cachedRebarBarTypes != null)
                {
                    return _cachedRebarBarTypes;
                }

                _cachedRebarBarTypes = new ObservableCollection<RebarBarType>();
                var collector = new FilteredElementCollector(RevitInterop.Document)
                    .OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
                foreach (var barType in collector)
                {
                    _cachedRebarBarTypes.Add(barType);
                }

                return _cachedRebarBarTypes;
            }
        }

        /// <summary>
        /// Кэшированный список имен типов арматурных стержней. Список создается при первом обращении к свойству.
        /// При последующих обращениях возвращается тот-же самый список
        /// <para>Данное свойство нужно использовать при инициализации начальных свойств классов,
        /// представляющих настройки армирования</para>
        /// <para>Первое значение в списке - "Создать"</para></summary>
        public ObservableCollection<string> CachedRebarBarTypeNames
        {
            get
            {
                if (_cachedRebarBarTypeNames != null)
                {
                    return _cachedRebarBarTypeNames;
                }

                _cachedRebarBarTypeNames = new ObservableCollection<string>
                {
                    Language.GetItem(LangItem, "create")
                };

                foreach (var rbt in RebarBarTypes)
                    _cachedRebarBarTypeNames.Add(rbt.Name);
                return _cachedRebarBarTypeNames;
            }
        }

        /// <summary>
        /// Кэшированный список имен типов арматурных стержней периодического профиля в текущем проекте.
        /// Список создается при первом обращении к свойству.
        /// При последующих обращениях возвращается тот-же самый список
        /// <para>Первое значение в списке - "Создать"</para></summary>
        public ObservableCollection<string> CachedDeformedRebarBarTypeNames
        {
            get
            {
                if (_cachedDeformedRebarBarTypeNames != null)
                {
                    return _cachedDeformedRebarBarTypeNames;
                }

                _cachedDeformedRebarBarTypeNames = new ObservableCollection<string>
                {
                    Language.GetItem(LangItem, "create")
                };

                RebarBarTypes.Where(r => r.DeformationType == RebarDeformationType.Deformed).ToList()
                    .ForEach(rbt => _cachedDeformedRebarBarTypeNames.Add(rbt.Name));
                return _cachedDeformedRebarBarTypeNames;
            }
        }

        /// <summary>
        /// Кэшированный список имен типов гладких арматурных стержней в текущем проекте.
        /// Список создается при первом обращении к свойству.
        /// При последующих обращениях возвращается тот-же самый список
        /// <para>Первое значение в списке - "Создать"</para></summary>
        public ObservableCollection<string> CachedPlainRebarBarTypeNames
        {
            get
            {
                if (_cachedPlainRebarBarTypeNames != null)
                {
                    return _cachedPlainRebarBarTypeNames;
                }

                _cachedPlainRebarBarTypeNames = new ObservableCollection<string>
                {
                    Language.GetItem(LangItem, "create")
                };

                RebarBarTypes.Where(r => r.DeformationType == RebarDeformationType.Plain).ToList()
                    .ForEach(rbt => _cachedPlainRebarBarTypeNames.Add(rbt.Name));
                return _cachedPlainRebarBarTypeNames;
            }
        }

        /// <summary>
        /// Очистка кэша - удаление кэшированных списков
        /// </summary>
        public void ClearCache()
        {
            _cachedRebarBarTypes = null;
            _cachedRebarBarTypeNames = null;
            _cachedDeformedRebarBarTypeNames = null;
            _cachedPlainRebarBarTypeNames = null;

            OnPropertyChanged(nameof(CachedRebarTypes));
            OnPropertyChanged(nameof(CachedRebarBarTypeNames));
            OnPropertyChanged(nameof(CachedDeformedRebarBarTypeNames));
            OnPropertyChanged(nameof(CachedPlainRebarBarTypeNames));
        }

        /// <summary>
        /// Создание арматурного стержня по диаметру
        /// </summary>
        /// <param name="diameterInMm">Диаметр стержня в мм</param>
        /// <param name="rebarDeformationType">Профиль: периодического профиля, гладкие</param>
        public RebarBarType CreateRebarBarType(
            double diameterInMm, RebarDeformationType rebarDeformationType)
        {
            // Так как вариант "Создать" может быть указан несколько раз,
            // то при втором обращении тип арматурного стержня уже может существовать
            // Поэтому нужно сначала поискать его
            // Несущая арматура ⌀
            var rebarBarTypeName = $"{Language.GetItem(LangItem, "rn")}{diameterInMm}";
            var rebarBarType = GetRebarBarType(rebarBarTypeName, diameterInMm);
            if (rebarBarType != null)
            {
                RebarBendRuleService.Instance.CheckAndFixRebarBend(rebarBarType, true);
                return rebarBarType;
            }

            var doc = RevitInterop.Document;
            rebarBarType = RebarBarType.Create(doc);
            rebarBarType.BarDiameter = diameterInMm.MmToFt();
            rebarBarType.DeformationType = rebarDeformationType;

            rebarBarType.Name = rebarBarTypeName;

            RebarBendRuleService.Instance.CheckAndFixRebarBend(rebarBarType, false);

            return rebarBarType;
        }

        /// <summary>
        /// Получение типа арматурного стержня по имени и диаметру
        /// </summary>
        /// <param name="rebarBarTypeName">Имя типа арматурного стержня</param>
        /// <param name="diameterInMm">Диаметр стержня в мм</param>
        private RebarBarType GetRebarBarType(string rebarBarTypeName, double diameterInMm)
        {
            if (RebarBarTypes.Any())
            {
                foreach (var barType in RebarBarTypes)
                {
                    if (Math.Abs(barType.BarDiameter - diameterInMm.MmToFt()) < 0.0001 &&
                        barType.Name.Contains(rebarBarTypeName))
                    {
                        return barType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Получение типа арматурного стержня по имени или создание по диаметру, если по имени не найден
        /// <para>В методе происходит также проверка типа арматурного стержня на соответствие нормам СП</para></summary>
        /// <param name="rebarBarTypeName">Имя типа арматурного стержня</param>
        /// <param name="diameterInMm">Диаметр стержня в мм</param>
        /// <param name="rebarDeformationType">Профиль: периодического профиля, гладкие</param>
        /// <returns>Null - если не удалось получить и создать тип стержня (маловероятно)</returns>
        public RebarBarType GetOrCreateRebarBarType(
            string rebarBarTypeName, double diameterInMm, RebarDeformationType rebarDeformationType)
        {
            var needCreate = false;
            RebarBarType rebarBarType = null;
            if (RebarBarTypes.Any())
            {
                foreach (var barType in RebarBarTypes)
                {
                    if (Math.Abs(barType.BarDiameter - diameterInMm.MmToFt()) < Tolerance &&
                        barType.Name.Contains(rebarBarTypeName))
                    {
                        rebarBarType = barType;
                        break;
                    }
                }
            }
            else
            {
                needCreate = true;
            }

            if (needCreate || rebarBarType == null)
            {
                rebarBarType = CreateRebarBarType(diameterInMm, rebarDeformationType);

                ClearCache();
            }

            if (rebarBarType != null)
            {
                RebarBendRuleService.Instance.CheckAndFixRebarBend(rebarBarType, true);
            }

            return rebarBarType;
        }

        /// <summary>
        /// Возвращает экземпляр <see cref="RebarBarType"/> по имени
        /// </summary>
        /// <param name="rebarBarTypeName">Имя типа арматурного стержня</param>
        public RebarBarType GetRebarBarTypeByName(string rebarBarTypeName)
        {
            foreach (var barType in RebarBarTypes)
            {
                if (barType.Name == rebarBarTypeName)
                {
                    return barType;
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает тип крюка с указанным углом загиба и стилем, если такой имеется в проекте. Иначе null
        /// </summary>
        /// <param name="angleInDegrees">Угол загиба в градусах</param>
        /// <param name="rebarStyle">Стиль арматурного стержня</param>
        public RebarHookType GetOrCreateRebarHookType(double angleInDegrees, RebarStyle rebarStyle)
        {
            if (Math.Abs(angleInDegrees) < 0.01)
                return null;

            foreach (var rebarHookType in new FilteredElementCollector(RevitInterop.Document)
                .OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>())
            {
                if (Math.Abs(rebarHookType.HookAngle.RadianToDegree() - angleInDegrees) < 1.0 &&
                    rebarHookType.Style == rebarStyle)
                    return rebarHookType;
            }

            var hook = RebarHookType.Create(RevitInterop.Document, angleInDegrees.DegreeToRadian(), 10);
            hook.Style = rebarStyle;
            return hook;
        }

        /// <summary>
        /// Есть ли в списке типов арматурных стержней текущего проекта арматура с указанным именем типа
        /// </summary>
        /// <param name="rebarBarTypeName">Имя типа арматурного стержня, которое требуется проверить</param>
        /// <param name="diameter">Диаметр найденного типа арматурного стержня</param>
        public bool HasRebarBarType(string rebarBarTypeName, out double diameter)
        {
            diameter = 0.0;
            if (string.IsNullOrEmpty(rebarBarTypeName))
                return false;

            foreach (var rebarBarType in RebarBarTypes)
            {
                if (rebarBarType.Name.Equals(rebarBarTypeName))
                {
                    diameter = rebarBarType.GetDiameterInMm();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Есть ли в списке типов арматурных стержней текущего проекта арматура с указанным диаметром
        /// </summary>
        /// <param name="diameter">Диаметр арматурного стержня, который требуется проверить</param>
        /// <param name="rebarBarTypeName">Имя найденного типа арматурного стержня</param>
        public bool HasRebarBarType(double diameter, out string rebarBarTypeName)
        {
            foreach (var rebarBarType in RebarBarTypes)
            {
                if (Math.Abs(rebarBarType.GetDiameterInMm() - diameter) < 0.1)
                {
                    rebarBarTypeName = rebarBarType.Name;
                    return true;
                }
            }

            rebarBarTypeName = string.Empty;
            return false;
        }

        /// <summary>
        /// Установка значений для свойств "Тип арматурного стержня" и "Диаметр арматурного стержня" в настройках
        /// армирования. Установка происходит через инкапсулированный метод, что позволяет не вызывать событие
        /// PropertyChanged у объекта настроек армирования
        /// </summary>
        /// <param name="setProperties">Инкапсулированный метод установки свойств "Тип арматурного стержня" и
        /// "Диаметр арматурного стержня" в объекте настроек армирования</param>
        /// <param name="defaultName">Тип арматурного стержня, указанный в настройках армирования по умолчанию</param>
        /// <param name="defaultDiameter">Диаметр арматурного стержня, указанный в настройках армирования по умолчанию</param>
        public void SetRebarTypeNameAndDiameter(
            Action<string, double> setProperties, string defaultName, double defaultDiameter)
        {
            foreach (var rbt in CachedRebarTypes)
            {
                var diameterInMm = rbt.GetDiameterInMm();
                if (rbt.Name == defaultName)
                {
                    setProperties(rbt.Name, diameterInMm);
                    return;
                }
            }

            foreach (var rbt in CachedRebarTypes)
            {
                var diameterInMm = rbt.GetDiameterInMm();
                if (Math.Abs(diameterInMm - defaultDiameter) < 0.1)
                {
                    setProperties(rbt.Name, diameterInMm);
                    return;
                }
            }

            setProperties(CachedRebarBarTypeNames.First(), defaultDiameter);
        }
    }
}
