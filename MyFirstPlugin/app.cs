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
            string ribbonTabName = "SZN_Plugins";
            application.CreateRibbonTab(ribbonTabName);
            RibbonPanel panel = application.CreateRibbonPanel(ribbonTabName, "Панель инструментов");
            PushButtonData pushButtonData =
                new PushButtonData("Button1", "Передать параметры",assemblyPath, "MyFirstPlugin.MyFirstCommand");
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

            return Result.Succeeded;
        }
    }
}
