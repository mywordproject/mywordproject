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
    [Description("销售出库单---生成应收和应付")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class XSCKDServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FSaleOrgId","FStockOrgId","FCustomerID","FMaterialID","FLot",
                "FBaseUnitID","FPriceUnitId","FBomID","FPRICEBASEQTY","FPriceUnitQty","FDate","FBillTypeID",
                "FRealQty","FUnitID","FPRICEBASEQTY","FBaseUnitQty","FStockID","FTransferBizType",
                "FPrice","FEntryTaxRate","FTaxPrice"
                };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }

        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach (var date in e.DataEntitys)
            {
                string yssql = $@"select FBIZBILLNO from  T_IOS_ARSettlementDetail where FBIZBILLNO='{date["BillNo"].ToString()}'";
                var qs = DBUtils.ExecuteDynamicObject(Context, yssql);
                if (qs.Count > 0)
                {
                    throw new KDBusinessException("", "该订单已生成过应收应付物料");
                }
                else
                {
                    if (date["StockOrgId_Id"].ToString() != date["SaleOrgId_Id"].ToString())
                    {
                        //销售出库
                        string xscksql = $@"
                       select * from T_SAL_OUTSTOCK a
                       inner join T_SAL_OUTSTOCKENTRY b on a.FID = b.FID
                       INNER JOIN T_SAL_OUTSTOCKENTRY_R c ON b.FENTRYID = c.FENTRYID
                       WHERE FSRCBILLNO = '{date["BillNo"]}'";
                        var xsck = DBUtils.ExecuteDynamicObject(Context, xscksql);
                        if (xsck.Count > 0)
                        {
                            //应收单据
                            var data = Utils.LoadBillDataType(Context, "IOS_ARSettlement");
                            data["FFormId"] = "IOS_ARSettlement";
                            data["ReferType"] = "RECEIVE";
                            data["DocumentStatus"] = "Z";
                            data["WholeStatus"] = "Z";
                            data["AcctOrgId_Id"] = date["StockOrgId_Id"];//核算组织
                            data["SettleOrgId_Id"] = date["StockOrgId_Id"];//结算组织
                            data["MapDetailID_Id"] = xsck[0]["FCUSTOMERID"];//对应客户、、
                            data["MapAcctOrgId_Id"] = date["SaleOrgId_Id"];//接收方(核算组织)、、
                            data["MapSettleOrgID_Id"] = date["SaleOrgId_Id"];//接收方(结算组织)、、
                            data["CurrencyId_Id"] = 1;//结算币别
                            data["BizEndDate"] = DateTime.Now.ToString();
                            data["AcctSystemID_Id"] = 1;//结算体系
                           //单据体信息
                            var zjdbdentry = (DynamicObjectCollection)date["SAL_OUTSTOCKENTRY"];
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
                                     FTAXRATE,FPRICEBASE,F_TRAX_DDZKL,F_TRAX_GDZKL,F_TRAX_XJZKL,F_TRAX_TSZKL,F_TRAX_HHZKBL,F_TRAX_XSCBL,F_TRAX_YBZKBL,F_TRAX_TSZKDJP,
                                    F_TRAX_DDZKJE,F_TRAX_XJZKJE,F_TRAX_GDZKJE,F_TRAX_TSZKJE,F_TRAX_TSZKDJP
                                    from T_IOS_PRICELIST a
                                     inner join T_IOS_PRICELISTENTRY b on a.FID=b.FID
                                     WHERE FCREATEORGID='{date["StockOrgId_Id"].ToString()}'
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
                                                gainEntry["F_TRAX_DDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_XJZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_GDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_TSZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) == 0 ? Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]) * Convert.ToDouble(zjdb["RealQty"].ToString()) : Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_TSZKDJP"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                            }
                                            qjly = "IOSPRICE";
                                        }
                                        else if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "False")
                                        {
                                            jsjg = Convert.ToDouble(zjdb["Price"].ToString());
                                            hsje = Convert.ToDouble(zjdb["TaxPrice"].ToString());
                                            sl = Convert.ToDouble(zjdb["TaxRate"].ToString());
                                            qjly = "COSTPRICE";
                                        }
                                    }
                                    gainEntry["MaterialId_Id"] = zjdb["MaterialID_Id"];//物料
                                    gainEntry["Lot_Id"] = zjdb["Lot_Id"];//批号
                                    gainEntry["BomId_Id"] = zjdb["BomID_Id"];//BOM版本
                                    gainEntry["UnitID_Id"] = zjdb["PriceUnitId_Id"];//计价单位
                                    gainEntry["BaseUnitID_Id"] = zjdb["BaseUnitID_Id"];//基本计量单位
                                    gainEntry["Qty"] = zjdb["PriceUnitQty"];//计价单位数量
                                    gainEntry["BaseQty"] = zjdb["PRICEBASEQTY"];//基本单位数量                        
                                    gainEntry["Price"] = jsjg;//结算价格
                                    gainEntry["TaxPrice"] = hsje;//含税单价
                                    gainEntry["Amount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//结算金额
                                    gainEntry["TaxRate"] = sl;//税率%
                                    gainEntry["TaxAmount"] = jsjg * sl / 100 * Convert.ToDouble(zjdb["RealQty"].ToString());//税额
                                    gainEntry["AllAmount"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//价税合计
                                    gainEntry["FMTONO"] = "";//计划跟踪号
                                    gainEntry["BizFormId_Id"] = "SAL_OUTSTOCK";//业务单据名称
                                    gainEntry["BizDate"] = date["Date"];//业务单据日期
                                    gainEntry["BizBillNo"] = date["BillNo"];//业务单据编号
                                    gainEntry["BizBillTypeId_Id"] = date["BillTypeID_Id"];//业务单据类型
                                    gainEntry["SettleReconciliationQty"] = zjdb["RealQty"];//结算对账数量
                                    gainEntry["SettleReconciliationAmount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//结算对账金额
                                    gainEntry["SettleReconciliationTaxAmount"] = (hsje * Convert.ToDouble(zjdb["RealQty"].ToString())) / (1 + sl / 100) * (sl / 100);//结算对账税额
                                    gainEntry["SettleReconciliationAllAmount"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//结算对账价税合计
                                    gainEntry["FSTOCKID_Id"] = zjdb["StockID_Id"];//仓库
                                    gainEntry["StockUnitId_Id"] = zjdb["UnitID_Id"];//库存单位
                                    gainEntry["StockQty"] = zjdb["BaseUnitQty"];//库存单位数量
                                    gainEntry["BaseStockQty"] = zjdb["BaseUnitQty"];//库存基本单位数量
                                    gainEntry["BasePriceQty"] = zjdb["PRICEBASEQTY"];//计价基本单位数量
                                    gainEntry["SysPrice"] = jsjg;//系统定价
                                    gainEntry["PriceBase"] = jgxs;//价格系数
                                    gainEntry["PriceSources"] = qjly;//取价来源
                                    gainEntry["OutSTKOrgId_Id"] = date["StockOrgId_Id"];//调出库存组织
                                    gainEntry["InSTKOrgId_Id"] = date["StockOrgId_Id"];//调入库存组织
                                    gainEntry["OutOwnerId_Id"] = date["StockOrgId_Id"];//调出货主
                                    gainEntry["InOwnerId_Id"] = date["SaleOrgId_Id"];//调入货主
                                    gainEntry["TransferBizType_Id"] = 2;//业务类型
                                    gainEntry["TransferDirect"] = "GENERAL";//结算方向
                                    gainEntry["SrcFormId_Id"] = "SAL_OUTSTOCK";//内部结算单据名称
                                    gainEntry["SrcBillTypeId_Id"] = xsck[0]["FBILLTYPEID"];//内部结算单据类型
                                    gainEntry["SrcBillNo"] = xsck[0]["FBILLNO"];//内部结算单据编号
                                    gainEntry["F_TRAX_DiscountAmount"] = Convert.ToDouble(zjdb["RealQty"].ToString()) * js;//折扣金额
                                    gainEntryCollection.Add(gainEntry);
                                }
                                //汇总单据体
                                if (data["SumEntity"] is DynamicObjectCollection gainEntryCollection1)
                                {
                                    //单据体
                                    var gainEntry1 = Utils.LoadBillDataType(Context, "IOS_ARSettlement", "FSumEntity");
                                    gainEntry1["SumType_Id"] = date["TransferBizType_Id"];//跨组织业务类型
                                    gainEntry1["SumAcount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//应付结算金额
                                                                                                                  //gainEntry1["SumAmount"] = "";//应付关联金额
                                    gainEntry1["FSUMALLACOUNT"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//结算价税合计
                                                                                                                      //gainEntry1["FSUMALLAMOUNT"] = "";//关联价税合计
                                    gainEntryCollection1.Add(gainEntry1);
                                }
                                var Result = Utils.BillOperate(Context, "IOS_ARSettlement", Utils.SaveOption.Audit, data);
                            }
                        }
                    }
                    if (date["StockOrgId_Id"].ToString() != date["SaleOrgId_Id"].ToString())
                    {
                        //采购入库
                        string cgrksql = $@"
                          select * from t_STK_InStock a
                        inner join T_STK_INSTOCKENTRY b on a.FID=b.FID                      
                        where FSRCBILLNO='{date["BillNo"].ToString()}'";
                        var cgrk = DBUtils.ExecuteDynamicObject(Context, cgrksql);
                        if (cgrk.Count > 0)
                        {
                            //销售出库
                            //应收单据
                            var data = Utils.LoadBillDataType(Context, "IOS_APSettlement");
                            data["FFormId"] = "IOS_APSettlement";
                            data["ReferType"] = "PAY";
                            data["DocumentStatus"] = "Z";
                            data["WholeStatus"] = "Z";
                            data["AcctOrgId_Id"] = date["SaleOrgId_Id"];//核算组织 SaleOrgId_Id
                            data["SettleOrgId_Id"] = date["SaleOrgId_Id"];//结算组织
                            data["MapDetailID_Id"] = cgrk[0]["FSUPPLIERID"];//对应供应商、、
                            data["MapAcctOrgId_Id"] = date["StockOrgId_Id"];//接收方(核算组织)、、
                            data["MapSettleOrgID_Id"] = date["StockOrgId_Id"];//接收方(结算组织)、、
                            data["CurrencyId_Id"] = 1;//结算币别
                            data["BizEndDate"] = DateTime.Now.ToString();
                            data["AcctSystemID_Id"] = 1;//结算体系
                                                        //单据体信息
                            var zjdbdentry = (DynamicObjectCollection)date["SAL_OUTSTOCKENTRY"];
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
                                        string a = ((DynamicObject)kcsx["InvPtyId"])["Name"].ToString();
                                        string b = kcsx["IsAffectCost"].ToString();
                                        if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "True")
                                        {
                                            //结算价目表
                                            string jsjmbsql = $@"
                                     select FPRICEBASE,FTAXPRICE HSJG,
                                     round(FTAXPRICE/(1+FTAXRATE/100),6) JG, 
                                     FTAXRATE,FPRICEBASE ,F_TRAX_DDZKL,F_TRAX_GDZKL,F_TRAX_XJZKL,F_TRAX_TSZKL,F_TRAX_HHZKBL,F_TRAX_XSCBL,F_TRAX_YBZKBL,F_TRAX_TSZKDJP,
                                    F_TRAX_DDZKJE,F_TRAX_XJZKJE,F_TRAX_GDZKJE,F_TRAX_TSZKJE,F_TRAX_TSZKDJP
                                     from T_IOS_PRICELIST a
                                     inner join T_IOS_PRICELISTENTRY b on a.FID=b.FID
                                     WHERE FCREATEORGID='{date["StockOrgId_Id"].ToString()}'
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
                                                gainEntry["F_TRAX_DDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_DDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_XJZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_XJZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_GDZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_GDZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_TSZKJE"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"])==0? Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"])* Convert.ToDouble(zjdb["RealQty"].ToString()): Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKL"]) / 100 * hsje * Convert.ToDouble(zjdb["RealQty"].ToString());
                                                gainEntry["F_TRAX_TSZKDJP"] = Convert.ToDouble(jsjmb[0]["F_TRAX_TSZKDJP"]);
                                            }
                                            qjly = "IOSPRICE";
                                        }
                                        else if (((DynamicObject)kcsx["InvPtyId"])["Name"].ToString() == "批号" && kcsx["IsAffectCost"].ToString() == "False")
                                        {
                                            jsjg = Convert.ToDouble(zjdb["Price"].ToString());
                                            hsje = Convert.ToDouble(zjdb["TaxPrice"].ToString());
                                            sl = Convert.ToDouble(zjdb["TaxRate"].ToString());
                                            qjly = "COSTPRICE";
                                        }
                                    }
                                    gainEntry["MaterialId_Id"] = zjdb["MaterialID_Id"];//物料
                                    gainEntry["Lot_Id"] = zjdb["Lot_Id"];//批号
                                    gainEntry["BomId_Id"] = zjdb["BomID_Id"];//BOM版本
                                    gainEntry["UnitID_Id"] = zjdb["PriceUnitId_Id"];//计价单位
                                    gainEntry["BaseUnitID_Id"] = zjdb["BaseUnitID_Id"];//基本计量单位
                                    gainEntry["Qty"] = zjdb["PriceUnitQty"];//计价单位数量
                                    gainEntry["BaseQty"] = zjdb["PRICEBASEQTY"];//基本单位数量                        
                                    gainEntry["Price"] = jsjg;//结算价格
                                    gainEntry["TaxPrice"] = hsje;//含税单价
                                    gainEntry["Amount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//结算金额
                                    gainEntry["TaxRate"] = sl;//税率%
                                    gainEntry["TaxAmount"] = jsjg * sl / 100 * Convert.ToDouble(zjdb["RealQty"].ToString());//税额
                                    gainEntry["AllAmount"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//价税合计
                                    gainEntry["FMTONO"] = "";//计划跟踪号
                                    gainEntry["BizFormId_Id"] = "SAL_OUTSTOCK";//业务单据名称
                                    gainEntry["BizDate"] = date["Date"];//业务单据日期
                                    gainEntry["BizBillNo"] = date["BillNo"];//业务单据编号
                                    gainEntry["BizBillTypeId_Id"] = date["BillTypeID_Id"];//业务单据类型
                                    gainEntry["SettleReconciliationQty"] = zjdb["RealQty"];//结算对账数量
                                    gainEntry["SettleReconciliationAmount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//结算对账金额
                                    gainEntry["SettleReconciliationTaxAmount"] = (hsje * Convert.ToDouble(zjdb["RealQty"].ToString())) / (1 + sl / 100) * (sl / 100);//结算对账税额
                                    gainEntry["SettleReconciliationAllAmount"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//结算对账价税合计
                                    gainEntry["FSTOCKID_Id"] = zjdb["StockID_Id"];//仓库
                                    gainEntry["StockUnitId_Id"] = zjdb["UnitID_Id"];//库存单位
                                    gainEntry["StockQty"] = zjdb["BaseUnitQty"];//库存单位数量
                                    gainEntry["BaseStockQty"] = zjdb["BaseUnitQty"];//库存基本单位数量
                                    gainEntry["BasePriceQty"] = zjdb["PRICEBASEQTY"];//计价基本单位数量
                                    gainEntry["SysPrice"] = jsjg;//系统定价
                                    gainEntry["PriceBase"] = jgxs;//价格系数
                                    gainEntry["PriceSources"] = qjly;//取价来源
                                    gainEntry["OutSTKOrgId_Id"] = date["StockOrgId_Id"];//调出库存组织
                                    gainEntry["InSTKOrgId_Id"] = date["StockOrgId_Id"];//调入库存组织
                                    gainEntry["OutOwnerId_Id"] = date["StockOrgId_Id"];//调出货主
                                    gainEntry["InOwnerId_Id"] = date["SaleOrgId_Id"];//调入货主
                                    gainEntry["TransferBizType_Id"] = 2;//业务类型
                                    gainEntry["TransferDirect"] = "GENERAL";//结算方向
                                    gainEntry["SrcFormId_Id"] = "STK_InStock";//内部结算单据名称
                                    gainEntry["SrcBillTypeId_Id"] = cgrk[0]["FBILLTYPEID"];//内部结算单据类型
                                    gainEntry["SrcBillNo"] = cgrk[0]["FBILLNO"];//内部结算单据编号
                                    gainEntry["F_TRAX_DiscountAmount"] = Convert.ToDouble(zjdb["RealQty"].ToString()) * js;//折扣金额
                                    gainEntryCollection.Add(gainEntry);
                                }
                                //汇总单据体
                                if (data["SumEntity"] is DynamicObjectCollection gainEntryCollection1)
                                {
                                    //单据体
                                    var gainEntry1 = Utils.LoadBillDataType(Context, "IOS_APSettlement", "FSumEntity");
                                    gainEntry1["SumType_Id"] = date["TransferBizType_Id"];//跨组织业务类型
                                    gainEntry1["SumAcount"] = jsjg * Convert.ToDouble(zjdb["RealQty"].ToString());//应付结算金额
                                                                                                                  //gainEntry1["SumAmount"] = "";//应付关联金额
                                    gainEntry1["FSUMALLACOUNT"] = hsje * Convert.ToDouble(zjdb["RealQty"].ToString());//结算价税合计
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
