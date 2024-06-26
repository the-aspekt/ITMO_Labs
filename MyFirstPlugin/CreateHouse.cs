using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NPOI.HSSF.Record.AutoFilter;
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
    public class CreateHouse : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;
                Document document = uIDocument.Document;

                List<Level> levels = LevelsUtils.GetLevels(document);
                Level zeroLevel = levels.Where(x => x.Elevation == 0).FirstOrDefault();
                Level nextLevel = levels.Where(x => x.Elevation > 0).FirstOrDefault();

                double length = UnitUtils.ConvertToInternalUnits(6000, UnitTypeId.Millimeters);
                double width = UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters);

                List<XYZ> points = new List<XYZ>();
                points.Add(new XYZ(0, 0, 0));
                points.Add(new XYZ(0, width, 0));
                points.Add(new XYZ(length, width, 0));
                points.Add(new XYZ(length, 0, 0));
                points.Add(new XYZ(0, 0, 0));

                List<Wall> walls = new List<Wall>();

                using (var t = new Transaction(document, "Создание домика"))
                {
                    t.Start();
                    CreateWalls(document, points, zeroLevel, nextLevel, ref walls);
                    AddDoor(document, walls);
                    AddWindows(document, walls);
                    AddExtrusionRoof(document, walls);
                    t.Commit();
                }

                return Result.Succeeded;

            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

        }

        private void AddExtrusionRoof(Document document, List<Wall> walls, double elevation = 1000, double eave = 500)
        {
            var roofType = RoofsUtils.GetTypes(document).Where(x=>x.Name.Contains("Тип")).FirstOrDefault();

            Wall wall = walls.FirstOrDefault();
            var levelId = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId();
            Level level = document.GetElement(levelId) as Level;

            CurveArray curveArray = new CurveArray();
            XYZ elevationCoordinate = new XYZ(0, 0, UnitUtils.ConvertToInternalUnits(elevation, UnitTypeId.Millimeters));

            XYZ p1 = WallsUtils.GetPointOnTopOfWall(wall, 0)- new XYZ(0, wall.Width / 2, 0);
            XYZ p2 = WallsUtils.GetPointOnTopOfWall(wall, 0.5)+ elevationCoordinate;
            XYZ p3 = WallsUtils.GetPointOnTopOfWall(wall, 1) + new XYZ(0, wall.Width / 2,  0);
            XYZ p4 = WallsUtils.GetPointOnBaseOfWall(wall);

            //var orientation = wall.Orientation.Normalize();

            curveArray.Append(Line.CreateBound(p1,p2));
            curveArray.Append(Line.CreateBound(p2,p3));

            ReferencePlane plane = document.Create.NewReferencePlane(p3, p1, new XYZ(0, 0, p2.Z), document.ActiveView); // не работает в этом месте
            /*
            XYZ extrusionEndPoint = new XYZ(); 
            
            foreach (Wall i in walls)
            {
                if (i.Orientation.Normalize().AngleTo(orientation) == 0)
                {
                    extrusionEndPoint = WallsUtils.GetPointOnBaseOfWall(i);
                }
            }
            */
            double extrusionStart = UnitUtils.ConvertToInternalUnits(-eave, UnitTypeId.Millimeters);

            double extrusionEnd = UnitUtils.ConvertToInternalUnits(6000 + eave, UnitTypeId.Millimeters);

            document.Create.NewExtrusionRoof(curveArray, plane, level, roofType, extrusionStart, extrusionEnd);
        }

        private void AddWindows(Document document, List<Wall> walls)
        {
            var windowTypes = WindowsUtils.GetSymbols(document);
            FamilySymbol windowType = windowTypes
                .Where(x => UnitUtils.ConvertFromInternalUnits(x.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble(), UnitTypeId.Millimeters) > 800
                && UnitUtils.ConvertFromInternalUnits(x.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble(), UnitTypeId.Millimeters) < 1200
                && UnitUtils.ConvertFromInternalUnits(x.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble(), UnitTypeId.Millimeters) > 1200)
                .FirstOrDefault();
            if (!windowType.IsActive)
                windowType.Activate();

            for (int i = 0; i< walls.Count; i++)
            {
                Wall wall = walls[i];
                double coordinate = i != 0 ? 0.5 : 0.25;
                XYZ targetPoint = WallsUtils.GetPointOnBaseOfWall(wall, coordinate);
                Level level = document.GetElement(wall.LevelId) as Level;
                var window = document.Create.NewFamilyInstance(targetPoint, windowType, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters));
            }
        }

        private void AddDoor(Document document, List<Wall> walls)
        {
            var doorTypes = DoorsUtils.GetSymbols(document);
            FamilySymbol doorType = doorTypes
                .Where(x => UnitUtils.ConvertFromInternalUnits(x.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters) > 800
                && UnitUtils.ConvertFromInternalUnits(x.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters) < 1200)
                .FirstOrDefault();

            if (!doorType.IsActive)
                doorType.Activate();

            Wall wall = walls.FirstOrDefault();
            XYZ targetPoint = WallsUtils.GetPointOnBaseOfWall(wall, 0.75);
            Level level = document.GetElement(wall.LevelId) as Level;

            document.Create.NewFamilyInstance(targetPoint, doorType, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }

        private void CreateWalls(Document document, List<XYZ> points, Level zeroLevel, Level nextLevel, ref List<Wall> walls)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(document, line, zeroLevel.Id, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(nextLevel.Id);
                walls.Add(wall);
            }
        }
    }
}
