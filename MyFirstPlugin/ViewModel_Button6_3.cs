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

namespace MyFirstPlugin
{
    public class ViewModel_Button6_3
    {
        private ExternalCommandData _commandData;

        public DelegateCommand CreateFurnitureCommand { get; }
        public List<FamilySymbol> FurnitureTypes { get; set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedFurnitureType { get; set; }
        public List<Level> Levels { get; set; } = new List<Level>();
        public Level SelectedLevel { get; set; }
        public int NumberOfElements { get; set; }
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

        public ViewModel_Button6_3(ExternalCommandData commandData)
        {
            _commandData = commandData;
            CreateFurnitureCommand = new DelegateCommand(OnCreateFurnitureCommand);
            FurnitureTypes = FamilySymbolUtils.GetSymbols(_commandData).Where(s => s.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased).ToList();
            Levels = LevelsUtils.GetLevels(_commandData);
            NumberOfElements = 1;

            Points = SelectionUtils.Get2Points(_commandData, ObjectSnapTypes.Endpoints);
        }             

        private void OnCreateFurnitureCommand()
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;
            
            if (SelectedFurnitureType == null || SelectedLevel == null || NumberOfElements < 1)
            {
                return;
            }
            using (var t = new Transaction(document, "Создание последовательности элементов"))
            {
                t.Start();

                //Line newLine = Line.CreateBound(Points[0], Points[1]);
                for (int i = 1; i <= NumberOfElements; i++)
                {
                    //XYZ point = newLine.Evaluate(i / (NumberOfElements + 1), true);
                    XYZ point = Points[0] + (Points[1]- Points[0]) * i / (NumberOfElements + 1);
                    FamilyInstanceUtils.CreateFamilyInstanceWithoutTransaction(_commandData, SelectedFurnitureType, point, SelectedLevel);
                }
                t.Commit();
            }

            RaiseShowRequest();
        }
    }
}
