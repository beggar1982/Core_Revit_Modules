namespace ModPlus_Revit.Services
{
    using System;
    using System.Collections.Generic;
    using Autodesk.Revit.DB.Structure;
    using Enums;
    using JetBrains.Annotations;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using Utils;

    /// <summary>
    /// Сервис работы с правилами гиба арматуры
    /// </summary>
    [PublicAPI]
    public class RebarBendRuleService : VmBase
    {
        private const string LangName = "ReinforcementBendRules";

        // Список типов арматурного стержня, для которых уже отображалось сообщение о нарушении норм
        private readonly List<string> _rebarBarTypesShowedError;
        private static RebarBendRuleService _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="RebarBendRuleService"/> class.
        /// </summary>
        public RebarBendRuleService()
        {
            _rebarBarTypesShowedError = new List<string>();
        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static RebarBendRuleService Instance => _instance ?? (_instance = new RebarBendRuleService());

        /// <summary>
        /// Источник правил гиба
        /// </summary>
        public RebarBendRuleSourceDocument SourceDocument
        {
            get =>
                Enum.TryParse(
                    UserConfigFile.GetValue(LangName, nameof(SourceDocument)),
                    out RebarBendRuleSourceDocument rebarBendRuleSourceDocument)
                    ? rebarBendRuleSourceDocument
                    : RebarBendRuleSourceDocument.Sp63;
            set
            {
                UserConfigFile.SetValue(LangName, nameof(SourceDocument), value.ToString(), true);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SourceDocumentTitle));
                OnPropertyChanged(nameof(RuleDescription));
            }
        }

        /// <summary>
        /// Название документа и пункта правила
        /// </summary>
        public string SourceDocumentTitle
        {
            get
            {
                switch (SourceDocument)
                {
                    case RebarBendRuleSourceDocument.Sp63:
                        return Language.GetItem(LangName, "t1");
                    case RebarBendRuleSourceDocument.En1992:
                        return Language.GetItem(LangName, "t2");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Описание правила
        /// </summary>
        public string RuleDescription
        {
            get
            {
                switch (SourceDocument)
                {
                    case RebarBendRuleSourceDocument.Sp63:
                        return Language.GetItem(LangName, "r1");
                    case RebarBendRuleSourceDocument.En1992:
                        return Language.GetItem(LangName, "r2");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Исправлять диаметры загиба согласно нормативной документации в существующих типах <see cref="RebarBarType"/>
        /// </summary>
        public bool FixBendInExistRebarBarTypes
        {
            get => !bool.TryParse(UserConfigFile.GetValue(LangName, nameof(FixBendInExistRebarBarTypes)), out var b) || b;
            set
            {
                UserConfigFile.SetValue(LangName, nameof(FixBendInExistRebarBarTypes), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Очистить список типов арматуры, для которой было показано сообщение о нарушении норм
        /// </summary>
        public void ClearShowedErrors()
        {
            _rebarBarTypesShowedError.Clear();
        }

        /// <summary>
        /// Проверка типа арматурного стержня на соответствие нормам гиба
        /// <para>Метод должен выполняться внутри транзакции!</para>
        /// </summary>
        /// <param name="rebarBarType">Тип арматурного стержня</param>
        /// <param name="showMessage">Показывать ли сообщение в случае несоответствия нормам</param>
        public void CheckAndFixRebarBend(RebarBarType rebarBarType, bool showMessage)
        {
            var ruleDoc = SourceDocument;
            var barDiameter = rebarBarType.BarDiameter;
            var barDiameterInMm = rebarBarType.GetDiameterInMm();
            
            var multiplyFactor = GetMultiplyFactor(barDiameterInMm, rebarBarType.DeformationType, out var armType);
            
            if (NeedFix(rebarBarType, multiplyFactor))
            {
                if (showMessage && !_rebarBarTypesShowedError.Contains(rebarBarType.Name))
                {
                    ShowMessage(
                        rebarBarType.Name,
                        rebarBarType.StandardBendDiameter.FtToMm(),
                        GetSourceDocumentName(ruleDoc),
                        armType,
                        barDiameterInMm,
                        multiplyFactor);

                    _rebarBarTypesShowedError.Add(rebarBarType.Name);
                }

                rebarBarType.StandardBendDiameter = multiplyFactor * barDiameter;
                rebarBarType.StandardHookBendDiameter = multiplyFactor * barDiameter;
                rebarBarType.StirrupTieBendDiameter = multiplyFactor * barDiameter;
            }
        }

        /// <summary>
        /// Получение нормативного радиуса гиба 
        /// </summary>
        /// <param name="rebarBarType">Тип арматурного стержня</param>
        public double GetNormativeBendRadiusInMm(RebarBarType rebarBarType)
        {
            var barDiameterInMm = rebarBarType.GetDiameterInMm();
            var multiplyFactor = GetMultiplyFactor(barDiameterInMm, rebarBarType.DeformationType, out _);
            return barDiameterInMm * multiplyFactor / 2;
        }

        /// <summary>
        /// Получение нормативного радиуса гиба для указанного диаметра
        /// </summary>
        /// <param name="diameterInMm">Диаметр в мм</param>
        /// <param name="rebarDeformationType">Тип профиля арматуры - периодического или гладкая</param>
        public double GetNormativeBendRadiusInMm(double diameterInMm, RebarDeformationType rebarDeformationType)
        {
            var multiplyFactor = GetMultiplyFactor(diameterInMm, rebarDeformationType, out _);
            return diameterInMm * multiplyFactor / 2;
        }

        private static void ShowMessage(
            string rebarBarTypeName,
            double currentBendDiameterInMm,
            string docName,
            string armType,
            double diameterInMm,
            double multiplyFactor)
        {
            ////<m3>Для арматурного стержня</m3>
            ////<m4>значение свойства "Стандартный диаметр загиба"</m4>
            ////<m5>не соответствует нормам</m5>
            ////<m6>Минимальное значение диаметра загиба для арматуры</m6>
            ////<m7>диаметром</m7>
            ////<m8>должно равняться:</m8>
            ////<m9>Значение свойства будет заменено</m9>

            MessageBox.Show(
                $"{Language.GetItem(LangName, "m3")} \"{rebarBarTypeName}\" {Language.GetItem(LangName, "m4")} {currentBendDiameterInMm} {Language.GetItem(LangName, "mm")} {Language.GetItem(LangName, "m5")} {docName}.{Environment.NewLine}{Language.GetItem(LangName, "m6")}{(!string.IsNullOrEmpty(armType) ? (" \"" + armType + "\"") : string.Empty)} {Language.GetItem(LangName, "m7")} {diameterInMm} {Language.GetItem(LangName, "mm")} {Language.GetItem(LangName, "m8")} {multiplyFactor} * {diameterInMm} = {multiplyFactor * diameterInMm} {Language.GetItem(LangName, "mm")}.{Environment.NewLine}{Language.GetItem(LangName, "m9")}",
                MessageBoxIcon.Alert);
        }

        private static string GetSourceDocumentName(RebarBendRuleSourceDocument sourceDocument)
        {
            if (sourceDocument == RebarBendRuleSourceDocument.Sp63)
                return Language.GetItem(LangName, "d1");
            if (sourceDocument == RebarBendRuleSourceDocument.En1992)
                return Language.GetItem(LangName, "d2");
            throw new ArgumentException();
        }

        private double GetMultiplyFactor(double diameterInMm, RebarDeformationType deformationType, out string armType)
        {
            var ruleDoc = SourceDocument;
            var multiplyFactor = double.NaN;
            armType = string.Empty;
            
            if (ruleDoc == RebarBendRuleSourceDocument.Sp63)
            {
                if (deformationType == RebarDeformationType.Plain)
                {
                    multiplyFactor = diameterInMm < 20.0 ? 2.5 : 4;
                    armType = Language.GetItem(LangName, "m2");
                }
                else
                {
                    multiplyFactor = diameterInMm < 20.0 ? 5 : 8;
                    armType = Language.GetItem(LangName, "m1");
                }
            }
            else if (ruleDoc == RebarBendRuleSourceDocument.En1992)
            {
                multiplyFactor = diameterInMm <= 16.0 ? 4 : 7;
            }

            return multiplyFactor;
        }

        private static bool NeedFix(RebarBarType rebarBarType, double multiplyFactor)
        {
            var barDiameterInMm = rebarBarType.GetDiameterInMm();
            if (Math.Abs((barDiameterInMm * multiplyFactor) - rebarBarType.StandardBendDiameter.FtToMm()) > 0.1)
                return true;
            if (Math.Abs((barDiameterInMm * multiplyFactor) - rebarBarType.StandardHookBendDiameter.FtToMm()) > 0.1)
                return true;
            if (Math.Abs((barDiameterInMm * multiplyFactor) - rebarBarType.StirrupTieBendDiameter.FtToMm()) > 0.1)
                return true;

            return false;
        }
    }
}
