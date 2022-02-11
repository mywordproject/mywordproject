using ingdee.K3.SCM.Extend.BusinessPlugIn;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [Description("采购退货订单---获取采购折扣率和折扣金额")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGTHBillPlugin : AbstractBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
         }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z")
            {
                if (((DynamicObject)this.Model.GetValue("FBILLTYPEID")) != null)
                {
                    if (((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Id"].ToString() == "61ef92c48d640b")
                    {
                        var gys = (DynamicObject)this.Model.GetValue("FSUPPLIERID");//供应商
                        var cgthdate = this.Model.DataObject["POOrderEntry"] as DynamicObjectCollection;
                        foreach (var da in cgthdate)
                        {
                            string wl = ((DynamicObject)da["MaterialId"])["Number"].ToString();//物料                       
                            string ph = ((DynamicObject)da["FLot"])["Number"].ToString();//批号  
                            int hh = Convert.ToInt32(da["Seq"].ToString());//行号
                                                                           //通过采购入库单获取采购订单号
                            string cgrksql = $@"/*dialect*/
                    SELECT FSRCBILLNO from t_STK_InStock a
                    inner join T_STK_INSTOCKENTRY b on a.FID = b.FID
					inner join T_BD_MATERIAL C ON  C.FMATERIALID=b.FMATERIALID
					inner join t_BD_Supplier d on a.FSUPPLIERID=d.FSUPPLIERID
					inner join T_BD_LOTMASTER e on e.flotid=b.FLOT
                     where C.fNumber = '{wl}' and e.FNUMBER = '{ph}' and d.fnumber = '{gys["Number"].ToString()}'";
                            var cgrkdate = DBUtils.ExecuteDynamicObject(Context, cgrksql);
                            if (cgrkdate.Count > 0)
                            {
                                string cgddsql = $@"/*dialect*/
                           SELECT 
                           ROUND(CASE WHEN b.FUNITID != WLDW.FBASEUNITID THEN (f.FTAXPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE f.FTAXPRICE END,6) FTAXPRICE,
                           F_TRAX_DDZKL,F_TRAX_XJZKL,F_TRAX_GDZKL,F_TRAX_TSZKL,b.F_TRAX_DDZKDJP,b.F_TRAX_XJZKDJP,b.F_TRAX_GDZKDJP
                           from t_PUR_POOrder a
                           inner join t_PUR_POOrderEntry b on a.FID=b.FID
                           INNER JOIN T_PUR_POORDERENTRY_F f ON b.FENTRYID=F.FENTRYID
                           INNER JOIN T_BD_MATERIAL WL
                           ON WL.FMATERIALID=b.FMATERIALID
                           INNER JOIN T_BD_UNITCONVERTRATE WLHS
                           ON WLHS.FMASTERID=WL.FMASTERID
                           INNER JOIN t_BD_MaterialBase WLDW
                           ON WL.FMATERIALID=WLDW.FMATERIALID
                           where a.fbillno ='{cgrkdate[0]["FSRCBILLNO"].ToString()}' and  WL.fNumber = '{wl}' ";
                                var cgdddate = DBUtils.ExecuteDynamicObject(Context, cgddsql);
                                foreach (var date in cgdddate)
                                {
                                    this.Model.SetValue("FTAXPRICE", date["FTAXPRICE"].ToString(), hh - 1);
                                    this.Model.SetValue("F_TRAX_DDZKL", date["F_TRAX_DDZKL"].ToString(), hh - 1);
                                    //this.Model.SetValue("F_TRAX_DDZKJE", date["F_TRAX_DDZKJE"].ToString(), hh-1);
                                    this.Model.SetValue("F_TRAX_XJZKL", date["F_TRAX_XJZKL"].ToString(), hh - 1);
                                    //this.Model.SetValue("F_TRAX_XJZKJE", date["F_TRAX_XJZKJE"].ToString(), hh-1);
                                    this.Model.SetValue("F_TRAX_GDZKL", date["F_TRAX_GDZKL"].ToString(), hh - 1);
                                    //this.Model.SetValue("F_TRAX_GDZKJE", date["F_TRAX_GDZKJE"].ToString(), hh-1);
                                    this.Model.SetValue("F_TRAX_TSZKL", date["F_TRAX_TSZKL"].ToString(), hh - 1);
                                    //this.Model.SetValue("F_TRAX_TSZKJE", date["F_TRAX_TSZKJE"].ToString(), hh-1);
                                    this.Model.SetValue("F_TRAX_DDZKDJP", date["F_TRAX_DDZKDJP"].ToString(), hh - 1);
                                    this.Model.SetValue("F_TRAX_XJZKDJP", date["F_TRAX_XJZKDJP"].ToString(), hh - 1);
                                    this.Model.SetValue("F_TRAX_GDZKDJP", date["F_TRAX_GDZKDJP"].ToString(), hh - 1);
                                    this.Model.SetValue("F_TRAX_PURCHASEORDERNUMBER", cgrkdate[0]["FSRCBILLNO"].ToString(), hh - 1);

                                }
                            }
                        }
                    }
                }
            }
        }
  
    }
}
