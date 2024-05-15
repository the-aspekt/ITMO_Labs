using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CountWallsByLevels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            var allWalls = new FilteredElementCollector(document)
               .OfClass(typeof(Wall))
               .Cast<Wall>()
               .ToList();

            var allLevels = new FilteredElementCollector(document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            string finalMessage = "";

            foreach (Level level in allLevels)
            {
                int levelWallsCount = allWalls
                .Where(wall => wall.LevelId == level.Id)
                .ToList()
                .Count;
                if (levelWallsCount > 0)
                    finalMessage += $"Количество стен этажа {level.Name}: {levelWallsCount} {Environment.NewLine}";
            }            

            TaskDialog.Show("Завершено", finalMessage);
            return Result.Succeeded;
        }
    }
}
