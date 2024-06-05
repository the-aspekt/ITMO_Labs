using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Mechanical;
using RevitAPITrainingLibrary;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace MyFirstPlugin
{
    public class ViewModel_Button1_1
    {
        private ExternalCommandData _commandData;
        private Document _doc;

        public DelegateCommand CreateSheetCommand { get; }
        public List<Family> BasicFamily { get; set; } = new List<Family>();
        public Family SelectedBasicFamily { get; set; }
        public List<Family> TargetFamily { get; set; } = new List<Family>();
        public Family SelectedTargetFamily { get; set; }
        public List<FamilySymbol> TitleBlocks { get; set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedTitleBlock { get; set; }

        public List<View> Views { get; set; } = new List<View>();
        public View SelectedView { get; set; }

        private string parameterName = "Designed_By";
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
            CreateSheetCommand = new DelegateCommand(OnCreateSheetCommand);

            TitleBlocks = TitleblockUtils.GetSymbols(_doc);
            SelectedTitleBlock = TitleBlocks.FirstOrDefault();
            BasicFamily = FamiliesUtils.GetFamilies(_doc);
            SelectedBasicFamily = BasicFamily.FirstOrDefault(f => f.Name.Contains("Плита")
                                    && f.FamilyCategory != Category.GetCategory(_doc, BuiltInCategory.OST_DetailComponents));
            TargetFamily = FamiliesUtils.GetFamilies(_doc);
            SelectedTargetFamily = TargetFamily.FirstOrDefault(f => f.Name.Contains("Плита")
                                    && f.FamilyCategory.Id == Category.GetCategory(_doc, BuiltInCategory.OST_DetailComponents).Id);
            Designed_By = "";
            Views = ViewsUtils.GetLegends(_doc);

        }

        private void OnCreateSheetCommand()
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

            TaskDialog.Show("Внимание", $"Будет создано: 1 листов"); //{BasicFamilySymbols.Count}

            using (var t = new Transaction(document, "Создание листов"))
            {
                t.Start();

                for (int i = 0; i < 1; i++) //BasicFamilySymbols.Count
                {
                    ViewSheet list = ViewSheet.Create(document, SelectedTitleBlock.Id);
                    FamilySymbol currentBFS = BasicFamilySymbols[i];
                    FamilySymbol currentTFS = TargetFamilySymbols.FirstOrDefault(fs => fs.Name == currentBFS.Name)
                        ?? (FamilySymbol)TargetFamilySymbols.FirstOrDefault().Duplicate(currentBFS.Name);

                   // TaskDialog.Show("Завершено", $"Создан или найден тип: {currentBFS.Name}");

                    for (int j = 0; j < commonStrings.Count; j++)
                    {
                        string lookUpParameterName = commonStrings[j];
                        FamiliesSymbolsUtils.SetParameterValue(currentBFS, currentTFS, lookUpParameterName);
                    }
                    //TaskDialog.Show("Завершено", $"Присвоено параметров: {commonStrings.Count}");

                    //получить базовый чертежный вид
                    var viewDraftings = ViewsUtils.GetDraftingViews(document);
                    ViewDrafting viewTemplate = viewDraftings.Where(v => v.Name == "шаблон оформления плиты перекрытия").FirstOrDefault();                                      

                    if (viewTemplate == null)
                    {
                        TaskDialog.Show("Ошибка", $"Отсутствует шаблон оформления плиты перекрытия!");
                        return;
                    }
                                                       
                    ViewFamilyType familyType = ViewsUtils.GetViewFamilyTypes(document).Where(p => p.ViewFamily == ViewFamily.Drafting).FirstOrDefault();
                    ElementClassFilter instanceFilter = new ElementClassFilter(typeof(FamilyInstance));
                    ViewDrafting newViewDrafting = null; //можно переписать в ЛИНК формате

                    foreach (ViewDrafting item in existingViewDrafting)
                    {
                        if (item.Name == currentBFS.Name)
                        {
                            newViewDrafting = item;
                            break;
                        }
                    }
                    
                    if (newViewDrafting == null)
                    {
                        newViewDrafting = document.GetElement(viewTemplate.Duplicate(ViewDuplicateOption.WithDetailing)) as ViewDrafting;
                        newViewDrafting.Name = currentBFS.Name;
                    }

                    //Блок создания элементов по экземплярам
                    //ViewDrafting newViewDrafting = ViewDrafting.Create(document, familyType.Id);
                    //newViewDrafting.Name = currentBFS.Name;
                    // TaskDialog.Show("Завершено", $"Создан  чертежный вид : {newViewDrafting.Name}");

                    // var default2DFamilyViewID = viewTemplate.GetDependentElements(instanceFilter);
                    // ElementTransformUtils.CopyElements(viewTemplate, default2DFamilyViewID, newViewDrafting, null, null);                    
                    //// TaskDialog.Show("Завершено", $"Скопированы экземпляры в количестве: {default2DFamilyViewID.Count}");

                    // ElementClassFilter textNoteFilter = new ElementClassFilter(typeof(TextNote));
                    // default2DFamilyViewID = viewTemplate.GetDependentElements(textNoteFilter);
                    // ElementTransformUtils.CopyElements(viewTemplate, default2DFamilyViewID, newViewDrafting, null, null);
                    //// TaskDialog.Show("Завершено", $"Скопированы текстовые примечания в количестве: {default2DFamilyViewID.Count}");

                    // ElementClassFilter dimensionsFilter = new ElementClassFilter(typeof(Dimension));
                    // default2DFamilyViewID = viewTemplate.GetDependentElements(dimensionsFilter);
                    // ElementTransformUtils.CopyElements(viewTemplate, default2DFamilyViewID, newViewDrafting, null, null);
                    //// TaskDialog.Show("Завершено", $"Скопированы размеры в количестве: {default2DFamilyViewID.Count}");

                    //изменить вхождение на правильный чертежный вид
                    var familyViewIDInstance = newViewDrafting.GetDependentElements(instanceFilter).FirstOrDefault();
                        FamilyInstance default2DFamilyView = document.GetElement(familyViewIDInstance) as FamilyInstance;
                        default2DFamilyView.ChangeTypeId(currentTFS.Id);


                        //разместить чертежный вид на листе
                        var thisTitleblock = TitleblockUtils.GetInstances(document).Where(inst => inst.OwnerViewId == list.Id).FirstOrDefault();
                        var box = thisTitleblock.get_BoundingBox(list);
                        XYZ pointForCurrentTFS = new XYZ(0.0, box.Max.Y, 0.0);
                        Viewport.Create(document, list.Id, newViewDrafting.Id, pointForCurrentTFS);

                        //резместить легенду на листе
                        if (SelectedView != null)
                        {
                            XYZ pointForLegend = new XYZ(box.Max.X * 0.1, box.Max.Y * 0.1, 0.0);
                            Viewport.Create(document, list.Id, SelectedView.Id, pointForLegend);
                        }

                        if (Designed_By.Length != 0 && definitionDesigned_By != null)
                        {
                            Parameter parameter = list.LookupParameter(parameterName);
                            parameter.Set(Designed_By);
                        }
                    }

                    t.Commit();
                }

                RaiseCloseRequest();
            }
        }
    }

