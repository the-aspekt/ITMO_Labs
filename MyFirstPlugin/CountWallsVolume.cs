﻿using Autodesk.Revit.Attributes;
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
    public class CountWallsVolume : IExternalCommand
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
            double volume = walls.Sum(wall => wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble());
            volume = UnitUtils.ConvertFromInternalUnits(volume, UnitTypeId.CubicMeters);

            string finalMessage = $"Объем выбранных стен {volume}";

            TaskDialog.Show("Завершено", finalMessage);
            return Result.Succeeded;
        }
    }
}
