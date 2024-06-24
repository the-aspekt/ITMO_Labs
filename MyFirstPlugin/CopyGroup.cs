using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitAPITrainingLibrary;
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
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {               
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;
                Document document = uIDocument.Document;
              
                GroupSelectionFilter gsf = new GroupSelectionFilter();
                var reference = uIDocument.Selection.PickObject(ObjectType.Element, gsf, "Выберите группу");
                Group group = document.GetElement(reference) as Group;
                XYZ groupCenter = FamiliesInstancesUtils.GetElementCenter(group);

                Room initialRoom = RoomUtils.GetRoomByPoint(document, groupCenter);
                XYZ initialRoomCenter = FamiliesInstancesUtils.GetElementCenter(initialRoom);
                XYZ offset = initialRoomCenter - groupCenter;

                XYZ roomPoint = uIDocument.Selection.PickPoint("Выберите точку вставки");
                Room targetRoom = RoomUtils.GetRoomByPoint(document, roomPoint);
                XYZ targetRoomCenter = FamiliesInstancesUtils.GetElementCenter(targetRoom);
                XYZ targetPoint = targetRoomCenter - offset;


                using (var t = new Transaction(document, "Копирование группы"))
                {
                    t.Start();
                    document.Create.PlaceGroup(targetPoint, group.GroupType);
                    t.Commit();
                }

                return Result.Succeeded;
                
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

        }
    }
}
