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
    [Description("退货采购单同步到WMS")]
    public class CGTHDServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FBillNo", "F_TRAX_Combo","FBUSINESSTYPE","BussinessType" ,"FOWNERID","FDeliveryDate",
                "FDate", "FPurchaseOrgId", "FSupplierID", "FDESCRIPTION","FAcceptAddress","FProduceDate",
                "FMATERIALID", "FEntryNote", "FLot", "FBaseUnitQty","F_TRAX_Qty"};
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
              if( ((DynamicObject)date["BillTypeID"])["Number"].ToString()== "CGDD09_SYS")
                {
                    //判断是否传递
                    if (Int32.Parse(date["F_TRAX_Combo"].ToString()) == 1)
                    {
                        JObject Dates = new JObject();
                        Dates.Add("billid" ,date["Id"].ToString());//id
                        Dates.Add("billtypeid", date["BillTypeID"] == null ? "" : ((DynamicObject)date["BillTypeID"])["Name"].ToString());//业务类型
                        Dates.Add("companycode", date["PURCHASEORGID"] == null ? "" : ((DynamicObject)date["PURCHASEORGID"])["Number"].ToString());//公司
                        Dates.Add("b_no", date["BillNo"].ToString());//单据编号
                                                                     //Dates.Add("isallocate", ""); //是否有转储单
                        Dates.Add("billldate", date["Date"].ToString());//单据日期
                        string wlhname = date["SUPPLIERID"] == null ? "" : (date["SUPPLIERID"] as DynamicObject)["Name"].ToString();
                        Dates.Add("supplyid", wlhname);//往来户
                        Dates.Add("storagelocation", "6001");//送出存储地点 ((DynamicObject)date["PURCHASEORGID"])["Number"].ToString()
                        //Dates.Add("deliveryaddress", date["AcceptAddress"].ToString());//客户收货地址******
                        //Dates.Add("deliverystorage", "");//客户收货仓库******
                        //Dates.Add("deliverycon", "");//送货联系人******
                        //Dates.Add("deliverytel", "");//送货联系电话******
                        Dates.Add("note", "");//备注
                        JArray info = new JArray();
                        DynamicObjectCollection FentryDate = date["POOrderEntry"] as DynamicObjectCollection;
                        foreach (var Entrydate in FentryDate)
                        {
                            JObject entry = new JObject();
                            entry.Add("billdtlid", Entrydate["Id"].ToString());//明细ID
                            entry.Add("skucode", "0020001");//SKUEntry date["MaterialID"] == null ? "" : (Entrydate["MaterialID"] as DynamicObject)["Number"].ToString()
                            //entry.Add("BatchCode", Entrydate["Lot"]==null?"":((DynamicObject)Entrydate["Lot"])["Number"].ToString());//批次号
                            //entry.Add("MakeDate",Entrydate["ProduceDate"].ToString());//生产日期
                            entry.Add("basequantity", Entrydate["BaseUnitQty"].ToString());//基本数量*********
                            entry.Add("quantity", Entrydate["F_TRAX_Qty"].ToString());//箱数*********
                            //entry.Add("store", Entrydate["STOCKID"] == null ? "" : ((DynamicObject)Entrydate["STOCKID"])["Number"].ToString());//仓库
                            //entry.Add("client", Entrydate["OWNERID"] == null ? "" : ((DynamicObject)Entrydate["OWNERID"])["Number"].ToString());//货主
                            //entry.Add("zone", "");//库区 （正常品区、退货区..)
                            //entry.Add("type", "");//状态 （商品、赠品、样品..）
                            //entry.Add("preScanned", "");//是否预扫码,N表示否，Y表示是
                            //entry.Add("MaterialType", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品           
                            //entry.Add("preScanCust", "");//预扫码客户编号
                            entry.Add("deliverydate", Entrydate["DeliveryDate"].ToString());//预计到货日期
                            entry.Add("notedtl", Entrydate["Note"].ToString());//备注
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
                        service.executeAsync("WMSTEST", "AddNoticeInReturn", "WMSTEST", rq, "", "", Dates.ToString());
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
