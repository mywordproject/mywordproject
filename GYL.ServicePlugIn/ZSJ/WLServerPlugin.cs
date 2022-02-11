using GYL.ServicePlugIn.WebReference;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace GYL.ServicePlugIn.ZSJ
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("主数据物料同步到WMS")]
    public class WLServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FNumber", "FName", "FBaseUnitId", "F_TRAX_XG" ,
             "F_TRAX_HSJBQty","FIsKFPeriod","FIsBatchManage","FVOLUME","FNETWEIGHT","FExpPeriod","F_TRAX_HSJBQty"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        /// <summary>
        /// 添加校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            var operValidator = new OperValidator();
            operValidator.AlwaysValidate = true;
            operValidator.EntityKey = "FBillHead";
            e.Validators.Add(operValidator);
        }

        /// <summary>
        /// 操作开始前功能处理
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            foreach (DynamicObject o in e.DataEntitys)
            {
            }
        }

        /// <summary>
        /// 操作结束后功能处理
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var date in e.DataEntitys)
            {
                string rq = date["CreateDate"].ToString();
                JArray INFO = new JArray();
                JObject Dates = new JObject();
                DynamicObjectCollection FentryDate = date["MaterialBase"] as DynamicObjectCollection;
                DynamicObjectCollection MaterialStock = date["MaterialStock"] as DynamicObjectCollection;
                foreach (var Entrydate in FentryDate)
                {
                    Dates.Add("code", date["Number"].ToString());//SKU商品编码
                    Dates.Add("name", date["Name"].ToString());//商品描述
                    Dates.Add("sn", " ");//69码*********
                    Dates.Add("type", 1);//类型****                 
                    Dates.Add("spec", date["F_TRAX_XG"]==null?"":((DynamicObject)date["F_TRAX_XG"])["Name"].ToString());//商品规格
                    Dates.Add("pcs",date["F_TRAX_HSJBQty"].ToString());//箱规*****
                    foreach(var Stock in MaterialStock)
                    {
                        Dates.Add("exp_date", Stock["ExpPeriod"].ToString());//效期天数
                        Dates.Add("trail1", Stock["IsBatchManage"].ToString()=="0"?2:1);//是否跟踪批次1、是；2、否
                        Dates.Add("trail2", Stock["IsKFPeriod"].ToString() == "0" ? 2 : 1);//是否跟踪生产日期1、是；2、否
                        Dates.Add("trail3", Stock["IsKFPeriod"].ToString() == "0" ? 2 : 1);//是否跟踪失效日期1、是；2、否
                    }                   
                    Dates.Add("util", Entrydate["BaseUnitId_Id"].ToString());//基本单位
                    Dates.Add("ie_type", 1);//国产\进口1、是；2、否
                    Dates.Add("volume", Entrydate["VOLUME"].ToString());//体积
                    Dates.Add("weight", Entrydate["NETWEIGHT"].ToString());//重量                                    
                }
                INFO.Add(Dates);
                GatewayService service = new GatewayService();
                service.Url = "http://47.100.33.219:8080/ws/gateway";
                string k="";
                service.executeCompleted += (a, b) =>
                {
                   k=b.Result;
                };
              service.executeAsync("WMSTEST", "AddMaterial", "WMSTEST", rq, "", "", INFO.ToString());

            }
        }


        /// <summary>
        /// 当前操作的校验器
        /// </summary>
        private class OperValidator : AbstractValidator
        {
            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {
                //foreach (var dataEntity in dataEntities)
                //{
                //判断到数据有错误
                //    if()
                //    {
                //        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                //            string.Empty,
                //            dataEntity["Id"].ToString(),
                //            dataEntity.DataEntityIndex,
                //            dataEntity.RowIndex,
                //            dataEntity["Id"].ToString(),
                //            "errMessage",
                //             string.Empty);
                //        validateContext.AddError(null, ValidationErrorInfo);
                //        continue;
                //    }

                //}
            }
        }
    }
}
