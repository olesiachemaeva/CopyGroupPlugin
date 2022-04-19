using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                GroupPickFillter groupPickFillter = new GroupPickFillter();
                //попросить пользователя выбрать группу для копирования
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFillter, "Выберите группу объектов"); // получили ссылку на выбранную пользователем группу объектов
                Element element = doc.GetElement(reference);
                Group group = element as Group; // тот объект по кт пользователь щелкнул получили и преобразовали к типу Group
                XYZ goupCenter = GetElementCenter(group);
                Room room = GetRoomByOption(doc, goupCenter);
                XYZ roomCenter = GetElementCenter(room);
                XYZ offset = goupCenter - roomCenter;


                //попросим пользователя выбрать какую то точку 
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(point, group.GroupType);
                transaction.Commit();
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch(Exception ex)
            {
                message = ex.Message;  //из-за чего произошла ошибка - текст исключения
                return Result.Failed;
            }


            return Result.Succeeded;

        }
        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null); // рамка в 3х измерениях
            return (bounding.Max + bounding.Min) / 2;
        }
        public Room GetRoomByOption(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach(Element e in collector)
            {
                Room room = e as Room;
                if (room! = null)
                {
                    if(room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }
    public class GroupPickFillter : ISelectionFilter //фильтр
    {
        public bool AllowElement(Element elem) 
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    
}
