using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    internal class app : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            #region Разделы2-5
            string ribbonTabName = "Разделы 2-5";

            application.CreateRibbonTab(ribbonTabName);
            RibbonPanel panel = application.CreateRibbonPanel(ribbonTabName, "Раздел 2");
            PushButtonData pushButtonData =
                new PushButtonData("Button1", "Передать параметры", assemblyPath, "MyFirstPlugin.Main1_1");
            panel.AddItem(pushButtonData);
            panel.AddSeparator();
            PushButtonData pushButtonData2 =
                new PushButtonData("Button2", "Кол-во воздуховодов", assemblyPath, "MyFirstPlugin.CountAirChannels");
            panel.AddItem(pushButtonData2);
            panel.AddSeparator();
            PushButtonData pushButtonData3 =
                new PushButtonData("Button3", "Кол-во труб на акт.виде", assemblyPath, "MyFirstPlugin.CountPipelinesOnActiveView");
            panel.AddItem(pushButtonData3);
            panel.AddSeparator();
            PushButtonData pushButtonData4 =
                new PushButtonData("Button4", "Кол-во колонн в модели", assemblyPath, "MyFirstPlugin.CountColumns");
            panel.AddItem(pushButtonData4);
            panel.AddSeparator();
            PushButtonData pushButtonData5 =
                new PushButtonData("Button5", "Кол-во стен по этажам", assemblyPath, "MyFirstPlugin.CountWallsByLevels");
            panel.AddItem(pushButtonData5);
            panel.AddSeparator();

            RibbonPanel panel2 = application.CreateRibbonPanel(ribbonTabName, "Раздел 3");
            PushButtonData pushButtonData2_1 =
                new PushButtonData("Button2_1", "Объем выбранных стен", assemblyPath, "MyFirstPlugin.CountWallsVolume");
            panel2.AddSeparator();
            panel2.AddItem(pushButtonData2_1);
            panel2.AddSeparator();
            PushButtonData pushButtonData2_2 =
                new PushButtonData("Button2_2", "Общая длина труб", assemblyPath, "MyFirstPlugin.CountPipelinesLength");
            panel2.AddItem(pushButtonData2_2);
            panel2.AddSeparator();
            PushButtonData pushButtonData2_3 =
                new PushButtonData("Button2_3", "Запись параметра", assemblyPath, "MyFirstPlugin.SetParameterValue");
            panel2.AddItem(pushButtonData2_3);
            panel2.AddSeparator();
            PushButtonData pushButtonData2_4 =
                new PushButtonData("Button2_4", "Параметр проекта", assemblyPath, "MyFirstPlugin.CreateProjectParameter");
            panel2.AddItem(pushButtonData2_4);
            panel2.AddSeparator();

            RibbonPanel panel3 = application.CreateRibbonPanel(ribbonTabName, "Раздел 4");
            PushButtonData pushButtonData3_1 =
                new PushButtonData("Button3_1", "Вывод выбранных стен", assemblyPath, "MyFirstPlugin.OutputWalls");
            panel3.AddSeparator();
            panel3.AddItem(pushButtonData3_1);
            panel3.AddSeparator();
            PushButtonData pushButtonData3_2 =
                new PushButtonData("Button3_2", "Вывод выбранных труб", assemblyPath, "MyFirstPlugin.OutputPipes");
            panel3.AddItem(pushButtonData3_2);
            panel3.AddSeparator();
            PushButtonData pushButtonData3_3 =
                new PushButtonData("Button3_3", "Вывод всех окон и дверей", assemblyPath, "MyFirstPlugin.OutputApertures");
            panel3.AddItem(pushButtonData3_3);
            panel3.AddSeparator();

            RibbonPanel panel4 = application.CreateRibbonPanel(ribbonTabName, "Раздел 5");
            PushButtonData pushButtonData4_1 =
                new PushButtonData("Button4_1", "Вызов окна с кнопками", assemblyPath, "MyFirstPlugin.Main4_1");
            panel4.AddSeparator();
            panel4.AddItem(pushButtonData4_1);
            panel4.AddSeparator();
            PushButtonData pushButtonData4_2 =
                new PushButtonData("Button4_2", "Вызов окна для изменения типов стен", assemblyPath, "MyFirstPlugin.Main4_2");
            panel4.AddItem(pushButtonData4_2);
            panel4.AddSeparator();
            #endregion
            #region Разделы6-8
            string ribbonTabName2 = "Разделы 6-8";

            application.CreateRibbonTab(ribbonTabName2);

            RibbonPanel panel6 = application.CreateRibbonPanel(ribbonTabName2, "Раздел 6");
            PushButtonData pushButtonData6_1 =
                new PushButtonData("Button6_1", "Создать воздуховод", assemblyPath, "MyFirstPlugin.Main6_1");
            panel6.AddItem(pushButtonData6_1);
            panel6.AddSeparator();
            PushButtonData pushButtonData6_2 =
                new PushButtonData("Button6_2", "Вставить мебель", assemblyPath, "MyFirstPlugin.Main6_2");
            panel6.AddItem(pushButtonData6_2);
            panel6.AddSeparator();
            PushButtonData pushButtonData6_3 =
                new PushButtonData("Button6_3", "Расставить элементы между двумя точками", assemblyPath, "MyFirstPlugin.Main6_3");
            panel6.AddItem(pushButtonData6_3);
            panel6.AddSeparator();

            RibbonPanel panel7 = application.CreateRibbonPanel(ribbonTabName2, "Раздел 7");
            PushButtonData pushButtonData7_1 =
                new PushButtonData("Button7_1", "Создать лист", assemblyPath, "MyFirstPlugin.Main7_1");
            panel7.AddItem(pushButtonData7_1);
            panel7.AddSeparator();

            RibbonPanel panel8 = application.CreateRibbonPanel(ribbonTabName2, "Раздел 8");
            PushButtonData pushButtonData8_1 =
                new PushButtonData("Button8_1", "Экспорт в IFC", assemblyPath, "MyFirstPlugin.ExportToIFC");
            panel8.AddItem(pushButtonData8_1);
            panel8.AddSeparator();
            PushButtonData pushButtonData8_2 =
                new PushButtonData("Button8_2", "Экспорт в NWC", assemblyPath, "MyFirstPlugin.ExportToNWC");
            panel8.AddItem(pushButtonData8_2);
            panel8.AddSeparator();
            PushButtonData pushButtonData8_3 =
                new PushButtonData("Button8_3", "Экспорт в JPEG", assemblyPath, "MyFirstPlugin.ExportToJPEG");
            panel8.AddItem(pushButtonData8_3);
            panel8.AddSeparator();
            #endregion
            #region Практика
            string ribbonTabName3 = "Практика разработки ОП";

            application.CreateRibbonTab(ribbonTabName3);

            RibbonPanel panel3_1 = application.CreateRibbonPanel(ribbonTabName3, "СР");
            PushButtonData pushButtonData3_1_1 =
                new PushButtonData("Buttonpanel3_1_1", "Копировать группу объектов", assemblyPath, "MyFirstPlugin.CopyGroup");
            panel3_1.AddItem(pushButtonData3_1_1);
            panel3_1.AddSeparator();
            PushButtonData pushButtonData3_1_2 =
                new PushButtonData("Buttonpanel3_1_2", "Создать домик", assemblyPath, "MyFirstPlugin.CreateHouse");
            panel3_1.AddItem(pushButtonData3_1_2);
            panel3_1.AddSeparator();

            #endregion


            return Result.Succeeded;
        }
    }
}
