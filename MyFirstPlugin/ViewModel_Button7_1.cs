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
    public class ViewModel_Button7_1
    {
        private ExternalCommandData _commandData;
        private Document _doc;

        public DelegateCommand CreateSheetCommand { get; }
        public List<FamilySymbol> TitleBlocks { get; set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedTitleBlock { get; set; }

        public List<View> Views { get; set; } = new List<View>();
        public View SelectedView { get; set; }
        public int NumberOfElements { get; set; }
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

        public ViewModel_Button7_1(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _doc = _commandData.Application.ActiveUIDocument.Document;
            CreateSheetCommand = new DelegateCommand(OnCreateSheetCommand);

            TitleBlocks = TitleblockUtils.GetSymbols(_doc);
            SelectedTitleBlock = TitleBlocks.FirstOrDefault();

            Views = ViewsUtils.GetLegends(_doc);

            NumberOfElements = 1;
            Designed_By = "";


        }             

        private void OnCreateSheetCommand()
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;
            Definition definitionDesigned_By = null;

            if (SelectedTitleBlock == null || NumberOfElements < 1)
            {
                return;
            }

            if (Designed_By.Length != 0)
            {                
                CategorySet newCategorySet = new CategorySet();
                newCategorySet.Insert(Category.GetCategory(document, BuiltInCategory.OST_Sheets));
                definitionDesigned_By = DefinitionsUtils.FindOrCreateDefinition(_commandData, parameterName, newCategorySet);
            }

            using (var t = new Transaction(document, "Создание листов"))
            {
                t.Start();

                for (int i = 0; i < NumberOfElements; i++)
                {
                    ViewSheet list = ViewSheet.Create(document, SelectedTitleBlock.Id);

                    if (SelectedView != null)
                    {
                        var thisTitleblock = TitleblockUtils.GetInstances(_doc).Where(inst => inst.OwnerViewId == list.Id).FirstOrDefault();
                        var box = thisTitleblock.get_BoundingBox(list);

                        XYZ point = (box.Max - box.Min)/2;
                        Viewport.Create(document, list.Id, SelectedView.Id, point);
                    }

                    if (Designed_By.Length != 0 && definitionDesigned_By != null)
                    {                        
                        Parameter parameter = list.LookupParameter(parameterName);
                        parameter.Set(Designed_By);
                    }
                }                              

                t.Commit();
            }

            RaiseShowRequest();
        }
    }
}
