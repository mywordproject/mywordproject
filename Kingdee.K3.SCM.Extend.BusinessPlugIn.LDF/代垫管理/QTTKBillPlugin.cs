using ingdee.K3.SCM.Extend.BusinessPlugIn;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.代垫管理
{
    [Description("其他入库单--通过品牌带出成本价")]
    //热启动,不用重启IIS
    [HotUpdate]
    public class QTTKBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.FieldName == "FMATERIALID")
            {
                var wls = (DynamicObject)this.Model.GetValue("FMATERIALID");
                var wl = Utils.LoadBDData(Context, "BD_MATERIAL",((DynamicObject)this.Model.GetValue("FMATERIALID"))["Number"].ToString());
                var gys = (DynamicObject)this.Model.GetValue("FSUPPLIERID");
                //FCOMBRANDID_CMK F_TRAX_BRAND
                if (gys != null)
                {
                    string ppsql = $@"/*dialect*/
                    select * from  T_BD_Brand a
                    inner join T_BD_XGSupplierEntity b on a.FID=b.FID 
                    where a.FID='{ wl["F_TRAX_Brand_Id"].ToString()}' 
                    and a.FDOCUMENTSTATUS='C'
                    ";
                    var DATE = DBUtils.ExecuteDynamicObject(Context, ppsql);
                    if (DATE.Count > 0)
                    {
                        foreach (var da in DATE)
                        {//0 单品默认成本，1 指定供应商采购标准价
                            if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 0)
                            {
                                //默认成本
                                string mrcbsql = $@"/*dialect*/
                            select F_TRAX_MRCB,
                            ROUND(CASE WHEN a.F_TRAX_MRCB=WLDW.FBASEUNITID THEN (a.F_TRAX_MRCB/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.F_TRAX_MRCB END,6) MRCB
                            from  T_SON_DPMRCBentity a
                            inner join  T_SON_DPMRCB b on a.FID=b.FID
                            INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=a.F_TRAX_WLBM
                            INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                            INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                            where F_TRAX_WLBM='{wls["Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                                var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                                if (mrcb.Count > 0)
                                {
                                    this.Model.SetValue("FPRICE", mrcb[0]["MRCB"].ToString(), e.Row);
                                    this.Model.SetValue("F_TRAX_MRCB", mrcb[0]["F_TRAX_MRCB"].ToString(), e.Row);
                                }

                            }
                            else if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 1)
                            {
                                string cgjmbsql = $@"/*dialect*/
                            select
                            ROUND(CASE WHEN b.FUNITID!=WLDW.FBASEUNITID THEN (b.FTAXPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE b.FTAXPRICE END,6) MRCB
                            from t_PUR_PriceList a
                            inner join t_PUR_PriceListEntry b on a.fid=b.fid
                            INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=b.FMATERIALID
                            INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                            INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                            where FSUPPLIERID='{da["F_TRAX_JGSUPPLIER"].ToString()}' and FMATERIALID='{wls["Id"]}' and a.FDOCUMENTSTATUS='C'";
                                var date = DBUtils.ExecuteDynamicObject(Context, cgjmbsql);
                                if (date.Count > 0)
                                {
                                    this.Model.SetValue("FPRICE", date[0]["MRCB"].ToString(), e.Row);
                                }
                            }

                        }

                    }
                }
                    
                

            }
        }
    }
}
