using GYL.ServicePlugIn.WebReference;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace GYL.ServicePlugIn.CGYW
{
     [Kingdee.BOS.Util.HotUpdate]
    [Description("采购订单同步到WMS")]
    public class CGDDServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FBillTypeID","FBillNo", "FPurchaseOrgId","F_TRAX_Combo",
                "FDate","FMaterialId","FEntryNote","FDeliveryDate","F_TRAX_Qty",
                "FNote", "FBaseUnitQty","FBaseUnitQty"};
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
                string rq = date["Date"].ToString();
                if (((DynamicObject)date["BillTypeID"])["Number"].ToString() == "CGDD01_SYS")
                {
                    //判断是否传递
                    if (Int32.Parse(date["F_TRAX_Combo"].ToString()) == 1)
                    {
                        JObject Dates = new JObject();
                        Dates.Add("billid", date["Id"].ToString());//id
                        Dates.Add("billtypeid", ((DynamicObject)date["BillTypeId"])["Name"].ToString());//业务类型
                        Dates.Add("companycode", date["PurchaseOrgId"] == null ? "" : ((DynamicObject)date["PurchaseOrgId"])["Number"].ToString());//公司*********
                        Dates.Add("erp_no", date["BillNo"].ToString());//单据编号   
                        Dates.Add("billldate", date["Date"].ToString());//单据日期                   
                        Dates.Add("supplyid", date["SupplierId"] == null ? "" : ((DynamicObject)date["SupplierId"])["Number"].ToString());//往来户
                        Dates.Add("storagelocation", "6001");//送出存储地点                                 
                        Dates.Add("note", "");//备注******
                        JArray info = new JArray();
                        DynamicObjectCollection FentryDate = date["POOrderEntry"] as DynamicObjectCollection;
                        foreach (var Entrydate in FentryDate)
                        {
                            JObject entry = new JObject();
                            entry.Add("billdtlid", Entrydate["Id"].ToString());//明细ID
                            entry.Add("skucode", "0020001");//SKU(Entrydate["MaterialID"] as DynamicObject)["Number"].ToString()
                            entry.Add("materialtype", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品
                            entry.Add("basequantity", Entrydate["BaseUnitQty"].ToString());//基本数量*********
                            entry.Add("quantity", Entrydate["F_TRAX_Qty"].ToString());//箱数*********
                            entry.Add("deliveryDate", Entrydate["DeliveryDate"].ToString());//预计到货日期                      
                            entry.Add("notedtl", Entrydate["Note"].ToString());//备注
                            info.Add(entry);
                        }
                        Dates.Add("grid", info);
                        GatewayService service = new GatewayService();
                        service.Url = "http://47.100.33.219:8080/ws/gateway";
                        string k = "";
                        service.executeCompleted += (a, b) =>
                        {
                            k = b.Result;
                        };
                        service.executeAsync("WMSTEST", "AddNoticeIn", "WMSTEST", rq, "", "", Dates.ToString());
                    }
                }
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
