using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [Description("采购订单获取折扣率和折扣金额")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGDDBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {

            base.DataChanged(e);
            if (e.Field.FieldName == "FMATERIALID")
            {
                if (((DynamicObject)this.Model.GetValue("FBILLTYPEID")) != null && ((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Number"].ToString() == "CGDD01_SYS")
                {
                    var gys = ((DynamicObject)this.View.Model.GetValue("FSUPPLIERID"));//供应商
                    var wl = (DynamicObject)this.Model.GetValue("FMATERIALID", e.Row);//物料
                    var cgzz = (DynamicObject)this.Model.GetValue("FPURCHASEORGID");//采购组织
                    if (wl != null && gys != null)
                    {
                        //获取采购价目表的折扣率和金额
                        string hqsql = $@"/*dialect*/
                         select 
                         ROUND(CASE WHEN b.FUNITID != WLDW.FBASEUNITID THEN (FTAXPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE FTAXPRICE END,6) FTAXPRICE,
                         F_TRAX_DDZKL,F_TRAX_DDZKJE,F_TRAX_XJZKL,F_TRAX_XJZKJE,
                         F_TRAX_GDZKL,F_TRAX_GDZKJE,F_TRAX_TSZKL,F_TRAX_TSZKJE,F_TRAX_TSZKDJP,
                         b.F_TRAX_SFSZ,b.F_TRAX_DDZKDJP,b.F_TRAX_XJZKDJP,b.F_TRAX_GDZKDJP,b.F_TRAX_HHZKBL,b.F_TRAX_DECIMAL1,b.F_TRAX_YBZKBL from t_PUR_PriceList a
                         inner join t_PUR_PriceListEntry b on a.fid=b.fid
                         inner join T_BD_MATERIAL wl on b.FMATERIALID=wl.FMATERIALID
                         INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=wl.FMASTERID
                         INNER JOIN t_BD_MaterialBase WLDW ON wl.FMATERIALID=WLDW.FMATERIALID                         
                         inner join t_BD_Supplier d on d.FSUPPLIERID=a.FSUPPLIERID
                         inner join T_ORG_Organizations e on e.forgid=a.FCREATEORGID
                         where d.FNumber='{gys["Number"]}' and wl.FNumber='{wl["Number"]}' 
                        and a.FDOCUMENTSTATUS='C' and e.FNUMBER='{cgzz["Number"]}'
                         ";
                        var hqdate = DBUtils.ExecuteDynamicObject(Context, hqsql);
                        if (hqdate.Count > 0)
                        {
                            this.Model.SetValue("F_TRAX_DDZKL", hqdate[0]["F_TRAX_DDZKL"], e.Row);
                            this.Model.SetValue("F_TRAX_DDZKJE", hqdate[0]["F_TRAX_DDZKJE"], e.Row);
                            this.Model.SetValue("F_TRAX_XJZKL", hqdate[0]["F_TRAX_XJZKL"], e.Row);
                            this.Model.SetValue("F_TRAX_XJZKJE", hqdate[0]["F_TRAX_XJZKJE"], e.Row);
                            this.Model.SetValue("F_TRAX_GDZKL", hqdate[0]["F_TRAX_GDZKL"], e.Row);
                            this.Model.SetValue("F_TRAX_GDZKJE", hqdate[0]["F_TRAX_GDZKJE"], e.Row);
                            this.Model.SetValue("F_TRAX_TSZKL", hqdate[0]["F_TRAX_TSZKL"], e.Row);
                            this.Model.SetValue("F_TRAX_TSZKJE", hqdate[0]["F_TRAX_TSZKJE"], e.Row);
                            this.Model.SetValue("F_TRAX_TSZKDJP", hqdate[0]["F_TRAX_TSZKDJP"], e.Row);

                            this.Model.SetValue("F_TRAX_SFSZ", hqdate[0]["F_TRAX_SFSZ"], e.Row);
                            this.Model.SetValue("F_TRAX_HHZKBL", hqdate[0]["F_TRAX_HHZKBL"], e.Row);
                            this.Model.SetValue("F_TRAX_XSCBL", hqdate[0]["F_TRAX_DECIMAL1"], e.Row);
                            this.Model.SetValue("F_TRAX_YBZKBL", hqdate[0]["F_TRAX_YBZKBL"], e.Row);
                            this.Model.SetValue("FTAXPRICE", hqdate[0]["FTAXPRICE"], e.Row);
                            this.Model.SetValue("F_TRAX_DDZKDJP", hqdate[0]["F_TRAX_DDZKDJP"], e.Row);
                            this.Model.SetValue("F_TRAX_XJZKDJP", hqdate[0]["F_TRAX_XJZKDJP"], e.Row);
                            this.Model.SetValue("F_TRAX_GDZKDJP", hqdate[0]["F_TRAX_GDZKDJP"], e.Row);
                            this.View.InvokeFieldUpdateService("FTAXPRICE", e.Row);
                        }
                    }

                }
            }
            else if (e.Field.FieldName == "FSUPPLIERID")
            {
                if (((DynamicObject)this.Model.GetValue("FBILLTYPEID")) != null && ((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Number"].ToString() == "CGDD01_SYS")
                {
                    var gys = ((DynamicObject)this.View.Model.GetValue("FSUPPLIERID"));//供应商
                    var cgzz = ((DynamicObject)this.View.Model.GetValue("FPURCHASEORGID"));//采购组织
                    var dates = (DynamicObjectCollection)this.Model.DataObject["POOrderEntry"];
                    if (dates.Count > 0)
                    {
                        foreach (var date in dates)
                        {
                            var wl = (DynamicObject)date["MaterialId"];//物料         
                            if (wl != null && gys != null)
                            {
                                //获取采购价目表的折扣率和金额
                                string hqsql = $@"/*dialect*/
                         select 
                         ROUND(CASE WHEN b.FUNITID != WLDW.FBASEUNITID THEN (FTAXPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE FTAXPRICE END,6) FTAXPRICE,
                         F_TRAX_DDZKL,F_TRAX_DDZKJE,F_TRAX_XJZKL,F_TRAX_XJZKJE,
                         F_TRAX_GDZKL,F_TRAX_GDZKJE,F_TRAX_TSZKL,F_TRAX_TSZKJE,F_TRAX_TSZKDJP,
                         b.F_TRAX_SFSZ,b.F_TRAX_DDZKDJP,b.F_TRAX_XJZKDJP,b.F_TRAX_GDZKDJP,b.F_TRAX_HHZKBL,b.F_TRAX_DECIMAL1,b.F_TRAX_YBZKBL from t_PUR_PriceList a
                         inner join t_PUR_PriceListEntry b on a.fid=b.fid
                         inner join T_BD_MATERIAL wl on b.FMATERIALID=wl.FMATERIALID
                         INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=wl.FMASTERID
                         INNER JOIN t_BD_MaterialBase WLDW ON wl.FMATERIALID=WLDW.FMATERIALID                         
                         inner join t_BD_Supplier d on d.FSUPPLIERID=a.FSUPPLIERID
                         inner join T_ORG_Organizations e on e.forgid=a.FCREATEORGID
                         where d.FNumber='{gys["Number"]}' and wl.FNumber='{wl["Number"]}' 
                        and a.FDOCUMENTSTATUS='C' and e.FNUMBER='{cgzz["Number"]}'
                         ";
                                var hqdate = DBUtils.ExecuteDynamicObject(Context, hqsql);
                                if (hqdate.Count > 0)
                                {
                                    this.Model.SetValue("F_TRAX_DDZKL", hqdate[0]["F_TRAX_DDZKL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_DDZKJE", hqdate[0]["F_TRAX_DDZKJE"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_XJZKL", hqdate[0]["F_TRAX_XJZKL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_XJZKJE", hqdate[0]["F_TRAX_XJZKJE"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_GDZKL", hqdate[0]["F_TRAX_GDZKL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_GDZKJE", hqdate[0]["F_TRAX_GDZKJE"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_TSZKL", hqdate[0]["F_TRAX_TSZKL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_TSZKJE", hqdate[0]["F_TRAX_TSZKJE"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_TSZKDJP", hqdate[0]["F_TRAX_TSZKDJP"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_SFSZ", hqdate[0]["F_TRAX_SFSZ"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_HHZKBL", hqdate[0]["F_TRAX_HHZKBL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_XSCBL", hqdate[0]["F_TRAX_DECIMAL1"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_YBZKBL", hqdate[0]["F_TRAX_YBZKBL"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("FTaxPrice", hqdate[0]["FTAXPRICE"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_DDZKDJP", hqdate[0]["F_TRAX_DDZKDJP"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_XJZKDJP", hqdate[0]["F_TRAX_XJZKDJP"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.Model.SetValue("F_TRAX_GDZKDJP", hqdate[0]["F_TRAX_GDZKDJP"], Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.View.InvokeFieldUpdateService("FTAXPRICE", Convert.ToInt32(date["Seq"].ToString()) - 1);
                                    this.View.UpdateView("FTaxPrice");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
