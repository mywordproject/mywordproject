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
    [Kingdee.BOS.Util.HotUpdate]
    [Description("直接调拨单同步到WMS")]
    public class ZJDBDServerPlugin: AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 操作结束后功能处理---------缺失方法
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var date in e.DataEntitys)
            {
                string rq = date["Date"].ToString();
                //其他出库
                JObject Dates = new JObject();
                Dates.Add("billId", date["Id"].ToString());//id
                Dates.Add("billtypeid", "QTCK");//业务类型
                Dates.Add("companycode", date["StockOutOrgId"] == null ? "" : ((DynamicObject)date["StockOutOrgId"])["Number"].ToString());//公司*********
                Dates.Add("erp_no", date["BillNo"].ToString());//单据编号
                Dates.Add("isallocate", 0);//是否有转储单,0表示没有；1表示有*********
                Dates.Add("billldate", date["Date"].ToString());//单据日期                  
                Dates.Add("supplyid", "");//往来户                     
                Dates.Add("storagelocation", "6001");//送出存储地点 date["StockId"] == null ? "" : ((DynamicObject)date["StockId"])["Number"].ToString()
                Dates.Add("deliveryaddress", "");//客户收货地址
                Dates.Add("deliverystorage", "");//客户收货仓库*********
                Dates.Add("deliverycon", "");//送货联系人
                Dates.Add("deliverytel", "");//送货联系电话*********
                Dates.Add("note", date["Note"].ToString());//备注

                //采购订单---其他入库                       
                JObject Datess = new JObject();
                Datess.Add("billid", date["Id"].ToString());//id
                Datess.Add("billtypeid", "QTRK");//业务类型
                Datess.Add("companycode", date["StockOrgId"] == null ? "" : ((DynamicObject)date["StockOrgId"])["Number"].ToString());//公司*********
                Datess.Add("erp_no", date["BillNo"].ToString());//单据编号   
                Datess.Add("billldate", date["Date"].ToString());//单据日期                   
                Datess.Add("supplyid", "");//往来户
                Datess.Add("storagelocation", "6001");//送出存储地点                                 
                Datess.Add("note", date["Note"].ToString());//备注

                JArray info = new JArray();
                JArray info1 = new JArray();
                var entrydates = date["StatusConvertEntry"] as DynamicObjectCollection;
                foreach (var entrydate in entrydates)
                {
                    //if (entrydate["ConvertType"].ToString() == "A")
                    //{
                        JObject entry = new JObject();
                        entry.Add("billdtlid", entrydate["Id"].ToString());//明细ID
                        entry.Add("skucode", "0020001");//SKU ((DynamicObject)entrydate["MaterialID"])["Number"].ToString()
                        entry.Add("materialtype", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品
                        entry.Add("basequantity", entrydate["BaseQty"].ToString());//基本数量
                        entry.Add("quantity", "");//箱数
                        entry.Add("deliverydate", "");//预计到货日期                       
                        entry.Add("customerunit", "");//客户计量单位名称*****
                        entry.Add("unitratio", "");//单位转换比率*****
                        entry.Add("customerordernumber", "");//客户订单号*****
                        entry.Add("allocateno", "");//转储单号*****
                        entry.Add("notedtl", entrydate["NoteEntry"].ToString());//备注
                        info.Add(entry);
                  //  }
                   // else if (entrydate["ConvertType"].ToString() == "B")
                  //  {
                        JObject entry1 = new JObject();
                        entry1.Add("billdtlid", entrydate["Id"].ToString());//明细ID
                        entry1.Add("skucode", "0020001");//SKU  (entrydate["MaterialID"] as DynamicObject)["Number"].ToString()
                        entry1.Add("materialtype", "SP");//商品属性，SP表示商品；YP表示样品；ZP表示赠品
                        entry1.Add("basequantity", entrydate["BaseQty"].ToString());//基本数量*********
                        entry1.Add("quantity", "");//箱数*********
                        entry1.Add("deliverydate", "");//预计到货日期                      
                        entry1.Add("notedtl", entrydate["NoteEntry"].ToString());//备注
                        info.Add(entry1);
                   // }
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
                        
             Datess.Add("grid", info1);
             GatewayService service1 = new GatewayService();
             service1.Url = "http://47.100.33.219:8080/ws/gateway";
             string k = "";
             service1.executeCompleted += (a, b) =>
             {
                 k = b.Result;
             };
             service1.executeAsync("WMSTEST", "AddNoticeIn", "WMSTEST", rq, "", "", Datess.ToString());
         
            }
        }
    }
}
