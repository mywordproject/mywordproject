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
    [Description("分布式调出单同步到WMS")]
    [Kingdee.BOS.Util.HotUpdate]
    public class FBSDCServerPlugin : AbstractOperationServicePlugIn
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
                //其他出库----****方法***
                JObject Dates = new JObject();
                Dates.Add("billId", date["Id"].ToString());//id
                Dates.Add("billtypeid", "QTCK");//业务类型
                Dates.Add("companycode", date["StockOrgID"] == null ? "" : ((DynamicObject)date["StockOrgID"])["Number"].ToString());//公司*********
                Dates.Add("erp_no", date["BillNo"].ToString());//单据编号
                Dates.Add("isallocate", 0);//是否有转储单,0表示没有；1表示有*********
                Dates.Add("billldate", date["Date"].ToString());//单据日期                  
                Dates.Add("supplyid", date["CustID"] == null ? "" : ((DynamicObject)date["CustID"])["Number"].ToString());//往来户                     
                Dates.Add("storagelocation", "6001");//送出存储地点  date["StockOrgID"] == null ? "" : ((DynamicObject)date["StockOrgID"])["Number"].ToString()
                Dates.Add("deliveryaddress", "");//客户收货地址
                Dates.Add("deliverystorage", "");//客户收货仓库*********
                Dates.Add("deliverycon", "");//送货联系人
                Dates.Add("deliverytel", "");//送货联系电话*********
                Dates.Add("note", date["NOTE"].ToString());//备注
                JArray info = new JArray();
                DynamicObjectCollection FentryDate = date["STK_STKTRANSFEROUTENTRY"] as DynamicObjectCollection;
                foreach (var entrydate in FentryDate)
                {
                    JObject entry = new JObject();
                    entry.Add("billdtlid", entrydate["Id"].ToString());//明细ID
                    entry.Add("skucode", "0020001");//SKU  ((DynamicObject)entrydate["MaterialID"])["Number"].ToString()
                    entry.Add("materialtype", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品
                    entry.Add("basequantity", entrydate["BaseQty"].ToString());//基本数量
                    entry.Add("quantity", "");//箱数
                    entry.Add("deliverydate", "");//预计到货日期                       
                    entry.Add("customerunit", "");//客户计量单位名称*****
                    entry.Add("unitratio", "");//单位转换比率*****
                    entry.Add("customerordernumber", "");//客户订单号*****
                    entry.Add("allocateno", "");//转储单号*****
                    entry.Add("notedtl", entrydate["EntryNote"].ToString());//备注
                    info.Add(entry);
                }
                Dates.Add("grid", info);
                GatewayService service = new GatewayService();
                service.Url = "http://47.100.33.219:8080/ws/gateway";
                string st = "";
                service.executeCompleted += (a, b) =>
                {

                    st = b.Result;
                };
                service.executeAsync("WMSTEST", "AddNoticeOut", "WMSTEST", rq, "", "", Dates.ToString());

            }

        }
    }
}

