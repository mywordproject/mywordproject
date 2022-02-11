using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.SCM.Core.SPM;
using Kingdee.K3.SCM.ServiceHelper;
using Kingdee.K3.SCM.SPM.Business.Plugin;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [HotUpdate]
    [Description("促销匹配-表单插件")]
    public class SubSPMPromotionMatchEdit : SPMPromotionMatchEdit
    {
        public Dictionary<string, decimal> Dic { get; set; }

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);

            object customParameter = e.Paramter.GetCustomParameter("conditionDatas");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(customParameter);
            var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PromotionPolicyMatchData>>(json);

            var list = SPMServiceHelper.PromotionMatch(base.Context, conditions);
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            DynamicObjectCollection collection = this.View.Model.GetVal<DynamicObjectCollection>("Entity");

            List<DynamicObject> dynamicObjectList = collection.Where(d => d.GetVal<string>("FPresentType") == "1").ToList();

            Dic = dynamicObjectList.GroupBy(d => d.GetVal<string>("FPromotionPolicyID")).ToDictionary(g => g.Key, g => g.ToList().Sum(d => d.GetVal<decimal>("FPresentQty")));
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (e.Key.ToUpperInvariant() == "FBTNOK")
            {
                DynamicObjectCollection collection = this.View.Model.GetVal<DynamicObjectCollection>("Entity");

                List<DynamicObject> dynamicObjectList = collection.Where(d => d.GetVal<string>("FPresentType") == "1").ToList();

                Dictionary<string, string> nameDictionary = new Dictionary<string, string>();
                foreach (DynamicObject dynamicObject in dynamicObjectList)
                {
                    string FPromotionPolicyID = dynamicObject.GetVal<string>("FPromotionPolicyID");
                    string FPromotionProject = dynamicObject.GetVal<string>("FPromotionProject");

                    nameDictionary[FPromotionPolicyID] = FPromotionProject;
                }

                var resultDic = dynamicObjectList.GroupBy(d => d.GetVal<string>("FPromotionPolicyID")).ToDictionary(g => g.Key, g => g.ToList().Sum(d => d.GetVal<decimal>("FPresentQty")));

                foreach (KeyValuePair<string, decimal> keyValuePair in resultDic)
                {
                    string key = keyValuePair.Key;
                    decimal value = keyValuePair.Value;

                    if (value > Dic[key])
                    {
                        e.Cancel = true;
                        this.View.ShowErrMessage($"促销政策{nameDictionary[key]}：赠品合计数量不能超过{Dic[key]}");
                        break;
                    }
                }
            }

            if (e.Cancel == false)
            {
                base.ButtonClick(e);
            }
        }
    }
}
