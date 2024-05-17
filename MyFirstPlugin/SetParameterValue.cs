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
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class SetParameterValue : IExternalCommand
    {
        public bool FindOrCreateDefinitionForPipes (string newDefinitionName, UIApplication uIApplication)
        {  
            Document document = uIApplication.ActiveUIDocument.Document;
            Definition definition = null;
            BindingMap bindingMap = document.ParameterBindings;          

            ParameterElement paremeterElement = new FilteredElementCollector(document)
                                        .OfClass(typeof(ParameterElement))
                                        .Cast<ParameterElement>()
                                        .Where(e => e.Name.Equals(newDefinitionName))
                                        .FirstOrDefault();

            if (paremeterElement == null)
            {
                //TaskDialog.Show("Промежуточный итог", "Параметра не существует, создаем новый");

                CategorySet categorySet = new CategorySet();
                categorySet.Insert(Category.GetCategory(document, BuiltInCategory.OST_PipeCurves));
                //Check if shared parameter file was specified
                DefinitionFile definitionFile = uIApplication.Application.OpenSharedParameterFile();
                if (definitionFile == null)
                {
                    TaskDialog.Show("Ошибка", "Не задан файл общих параметров");
                    return false;
                }
                //Find out if definition alreary exist
                DefinitionGroups definitionGroups = definitionFile.Groups;
                definition = definitionGroups.SelectMany(group => group.Definitions)
                    .FirstOrDefault(def => def.Name.Equals(newDefinitionName));
                if (definition == null)
                {
                    //Find out if group name alreary exist
                    string newGroupName = "newGroup";
                    if (definitionGroups.ToList().Find(group => group.Name.Equals(newGroupName)) == null)
                        definitionGroups.Create(newGroupName);
                    //create definition
                    DefinitionGroup currentGroup = definitionGroups.get_Item(newGroupName);
                    currentGroup.Definitions.Create(new ExternalDefinitionCreationOptions(newDefinitionName, ParameterType.Length));
                    definition = currentGroup.Definitions.get_Item(newDefinitionName);
                    TaskDialog.Show("Промежуточный итог", "Создан новый общий параметр в файле общих параметров " + definition.Name);
                }
                //else
                //    TaskDialog.Show("Промежуточный итог", "Нашелся существующий параметр в файле общих параметров " + definition.Name);
                Binding bindings = uIApplication.Application.Create.NewInstanceBinding(categorySet);
                using (Transaction ts = new Transaction(document, "Добавляем в проект параметр " + definition.Name))
                {
                    ts.Start();
                    bindingMap.Insert(definition, bindings, BuiltInParameterGroup.PG_GENERAL);
                    ts.Commit();
                }
            }
            else
            {
                definition = paremeterElement.GetDefinition();
                //TaskDialog.Show("Промежуточный итог", "Нашелся существующий в проекте параметр " + definition.Name);
            }
            return true;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            string parameterName = "Длина с запасом";
            bool isParameterExist = FindOrCreateDefinitionForPipes(parameterName, uIApplication);

            if (!isParameterExist)
            {
                TaskDialog.Show("Завершено", "Программа завершена с ошибкой");
                return Result.Cancelled;
            }

             Selection currentSelection = uIDocument.Selection;

             double length = 0;
             if (currentSelection.GetElementIds().Count < 1)
             {
                 TaskDialog.Show("Первое действие", "Выберите элементы");
                 //WallsSelectionFilter wsf = new WallsSelectionFilter();
                 List<Reference> pickedElement;
                 try
                 {
                     pickedElement = currentSelection.PickObjects(ObjectType.Element, "Выберите элементы").ToList();
                 }
                 catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                 {
                     return Result.Cancelled;
                 }
                using (Transaction t = new Transaction(document))
                {
                    t.Start($"Корректировка параметров труб");
                    int counter = 0;
                    foreach (Reference element in pickedElement)
                    {
                        Pipe pipe = document.GetElement(element) as Pipe;
                        if (pipe != null)
                        {
                            length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                            Parameter parameter = pipe.LookupParameter(parameterName);
                            parameter.Set(length*1.1);
                            counter++;
                        }
                    }
                    TaskDialog.Show("Завершено", $"Обработано труб: {counter}");
                    t.Commit();
                }
             }
             else
             {
                 var currentSelectionElementIDs = currentSelection.GetElementIds();
                 var selectedPipes = new FilteredElementCollector(document, currentSelectionElementIDs)
                     .WhereElementIsNotElementType()
                     .OfClass(typeof(Pipe))
                     .Cast<Pipe>()
                     .ToList();
                using (Transaction t = new Transaction(document))
                {
                    t.Start($"Корректировка параметров труб");
                    int counter = 0;
                    foreach (Pipe pipe in selectedPipes)
                    {
                            length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                            Parameter parameter = pipe.LookupParameter(parameterName);
                            parameter.Set(length * 1.1);
                            counter++;                        
                    }
                    TaskDialog.Show("Завершено", $"Обработано труб: {counter}");
                    t.Commit();
                }
            }

             if (length == 0)
                 TaskDialog.Show("Завершено", "Не выбрано ни одной трубы");
            
            return Result.Succeeded;
        }
    }
}
