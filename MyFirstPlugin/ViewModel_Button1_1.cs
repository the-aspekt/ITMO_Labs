using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RevitAPITrainingLibrary;
using System.Windows.Input;

namespace MyFirstPlugin
{
    public class ViewModel_Button1_1
    {
        private ExternalCommandData _commandData;
        private Document _doc;

        public ICommand CreateSheetCommand { get; }
        public List<Family> BasicFamily { get; set; } = new List<Family>();
        public Family SelectedBasicFamily { get; set; }
        public List<Family> TargetFamily { get; set; } = new List<Family>();
        public Family SelectedTargetFamily { get; set; }
        public List<FamilySymbol> TitleBlocks { get; set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedTitleBlock { get; set; }

        public List<View> Views { get; set; } = new List<View>();
        public View SelectedView { get; set; }

        private string parameterName = "ADSK_Назначение вида";
        public string Designed_By { get; set; }
        // public XYZ Point { get; set; }

        public List<XYZ> Points { get; set; } = new List<XYZ>();

        public event EventHandler HideRequest;
        private void RaiseHideRequest()
        {
            HideRequest?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ShowRequest;
        private void RaiseShowRequest()
        {
            ShowRequest?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }

        public ViewModel_Button1_1(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _doc = _commandData.Application.ActiveUIDocument.Document;
            //CreateSheetCommand = new DelegateCommand(OnCreateSheetCommand);

            TitleBlocks = TitleblockUtils.GetSymbols(_doc);
            SelectedTitleBlock = TitleBlocks.FirstOrDefault();
            BasicFamily = FamiliesUtils.GetFamilies(_doc);
            SelectedBasicFamily = BasicFamily.FirstOrDefault(f => f.Name.Contains("Плита")
                                    && f.FamilyCategory != Category.GetCategory(_doc, BuiltInCategory.OST_DetailComponents));
            TargetFamily = FamiliesUtils.GetFamilies(_doc);
            SelectedTargetFamily = TargetFamily.FirstOrDefault(f => f.Name.Contains("Плита")
                                    && f.FamilyCategory.Id == Category.GetCategory(_doc, BuiltInCategory.OST_DetailComponents).Id);
            Designed_By = "Тест плит";
            Views = ViewsUtils.GetLegends(_doc);

            CreateSheetCommand = new RelayCommand(OnCreateSheetCommand, OnCreateSheetCommandCanExecute);
        }
        
        private bool OnCreateSheetCommandCanExecute(object sender)
        {
            if (SelectedTitleBlock == null || SelectedBasicFamily == null || SelectedTargetFamily == null)
            {
                return false;
            }
            return true;
        }

        public string GetTemplateName(FamilySymbol panelsFamilySymbol)
        {
            string templateName = "Шаблон ";
            bool isBy3SidesSupported = panelsFamilySymbol.LookupParameter("Опирание по трём сторонам").AsInteger() == 1;
            if (isBy3SidesSupported)
                templateName += "ПТ ";
            else
                templateName += "ПД ";
            double currentLength = panelsFamilySymbol.LookupParameter("Панель_Длина").AsDouble();
            currentLength = UnitUtils.ConvertFromInternalUnits(currentLength, UnitTypeId.Millimeters);
            if (currentLength > 4999)
                templateName += "60";
            else
                templateName += "45";
            bool hasAperture = panelsFamilySymbol.LookupParameter("Проём__Вкл-выкл").AsInteger() == 1;
            if (hasAperture)
                templateName += " проем";

            return templateName;
        }
        public string GetSpecialTemplateName(FamilySymbol panelsFamilySymbol)
        {
            string templateName = "!Шаблон ";
            bool isBy3SidesSupported = panelsFamilySymbol.LookupParameter("Опирание по трём сторонам").AsInteger() == 1;
            if (isBy3SidesSupported)
                templateName += "ПТ ";
            else
                templateName += "ПД ";
            double currentLength = panelsFamilySymbol.LookupParameter("Панель_Длина").AsDouble();
            currentLength = UnitUtils.ConvertFromInternalUnits(currentLength, UnitTypeId.Millimeters);
            if (currentLength > 4999)
                templateName += "60";
            else
                templateName += "45";
            bool hasAperture = panelsFamilySymbol.LookupParameter("Проём__Вкл-выкл").AsInteger() == 1;
            if (hasAperture)
                templateName += " проем";

            return templateName;
        }

        private void OnCreateSheetCommand(object sender)
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;
            Definition definitionDesigned_By = null;

            if (SelectedTitleBlock == null || SelectedBasicFamily == null || SelectedTargetFamily == null)
            {
                return;
            }

            if (Designed_By.Length != 0)
            {
                CategorySet newCategorySet = new CategorySet();
                newCategorySet.Insert(Category.GetCategory(document, BuiltInCategory.OST_Sheets));
                definitionDesigned_By = DefinitionsUtils.FindOrCreateDefinition(_commandData, parameterName, newCategorySet);
            }

            List<FamilySymbol> BasicFamilySymbols = FamiliesSymbolsUtils.GetUniqueSymbolsInModel(document, SelectedBasicFamily);

            List<FamilySymbol> TargetFamilySymbols = SelectedTargetFamily.GetFamilySymbolIds()
                                                .Select(sIds => document.GetElement(sIds))
                                                .Cast<FamilySymbol>()
                                                .ToList();

            FamilySymbol sampleOfTFS = TargetFamilySymbols.FirstOrDefault();
            FamilySymbol sampleOfBFS = BasicFamilySymbols.FirstOrDefault();

            //получаем список совпадающих в 2-х семействах параметров
            List<string> commonStrings = FamiliesSymbolsUtils.InteresectSymbolsParameters(sampleOfBFS,sampleOfTFS);

            List<ViewDrafting> existingViewDrafting = ViewsUtils.GetDraftingViews(document);

            TaskDialog.Show("Внимание", $"Будет создано: {BasicFamilySymbols.Count} листов"); //{BasicFamilySymbols.Count}

            using (var t = new Transaction(document, "Создание листов"))
            {
                t.Start();

                for (int i = 0; i < 1; i++) //BasicFamilySymbols.Count
                {
                    FamilySymbol currentBFS = BasicFamilySymbols[i];

                    //получаем параметры плиты для выбора шаблона
                    string templateName = GetTemplateName(currentBFS);
                    bool isMirrored = currentBFS.LookupParameter("Отзеркаленная панель").AsInteger() == 1;

                    //получить базовый чертежный вид
                    var viewDraftings = ViewsUtils.GetDraftingViews(document);
                    ViewDrafting viewTemplate = viewDraftings.Where(v => v.Name == templateName).FirstOrDefault();

                    if (viewTemplate == null)
                    {
                        TaskDialog.Show("Ошибка", $"Отсутствует шаблон для оформления плиты перекрытия {currentBFS.Name}! \nПлита будет пропущена.");
                        continue;
                    }

                    // ViewFamilyType familyType = ViewsUtils.GetViewFamilyTypes(document).Where(p => p.ViewFamily == ViewFamily.Drafting).FirstOrDefault();
                    ElementClassFilter instanceFilter = new ElementClassFilter(typeof(FamilyInstance));
                    ViewDrafting newViewDrafting = existingViewDrafting.FirstOrDefault(item => item.Name == currentBFS.Name);

                    if (newViewDrafting == null)
                    {
                        newViewDrafting = document.GetElement(viewTemplate.Duplicate(ViewDuplicateOption.WithDetailing)) as ViewDrafting;
                        newViewDrafting.Name = currentBFS.Name;
                    }
                    newViewDrafting.LookupParameter("ADSK_Группирование").Set("Плагин");

                    //изменить вхождения на правильный чертежный вид
                    var familyIDInstance = newViewDrafting.GetDependentElements(instanceFilter).ToList();
                    foreach ( var familyElementID in familyIDInstance)
                    {
                        //FamilySymbol currentTFS = TargetFamilySymbols.FirstOrDefault(fs => fs.Name == currentBFS.Name)
                        //    ?? (FamilySymbol)TargetFamilySymbols.FirstOrDefault().Duplicate(currentBFS.Name);


                        FamilyInstance familyElement = document.GetElement(familyElementID) as FamilyInstance;
                        FamilySymbol familyElementSymbol = familyElement.Symbol;
                        FamilySymbol existedSymbol = null; //FamiliesSymbolsUtils.GetSymbols(document).Where(sym => sym.Name == currentBFS.Name).Where(sym => sym.Family == familyElementSymbol.Family).FirstOrDefault();
                        var existedSymbolID = familyElementSymbol.Family.GetFamilySymbolIds();
                        foreach ( var symbolID in existedSymbolID)
                        {
                            var symbolIDName = document.GetElement(symbolID).Name;
                            if (symbolIDName == currentBFS.Name)
                            {
                                existedSymbol = document.GetElement(symbolID) as FamilySymbol;
                                break;
                            }
                        }

                        if (existedSymbol == null)
                        {
                            FamilySymbol newFamilyElementSymbol = (FamilySymbol)familyElementSymbol.Duplicate(currentBFS.Name);
                            familyElement.ChangeTypeId(newFamilyElementSymbol.Id);
                        }
                        else
                        {
                            familyElement.ChangeTypeId(existedSymbol.Id);
                        }
                        // TaskDialog.Show("Завершено", $"Создан или найден тип: {currentBFS.Name}");

                        #region назначение параметров
                        for (int j = 0; j < commonStrings.Count; j++)
                        {
                            string lookUpParameterName = commonStrings[j];
                            FamiliesSymbolsUtils.SetParameterValue(currentBFS, familyElementSymbol, lookUpParameterName);
                        }
                        //TaskDialog.Show("Завершено", $"Присвоено параметров: {commonStrings.Count}");
                        #endregion
                    }






                    //разместить чертежный вид на листе
                    ViewSheet list = ViewSheet.Create(document, SelectedTitleBlock.Id);
                    var thisTitleblock = TitleblockUtils.GetInstances(document).Where(inst => inst.OwnerViewId == list.Id).FirstOrDefault();
                        var box = thisTitleblock.get_BoundingBox(list);
                        XYZ pointForCurrentTFS = new XYZ(box.Max.X, box.Max.Y, 0.0);
                        Viewport.Create(document, list.Id, newViewDrafting.Id, pointForCurrentTFS);

                        //резместить легенду на листе
                        if (SelectedView != null)
                        {
                            XYZ pointForLegend = new XYZ(box.Max.X * 0.1, box.Max.Y * 0.1, 0.0);
                            Viewport.Create(document, list.Id, SelectedView.Id, pointForLegend);
                        }

                        if (Designed_By.Length != 0 && definitionDesigned_By != null)
                        {
                            Parameter parameter = newViewDrafting.LookupParameter(parameterName);
                            parameter.Set(Designed_By);

                        }
                    }

                    t.Commit();
                }

                RaiseCloseRequest();
            }
        }
    }

