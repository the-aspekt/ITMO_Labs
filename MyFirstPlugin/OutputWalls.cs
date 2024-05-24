using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class OutputWalls : IExternalCommand
    {
       

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            Selection currentSelection = uIDocument.Selection;
            List<Wall> walls = new List<Wall>();

            if (currentSelection.GetElementIds().Count < 1)
            {
                TaskDialog.Show("Первое действие", "Выберите стены");
                WallsSelectionFilter wsf = new WallsSelectionFilter();
                List<Reference> pickedElement;
                
                try
                {
                    pickedElement = currentSelection.PickObjects(ObjectType.Element, wsf, "Выберите стены").ToList();
                }      
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                foreach (Reference element in pickedElement)
                {
                    Wall wall = document.GetElement(element) as Wall;
                    walls.Add(wall);
                }
            }
            else
            {
                var currentSelectionElementIDs = currentSelection.GetElementIds();
                walls = new FilteredElementCollector(document, currentSelectionElementIDs)   
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .ToList();
            }
            
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentDate = DateTime.Now.ToString("HH-mm");
            string filename = "walls" + currentDate + ".csv";

            string csvPath = Path.Combine(desktopPath, filename);
            string resultedText = "";

            foreach (Wall wall in walls)
            {
                string wallType = wall.Name;
                double wallVolume = wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble();
                wallVolume = Math.Round(UnitUtils.ConvertFromInternalUnits(wallVolume, UnitTypeId.CubicMeters),2);
                resultedText += $"{wallType};{wallVolume} {Environment.NewLine}";
            }

            File.WriteAllText(csvPath, resultedText);
            string finalMessage = $"Записано {walls.Count} стен в файл {filename}";

            TaskDialog.Show("Завершено", finalMessage);
            return Result.Succeeded;
        }
    }
}
