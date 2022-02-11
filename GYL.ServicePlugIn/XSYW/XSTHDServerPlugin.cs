using GYL.ServicePlugIn.WebReference;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace GYL.ServicePlugIn.XSYW
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("销售退货通知单同步到WMS")]
    public class XSTHDServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FBillTypeID", "FBillNo","FDate","FSaleOrgId" ,"FRetcustId","FRetorgId", "FDescription", "F_TRAX_Combo",
               "FLinkMan", "FLinkPhone","FMaterialId",
                "FEntryDescription","FBaseUnitQty","FDeliverydate"};
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
                //判断是否传递
                if (Int32.Parse(date["F_TRAX_Combo"].ToString()) == 1)
                {
                    JObject Dates = new JObject();
                    Dates.Add("billid", date["Id"].ToString());//id
                    Dates.Add("billtypeid", date["BillTypeID"] == null ? "" : ((DynamicObject)date["BillTypeID"])["Number"].ToString());//单据类型
                    Dates.Add("companycode", date["RetorgId"]==null?"": ((DynamicObject)date["RetorgId"])["Number"].ToString());//公司
                    Dates.Add("b_no", date["BillNo"].ToString());//单据编号
                    Dates.Add("isallocate", ""); //是否有转储单*********
                    Dates.Add("billldate", date["Date"].ToString());//单据日期
                    string wlhname = date["RetcustId"] == null ? "" : (date["RetcustId"] as DynamicObject)["Name"].ToString();
                    Dates.Add("supplyid", wlhname);//往来户
                    Dates.Add("storagelocation", "6001");//送出存储地点 date["RetorgId"] == null ? "" : ((DynamicObject)date["RetorgId"])["Number"].ToString()
                    Dates.Add("deliveryaddress", "");//客户收货地址******
                    Dates.Add("deliverystorage", "");//客户收货仓库******
                    Dates.Add("deliverycon",date["FLinkMan"].ToString());//送货联系人******
                    Dates.Add("deliverytel", date["FLinkPhone"].ToString());//送货联系电话******                               
                    Dates.Add("note", "");//备注
                    JArray info = new JArray();
                    DynamicObjectCollection FentryDate = date["SAL_RETURNNOTICEENTRY"] as DynamicObjectCollection;
                    foreach (var Entrydate in FentryDate)
                    {
                        JObject entry = new JObject();
                        entry.Add("billdtlid", Entrydate["Id"].ToString());//明细ID*********
                        entry.Add("skucode", "0020001");//SKU Entrydate["MaterialId"] == null ? "" : ((DynamicObject)Entrydate["MaterialId"])["Number"].ToString()
                        entry.Add("materialtype","SP");//商品属性必填 SP表示商品；YP表示样品；ZP表示赠品
                        entry.Add("basequantity", Entrydate["BaseUnitQty"].ToString());//基本数量*********
                        entry.Add("quantity", "");//箱数*********
                        entry.Add("customerunit", "");//客户计量单位名称
                        entry.Add("unitratio", "");//单位转换比率
                        entry.Add("customerordernumber", "");//客户订单号
                        entry.Add("allocateno", "");//转储单号
                        entry.Add("deliveryDate", Entrydate["Deliverydate"].ToString());//预计到货日期
                        entry.Add("notedtl",Entrydate["Description"].ToString());//备注
                        info.Add(entry);
                    }
                    Dates.Add("grid", info);
                    GatewayService service = new GatewayService();
                    service.Url = "http://47.100.33.219:8080/ws/gateway";
                    string str = "";
                    service.executeCompleted += (a, b) =>
                    {
                        str = b.Result;
                    };
                    service.executeAsync("WMSTEST", "AddNoticeOutReturn", "WMSTEST", rq, "", "", Dates.ToString());
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
