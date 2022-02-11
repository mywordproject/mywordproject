using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Newtonsoft.Json.Linq;

namespace YYLK
{
    public class untils
    {
        public static bool IsLogStep => false;

        public static void LogStep()
        {
            if (IsLogStep)
            {
                System.Diagnostics.StackTrace ss = new System.Diagnostics.StackTrace(true);
                System.Reflection.MethodBase mb = ss.GetFrame(2).GetMethod();
                Log(mb.Name);
            }
        }

        public static void LogStackTrace()
        {
            Log(new System.Diagnostics.StackTrace().ToString());
        }

        public static void Log(params object[] msgArr)
        {
            FileStream fileStream = new FileStream("d:\\logtxt.txt", FileMode.Append);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.Write($@"
[{DateTime.Now:yyyy-MM-dd hh:mm:ss}]
{(string.Join(System.Environment.NewLine, msgArr))}");
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();
        }
    }

    public interface ILog
    {
        void Log(params object[] strArr);
    }

    public static class Ext
    {
        public static bool HasValue<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null) return false;
            if (!enumerable.Any()) return false;
            return true;
        }

        public static bool HasValue(this string str)
        {
            if (str.IsNullOrEmptyOrWhiteSpace()) return false;
            return true;
        }

        //public static bool IsEmpty(this string str)
        //{
        //    return !HasValue(str);
        //}

        public static List<T> GetColumns<T>(this DynamicObjectCollection rows, string columnName)
        {
            var list = new List<T>();

            if (rows == null) return list;

            foreach (var row in rows)
            {
                list.Add(row.GetVal<T>(columnName));
            }

            return list;
        }

        public static List<T> GetFields<T>(this List<DynamicObject> rows, string field)
        {
            var list = new List<T>();

            if (rows == null) return list;

            foreach (var row in rows)
            {
                list.Add(row.GetVal<T>(field));
            }

            return list;
        }

        public static T GetVal<T>(this IDynamicFormModel model, string key, int row, T defValue = default(T))
        {
            object obj1 = model.GetValue(key, row);
            if (obj1 == null)
                return defValue;
            object obj2 = !(model.BillBusinessInfo.GetField(key) is BaseDataField) ? obj1 : ((Kingdee.BOS.Orm.DataEntity.DynamicObject)obj1)["Id"];
            if (obj2 == null)
                return defValue;
            if (typeof(T).IsSubclassOf(typeof(ValueType)) || typeof(T) == typeof(string))
                return (T)Convert.ChangeType(obj2, typeof(T));
            return (T)obj1;
        }

        public static T GetVal<T>(
            this IDynamicFormModel model,
            string key,
            T defValue = default(T))
        {
            return model.DataObject.GetVal(key, defValue);
        }

        public static T GetVal<T>(
            this DynamicObject dynamicObject,
            string propertyName,
            T defValue = default)
        {
            try
            {
                if (dynamicObject == null) return defValue;
                dynamicObject.DynamicObjectType.Properties.TryGetValue(propertyName, out var dynamicProperty);
                if (dynamicProperty == null)
                    return defValue;
                object obj = dynamicProperty.GetValue(dynamicObject);
                if (obj == null)
                    return defValue;
                if (typeof(T).IsSubclassOf(typeof(ValueType)) || typeof(T) == typeof(string))
                    return (T)Convert.ChangeType(obj, typeof(T));
                return (T)obj;
            }
            catch
            {
                return defValue;
            }
        }

        public static void Add(this JObject obj, string key, string innerKey, string value)
        {
            JObject numJObject = new JObject();
            numJObject.Add(innerKey, value);
            obj.Add(key, numJObject);
        }
    }
}
