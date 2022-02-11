using ingdee.K3.SCM.Extend.BusinessPlugIn;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF.组织间结算
{
    [Description("直接调拨单---生成应收和应付")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class ZJDBDBillPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FTransferBizType", "FSettleOrgId", "FSUPPLIERID","FCustID",
                "FStockOutOrgId" ,"FStockOrgId","FMaterialId","FDestLot","FBomId","FBaseUnitId",
                "FPriceUnitID","FDate","FConsignPrice","FTaxPrice","FUnitID","FBaseQty","FOwnerIdHead",
                "FTaxRate","FPriceQty","FPriceBaseQty","FSrcBizBaseQty","FQty","FPriceBaseQty","FOwnerOutIdHead",
                "FTransferDirect","FBizType","FBillTypeID","FTransferBizTypeId","FOwnerTypeOutIdHead"
                };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }

        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach(var date in e.DataEntitys)
            {
                string yssql = $@"select FBIZBILLNO from  T_IOS_ARSettlementDetail where FBIZBILLNO='{date["BillNo"].ToString()}'";
                var qs = DBUtils.ExecuteDynamicObject(Context, yssql);
                if(qs.Count>0)
                {
                    throw new KDBusinessException("", "该订单已生成过应收应付物料");
                }
                else
                {
                   string glbs= Guid.NewGuid().ToString("N");//关联标识
                    if (date["TransferBizType"].ToString() == "OverOrgTransfer")
                    {
                        //分布式调出
                        string fbstcsql = $@"
                        select * from T_STK_STKTRANSFEROUT a
                        inner join T_STK_STKTRANSFEROUTENTRY b on a.FID=b.FID
                        inner join T_STK_STKTRANSFEROUTENTRY_R c on b.FENTRYID=c.FENTRYID
                        where FSRCBILLNO='{date["BillNo"].ToString()}'";
                        var fbstcdate = DBUtils.ExecuteDynamicObject(Context, fbstcsql);
                        if (fbstcdate.Count > 0)
                        {
                            //单据
                            var data = Utils.LoadBillDataType(Context, "IOS_ARSettlement");
                            data["FFormId"] = "IOS_ARSettlement";
                            data["ReferType"] = "RECEIVE";
                            data["DocumentStatus"] = "Z";
                            data["WholeStatus"] = "Z";
                            data["AcctOrgId_Id"] = date["StockOutOrgId_Id"];//核算组织
                            data["SettleOrgId_Id"] = date["StockOutOrgId_Id"];//结算组织
                            data["MapDetailID_Id"] = date["CustID_Id"];//对应客户、、

                            data["MapAcctOrgId_Id"] = date["StockOrgId_Id"];//接收方(核算组织)、、
                            data["MapSettleOrgID_Id"] = date["StockOrgId_Id"];//接收方(结算组织)、、
                            data["CurrencyId_Id"] = 1;//结算币别
                            data["BizEndDate"] = DateTime.Now.ToString();
                            data["AcctSystemID_Id"] = 1;//结算体系
                                                        //单据体信息
                            var zjdbdentry = (DynamicObjectCollection)date["TransferDirectEntry"];
                            int i = 0;
                            foreach (var zjdb in zjdbdentry)
                            {
                                i++;
                                //判断物料有没有勾选，勾选则取结算价目表的值
                                var wl = Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(zjdb["MaterialId_Id"].ToString()));
                                var kcsxs = wl["MaterialInvPty"] as DynamicObjectCollection;
                                double jsjg = 0;//结算价格
                                double hsje = 0;//含税价格
                                double sl = 0;//税率
                                double jgxs = 0;//价格系数
                                string qjly = "COSTPRICE";//取价来源
                                double js = 0;//计算                              
                                if (data["DetailEntity"] is DynamicObjectCollection gainEntryCollection)
                                {
                                   
                                    //单据体
                                    var gainEntry = Utils.LoadBillDataType(Context, "IOS_ARSettlement", "FDetailEntity");
                                    gainEntry["Seq"] = i;//行号
                                    foreach (var kcsx in kcsxs)
                                    {
                                        if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "True")
                                        {
                                            //结算价目表
                                            string jsjmbsql = $@"
                                     select FPRICEBASE,FTAXPRICE HSJG,
                                     round(FTAXPRICE/(1+FTAXRATE/100),6) JG, 
                                     FTAXRATE,F_TRAX_DDZKL,F_TRAX_GDZKL,F_TRAX_XJZKL,F_TRAX_TSZKL,F_TRAX_HHZKBL,F_TRAX_XSCBL,F_TRAX_YBZKBL,F_TRAX_TSZKDJP,
                                     F_TRAX_DDZKJE,F_TRAX_XJZKJE,F_TRAX_GDZKJE,F_TRAX_TSZKJE
                                     from T_IOS_PRICELIST a
                                     inner join T_IOS_PRICELISTENTRY b on a.FID=b.FID
                                     WHERE FCREATEORGID='{date["StockOutOrgId_Id"].ToString()}'
                                     AND FMATERIALID='{zjdb["MaterialId_Id"].ToString()}'
                                     and b.FEFFECTIVEDATE<=to_date('{DateTime.Now.ToString()}','yyyy-mm-dd hh24:mi:ss')  
                                     and b.FEXPRIYDATE>= to_date('{DateTime.Now.ToString()}','yyyy-mm-dd hh24:mi:ss')";
                                            var jsjmb = DBUtils.ExecuteDynamicObject(Context, jsjmbsql);
                                            if (jsjmb.Count > 0)
                                            {
                                                jsjg = Convert.ToDouble(jsjmb[0]["JG"].ToString());
                                                hsje = Convert.ToDouble(jsjmb[0]["HSJG"].ToString());
                                                sl = Convert.ToDouble(jsjmb[0]["FTAXRATE"].ToString());
                                                jgxs = Convert.ToDouble(jsjmb[0]["FPRICEBASE"].ToString());
                                                js = Convert.ToDouble(jsjmb[0]["HSJG"]) * (Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100 +
                                                    Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 +
                                                    Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_HHZKBL"]) / 100 +
                                                     Convert.ToDouble(jsjmb[0]["F_TRAX_XSCBL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_YBZKBL"]) / 100) +
                                                    Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                                gainEntry["F_TRAX_GDZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                                gainEntry["F_TRAX_XJZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]);
                                                gainEntry["F_TRAX_TSZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]);
                                                gainEntry["F_TRAX_HHZKBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_HHZKBL"]);
                                                gainEntry["F_TRAX_XSCBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XSCBL"]);
                                                gainEntry["F_TRAX_YBZKBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_YBZKBL"]);
                                                gainEntry["F_TRAX_DDZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                                gainEntry["F_TRAX_DDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_XJZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_GDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_TSZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) == 0 ? Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]) * Convert.ToDouble(zjdb["Qty"].ToString()) : Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_TSZKDJP"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                               // gainEntry["MaterialId_Id"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                            }
                                            qjly = "IOSPRICE";
                                        }
                                        else if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "False")
                                        {
                                            var wlBases = wl["MaterialBase"] as DynamicObjectCollection;
                                            double jjbl = 0;//加价比例
                                            foreach (var wlBase in wlBases)
                                            {
                                                var sls = Utils.LoadBDData(Context, "BD_TaxRate", Convert.ToInt32(wlBase["TaxRateId_Id"].ToString()));
                                                sl = Convert.ToDouble(sls["TaxRate"]);
                                                jjbl = Convert.ToDouble(wlBase["CostPriceRate"]);
                                            }
                                            jsjg = Convert.ToDouble(fbstcdate[0]["FPRICE"].ToString()) * (1 + jjbl / 100);
                                            hsje = jsjg * (1 + sl / 100);
                                            // sl = Convert.ToDouble(zjdb["TaxRate"].ToString());
                                            qjly = "COSTPRICE";
                                        }
                                    }
                                    gainEntry["MaterialId_Id"] = zjdb["MaterialId_Id"];//物料
                                    gainEntry["Lot_Id"] = zjdb["DestLot_Id"];//批号
                                    gainEntry["BomId_Id"] = zjdb["BomId_Id"];//BOM版本
                                    gainEntry["UnitID_Id"] = zjdb["PriceUnitID_Id"];//计价单位
                                    gainEntry["BaseUnitID_Id"] = zjdb["BaseUnitId_Id"];//基本计量单位
                                    gainEntry["Qty"] = zjdb["PriceQty"];//计价单位数量
                                    gainEntry["BaseQty"] = zjdb["PriceBaseQty"];//基本单位数量                        
                                    gainEntry["Price"] = jsjg;//结算价格
                                    gainEntry["TaxPrice"] = hsje;//含税单价
                                    gainEntry["Amount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//结算金额
                                    gainEntry["TaxRate"] = sl;//税率%
                                    gainEntry["TaxAmount"] = jsjg * sl / 100 * Convert.ToDouble(zjdb["Qty"].ToString());//税额
                                    gainEntry["AllAmount"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//价税合计
                                    gainEntry["FMTONO"] = "";//计划跟踪号
                                    gainEntry["BizFormId_Id"] = "STK_TransferDirect";//业务单据名称
                                    gainEntry["BizDate"] = date["Date"];//业务单据日期
                                    gainEntry["BizBillNo"] = date["BillNo"];//业务单据编号
                                    gainEntry["BizBillTypeId_Id"] = date["BillTypeID_Id"];//业务单据类型
                                    gainEntry["SettleReconciliationQty"] = zjdb["Qty"];//结算对账数量
                                    gainEntry["SettleReconciliationAmount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//结算对账金额
                                    gainEntry["SettleReconciliationTaxAmount"] = (hsje * Convert.ToDouble(zjdb["Qty"].ToString()))/(1+ sl / 100)* (sl / 100);//结算对账税额
                                    gainEntry["SettleReconciliationAllAmount"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//结算对账价税合计
                                                                                                                                 // gainEntry["FSTOCKID_Id"] = "";//仓库
                                    gainEntry["StockUnitId_Id"] = zjdb["UnitId_Id"];//库存单位
                                    gainEntry["StockQty"] = zjdb["Qty"];//库存单位数量
                                    gainEntry["BaseStockQty"] = zjdb["BaseQty"];//库存基本单位数量
                                    gainEntry["BasePriceQty"] = zjdb["PriceBaseQty"];//计价基本单位数量
                                    gainEntry["SysPrice"] = jsjg;//系统定价
                                    gainEntry["PriceBase"] = jgxs;//价格系数
                                    gainEntry["PriceSources"] = qjly;//取价来源
                                    gainEntry["OutSTKOrgId_Id"] = date["StockOutOrgId_Id"];//调出库存组织
                                    gainEntry["InSTKOrgId_Id"] = date["StockOrgId_Id"];//调入库存组织
                                    gainEntry["OutOwnerId_Id"] = date["OwnerOutIdHead_Id"];//调出货主
                                    gainEntry["InOwnerId_Id"] = date["OwnerIdHead_Id"];//调入货主
                                    gainEntry["TransferBizType_Id"] = date["TransferBizTypeId_Id"];//业务类型
                                    gainEntry["TransferDirect"] = date["TransferDirect"];//结算方向
                                    gainEntry["SrcFormId_Id"] = "STK_TRANSFEROUT";//内部结算单据名称
                                    gainEntry["SrcID"] = fbstcdate[0]["FID"]; //内部结算单据内码
                                    gainEntry["SrcEntryId"] = fbstcdate[0]["FENTRYID"];//内部结算单据分录内码
                                    gainEntry["SrcBillTypeId_Id"] = fbstcdate[0]["FBILLTYPEID"];//内部结算单据类型
                                    gainEntry["SrcBillNo"] = fbstcdate[0]["FBILLNO"];//内部结算单据编号
                                    gainEntry["MarkKey"] = glbs;//关联标示
                                    gainEntry["ReMapAcctOrgId"] = date["StockOutOrgId_Id"];//对应核算组织
                                    gainEntry["ReMapSettleOrgID"] = date["StockOutOrgId_Id"];//对应结算组织
                                    gainEntry["BizEntryId"] = glbs;//业务单据分录内码
                                    gainEntry["BizID"] = glbs;//业务单据内码
                                    gainEntry["ConfigId"] = glbs;//结算配置Id
                                    gainEntry["BizEntryId"] = glbs;//业务单据分录内码
                                    gainEntry["BizEntryId"] = glbs;//业务单据分录内码



                                    gainEntry["F_TRAX_DiscountAmount"] = Convert.ToDouble(zjdb["Qty"].ToString())*js;//折扣金额
                                    gainEntryCollection.Add(gainEntry);
                                }
                                //汇总单据体
                                if (data["SumEntity"] is DynamicObjectCollection gainEntryCollection1)
                                {
                                    //单据体
                                    var gainEntry1 = Utils.LoadBillDataType(Context, "IOS_ARSettlement", "FSumEntity");
                                    gainEntry1["SumType_Id"] = date["TransferBizTypeId_Id"];//跨组织业务类型
                                    gainEntry1["SumAcount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//应付结算金额
                                                                                                              //gainEntry1["SumAmount"] = "";//应付关联金额
                                    gainEntry1["FSUMALLACOUNT"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//结算价税合计
                                                                                                                  //gainEntry1["FSUMALLAMOUNT"] = "";//关联价税合计
                                    gainEntryCollection1.Add(gainEntry1);
                                }
                                var Result = Utils.BillOperate(Context, "IOS_ARSettlement", Utils.SaveOption.Audit, data);
                            }
                        }
                    }
                    if (date["TransferBizType"].ToString() == "OverOrgTransfer")
                    {
                        //分布式调入
                        string fbstrsql = $@"
                        select * from T_STK_STKTRANSFERIN a
                        inner join T_STK_STKTRANSFERINENTRY b on a.FID=b.FID
                        inner join T_STK_STKTRANSFERINENTRY_R c on b.FENTRYID=c.FENTRYID
                        where FSRCBILLNO='{date["BillNo"].ToString()}'";
                        var fbstrdate = DBUtils.ExecuteDynamicObject(Context, fbstrsql);
                        if (fbstrdate.Count > 0)
                        {
                            //单据
                            var data = Utils.LoadBillDataType(Context, "IOS_APSettlement");
                            data["FFormId"] = "IOS_APSettlement";
                            data["ReferType"] = "PAY";
                            data["DocumentStatus"] = "Z";
                            data["WholeStatus"] = "Z";
                            data["AcctOrgId_Id"] = date["StockOrgId_Id"];//核算组织
                            data["SettleOrgId_Id"] = date["StockOrgId_Id"];//结算组织
                            data["MapDetailID_Id"] = date["SupplierID_Id"];//对应供应商
                                                                           //data["AcctSystemID_I"] = "";//会计结算体系
                            data["MapAcctOrgId_Id"] = date["StockOutOrgId_Id"];// 供货方(核算组织)
                            data["MapSettleOrgID_Id"] = date["StockOutOrgId_Id"];//供货方(结算组织)
                            data["CurrencyId_Id"] = 1;//结算币别
                            data["BizEndDate"] = DateTime.Now.ToString();
                            data["AcctSystemID_Id"] = 1;//结算体系
                                                        //单据体信息
                            var zjdbdentry = (DynamicObjectCollection)date["TransferDirectEntry"];
                            int i = 0;
                            foreach (var zjdb in zjdbdentry)
                            {
                                //判断物料有没有勾选，勾选则取结算价目表的值
                                var wl = Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(zjdb["MaterialId_Id"].ToString()));
                                var kcsxs = wl["MaterialInvPty"] as DynamicObjectCollection;
                                
                                double jsjg = 0;//结算价格
                                double hsje = 0;//含税价格
                                double sl = 0;//税率
                                double jgxs = 0;//价格系数
                                string qjly = "COSTPRICE";//取价来源
                                double js = 0;
                                
                                if (data["DetailEntity"] is DynamicObjectCollection gainEntryCollection)
                                {
                                    //单据体
                                    var gainEntry = Utils.LoadBillDataType(Context, "IOS_APSettlement", "FDetailEntity");
                                    gainEntry["Seq"] = i;//行号
                                    foreach (var kcsx in kcsxs)
                                    {

                                        if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "True")
                                        {
                                            //结算价目表
                                            string jsjmbsql = $@"
                                     select FPRICEBASE,FTAXPRICE HSJG,
                                     round(FTAXPRICE/(1+FTAXRATE/100),6) JG, 
                                     FTAXRATE ,F_TRAX_DDZKL,F_TRAX_GDZKL,F_TRAX_XJZKL,F_TRAX_TSZKL,F_TRAX_HHZKBL,F_TRAX_XSCBL,F_TRAX_YBZKBL,F_TRAX_TSZKDJP,
                                       F_TRAX_DDZKJE,F_TRAX_XJZKJE,F_TRAX_GDZKJE,F_TRAX_TSZKJE
                                    from T_IOS_PRICELIST a
                                     inner join T_IOS_PRICELISTENTRY b on a.FID=b.FID
                                     WHERE FCREATEORGID='{date["StockOutOrgId_Id"].ToString()}'
                                     AND FMATERIALID='{zjdb["MaterialId_Id"].ToString()}'
                                     and b.FEFFECTIVEDATE<=to_date(' {DateTime.Now.ToString()} ','yyyy-mm-dd hh24:mi:ss')  
                                     and b.FEXPRIYDATE>= to_date('{DateTime.Now.ToString()}','yyyy-mm-dd hh24:mi:ss')";
                                            var jsjmb = DBUtils.ExecuteDynamicObject(Context, jsjmbsql);
                                            if (jsjmb.Count > 0)
                                            {
                                                jsjg = Convert.ToDouble(jsjmb[0]["JG"].ToString());
                                                hsje = Convert.ToDouble(jsjmb[0]["HSJG"].ToString());
                                                sl = Convert.ToDouble(jsjmb[0]["FTAXRATE"].ToString());
                                                jgxs = Convert.ToDouble(jsjmb[0]["FPRICEBASE"].ToString());
                                                js = Convert.ToDouble(jsjmb[0]["HSJG"]) * (Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100 +
                                                   Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 +
                                                   Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_HHZKBL"]) / 100 +
                                                    Convert.ToDouble(jsjmb[0]["F_TRAX_XSCBL"]) / 100 + Convert.ToDouble(jsjmb[0]["F_TRAX_YBZKBL"]) / 100) +
                                                   Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                                gainEntry["F_TRAX_GDZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                                gainEntry["F_TRAX_XJZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]);
                                                gainEntry["F_TRAX_TSZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]);
                                                gainEntry["F_TRAX_HHZKBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_HHZKBL"]);
                                                gainEntry["F_TRAX_XSCBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XSCBL"]);
                                                gainEntry["F_TRAX_YBZKBL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_YBZKBL"]);
                                                gainEntry["F_TRAX_DDZKL"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                                gainEntry["F_TRAX_DDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100* hsje* Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_XJZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_GDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_TSZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) == 0 ? Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]) * Convert.ToDouble(zjdb["Qty"].ToString()) : Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["Qty"].ToString());
                                                gainEntry["F_TRAX_TSZKDJP"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                                // gainEntry["MaterialId_Id"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]);
                                            }
                                            qjly = "IOSPRICE";
                                        }
                                        else if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "False")
                                        {
                                            //分布式调出
                                            string fbstcsql = $@"
                                      select * from T_STK_STKTRANSFEROUT a
                                      inner join T_STK_STKTRANSFEROUTENTRY b on a.FID=b.FID
                                      inner join T_STK_STKTRANSFEROUTENTRY_R c on b.FENTRYID=c.FENTRYID
                                      where FSRCBILLNO='{date["BillNo"].ToString()}'";
                                            var fbstcdate = DBUtils.ExecuteDynamicObject(Context, fbstcsql);
                                            foreach (var fbstc in fbstcdate)
                                            {
                                                if (fbstc["FMATERIALID"].ToString() == zjdb["MaterialId_Id"].ToString())
                                                {
                                                    var wlBases = wl["MaterialBase"] as DynamicObjectCollection;
                                                    double jjbl = 0;//加价比例
                                                    foreach (var wlBase in wlBases)
                                                    {
                                                        var sls = Utils.LoadBDData(Context, "BD_TaxRate", Convert.ToInt32(wlBase["TaxRateId_Id"].ToString()));
                                                        sl = Convert.ToDouble(sls["TaxRate"]);
                                                        jjbl = Convert.ToDouble(wlBase["CostPriceRate"]);
                                                    }
                                                    jsjg = Convert.ToDouble(fbstc["FPRICE"].ToString()) * (1 + jjbl / 100);
                                                    hsje = jsjg * (1 + sl / 100);
                                                    // sl = Convert.ToDouble(zjdb["TaxRate"].ToString());
                                                    qjly = "COSTPRICE";
                                                }

                                            }

                                        }
                                    }
                                    gainEntry["MaterialId_Id"] = zjdb["MaterialId_Id"];//物料
                                    gainEntry["Lot_Id"] = zjdb["DestLot_Id"];//批号
                                    gainEntry["BomId_Id"] = zjdb["BomId_Id"];//BOM版本
                                    gainEntry["UnitID_Id"] = zjdb["PriceUnitID_Id"];//计价单位
                                    gainEntry["BaseUnitID_Id"] = zjdb["BaseUnitId_Id"];//基本计量单位
                                    gainEntry["Qty"] = zjdb["PriceQty"];//计价单位数量
                                    gainEntry["BaseQty"] = zjdb["PriceBaseQty"];//基本单位数量                        
                                    gainEntry["Price"] = jsjg;//结算价格
                                    gainEntry["TaxPrice"] = hsje;//含税单价
                                    gainEntry["Amount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//结算金额
                                    gainEntry["TaxRate"] = sl;//税率%
                                    gainEntry["TaxAmount"] = jsjg * sl / 100 * Convert.ToDouble(zjdb["Qty"].ToString());//税额
                                    gainEntry["AllAmount"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//价税合计
                                    gainEntry["FMTONO"] = "";//计划跟踪号
                                    gainEntry["BizFormId_Id"] = "STK_TransferDirect";//业务单据名称
                                    gainEntry["BizDate"] = date["Date"];//业务单据日期
                                    gainEntry["BizBillNo"] = date["BillNo"];//业务单据编号
                                    gainEntry["BizBillTypeId_Id"] = date["BillTypeID_Id"];//业务单据类型
                                    gainEntry["SettleReconciliationQty"] = zjdb["Qty"];//结算对账数量
                                    gainEntry["SettleReconciliationAmount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//结算对账金额
                                    gainEntry["SettleReconciliationTaxAmount"] = (hsje * Convert.ToDouble(zjdb["Qty"].ToString())) / (1 + sl / 100) * (sl / 100);//结算对账税额
                                    gainEntry["SettleReconciliationAllAmount"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//结算对账价税合计
                                    // gainEntry["FSTOCKID_Id"] = "";//仓库
                                    gainEntry["StockUnitId_Id"] = zjdb["UnitId_Id"];//库存单位
                                    gainEntry["StockQty"] = zjdb["Qty"];//库存单位数量
                                    gainEntry["BaseStockQty"] = zjdb["BaseQty"];//库存基本单位数量
                                    gainEntry["BasePriceQty"] = zjdb["PriceBaseQty"];//计价基本单位数量
                                    gainEntry["SysPrice"] = jsjg;//系统定价
                                    gainEntry["PriceBase"] = jgxs;//价格系数
                                    gainEntry["PriceSources"] = qjly;//取价来源
                                    gainEntry["OutSTKOrgId_Id"] = date["StockOutOrgId_Id"];//调出库存组织
                                    gainEntry["InSTKOrgId_Id"] = date["StockOrgId_Id"];//调入库存组织
                                    gainEntry["OutOwnerId_Id"] = date["OwnerOutIdHead_Id"];//调出货主
                                    gainEntry["InOwnerId_Id"] = date["OwnerIdHead_Id"];//调入货主
                                    gainEntry["TransferBizType_Id"] = date["TransferBizTypeId_Id"];//业务类型
                                    gainEntry["TransferDirect"] = date["TransferDirect"];//结算方向
                                    gainEntry["SrcFormId_Id"] = "STK_TRANSFERIN";//内部结算单据名称
                                    gainEntry["SrcBillTypeId_Id"] = fbstrdate[0]["FBILLTYPEID"];//内部结算单据类型
                                    gainEntry["SrcBillNo"] = fbstrdate[0]["FBILLNO"];//内部结算单据编号
                                    gainEntry["F_TRAX_DiscountAmount"] = Convert.ToDouble(zjdb["Qty"].ToString()) * js;//折扣金额
                                    gainEntryCollection.Add(gainEntry);
                                }
                                //汇总单据体
                                if (data["SumEntity"] is DynamicObjectCollection gainEntryCollection1)
                                {
                                    //单据体
                                    var gainEntry1 = Utils.LoadBillDataType(Context, "IOS_APSettlement", "FSumEntity");
                                    gainEntry1["SumType_Id"] = date["TransferBizTypeId_Id"];//跨组织业务类型
                                    gainEntry1["SumAcount"] = jsjg * Convert.ToDouble(zjdb["Qty"].ToString());//应付结算金额
                                                                                                                  //gainEntry1["SumAmount"] = "";//应付关联金额
                                    gainEntry1["FSUMALLACOUNT"] = hsje * Convert.ToDouble(zjdb["Qty"].ToString());//结算价税合计
                                                                                                                      //gainEntry1["FSUMALLAMOUNT"] = "";//关联价税合计
                                    gainEntryCollection1.Add(gainEntry1);
                                }
                                var Result = Utils.BillOperate(Context, "IOS_APSettlement", Utils.SaveOption.Audit, data);
                            }
                        }
                    }
                    
                }
                
            }
        }
    }
}
