using GYL.ServicePlugIn.WebReference;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace GYL.ServicePlugIn.ZCYW
{
    [Description("分布式调入单同步到WMS")]
    [Kingdee.BOS.Util.HotUpdate]
    public class FBSDRServerPlugin:AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FEntryNote","FMaterialId","FConvertType","FBaseQty","FNOTE","FDate","FStockOrgID","FBillTypeID",
                "FBillNo"
            };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var date in e.DataEntitys)
            {
                string rq = date["Date"].ToString();
                //其他入库----****方法***                  
                    JObject Dates = new JObject();
                    Dates.Add("billid", date["Id"].ToString());//id
                    Dates.Add("billtypeid", "QTRK");//业务类型
                    Dates.Add("companycode", date["StockOrgId"] == null ? "" : ((DynamicObject)date["StockOrgId"])["Number"].ToString());//公司*********
                    Dates.Add("erp_no", date["BillNo"].ToString());//单据编号   
                    Dates.Add("billldate", date["Date"].ToString());//单据日期                   
                    Dates.Add("supplyid", "");//往来户
                    Dates.Add("storagelocation", "6001");//送出存储地点   date["StockOrgId"] == null ? "" : ((DynamicObject)date["StockOrgId"])["Number"].ToString()                              
                Dates.Add("note", date["NOTE"].ToString());//备注
                    JArray info = new JArray();
                DynamicObjectCollection FentryDate = date["STK_STKTRANSFERINENTRY"] as DynamicObjectCollection;
                foreach (var entrydate in FentryDate)
                {
                    JObject entry = new JObject();
                    entry.Add("billdtlid", entrydate["Id"].ToString());//明细ID
                    entry.Add("skucode", "0020001");//SKU (entrydate["MaterialID"] as DynamicObject)["Number"].ToString()
                    entry.Add("Materialtype", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品
                    entry.Add("basequantity", entrydate["BaseQty"].ToString());//基本数量*********
                    entry.Add("quantity", "");//箱数*********
                    entry.Add("deliveryDate","");//预计到货日期                      
                    entry.Add("notedtl", entrydate["EntryNote"].ToString());//备注
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
