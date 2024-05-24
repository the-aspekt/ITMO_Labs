using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
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
    public class CountPipelinesLength : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            Selection currentSelection = uIDocument.Selection;
            List<Pipe> pipes = new List<Pipe>();
            if (currentSelection.GetElementIds().Count < 1)
            {
                TaskDialog.Show("Первое действие", "Выберите элементы");
                List<Reference> pickedElement;
                try
                {
                    pickedElement = currentSelection.PickObjects(ObjectType.Element, "Выберите элементы").ToList();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
                foreach (Reference element in pickedElement)
                {
                    Element thisElement = document.GetElement(element);
                    if (thisElement.GetType() == typeof(Pipe))
                    {
                        Pipe pipe = document.GetElement(element) as Pipe;
                        pipes.Add(pipe);
                    }
                }         
            }
            else
            {
                var currentSelectionElementIDs = currentSelection.GetElementIds();
                pipes = new FilteredElementCollector(document, currentSelectionElementIDs)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(Pipe))
                    .Cast<Pipe>()
                    .ToList();                
            }

            double length = pipes.Sum(pipe => pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble());
            if (length == 0)            
                TaskDialog.Show("Завершено", "Не выбрано ни одной трубы");            
            else
            {
                length = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters);
                TaskDialog.Show("Завершено", $"Длина выбранных труб {length}");
            }
            return Result.Succeeded;
        }
    }
}
