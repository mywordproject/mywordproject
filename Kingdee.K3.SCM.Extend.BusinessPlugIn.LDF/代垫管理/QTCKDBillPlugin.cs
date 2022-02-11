using ingdee.K3.SCM.Extend.BusinessPlugIn;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF.代垫管理
{
    [Description("其他出库单--通过品牌带出成本价"),HotUpdate]
    public class QTCKDBillPlugin:AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.FieldName == "FMATERIALID")
            {
                if(((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Number"].ToString()== "QTCKD07_SYS")
                {
                    var wls = (DynamicObject)this.Model.GetValue("FMATERIALID");
                   
                    var gys = (DynamicObject)this.Model.GetValue("F_TRAX_SUPPLIER");
                    //FCOMBRANDID_CMK F_TRAX_BRAND
                    if (gys != null && wls!=null)
                    {
                     var wl = Utils.LoadBDData(Context, "BD_MATERIAL", ((DynamicObject)this.Model.GetValue("FMATERIALID"))["Number"].ToString());
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
                            select a.F_TRAX_MRCB,
                            ROUND(CASE WHEN a.F_TRAX_DENOMINATIONUNIT=WLDW.FBASEUNITID THEN (a.F_TRAX_MRCB/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.F_TRAX_MRCB END,6) MRCB 
                            from
                            T_SON_DPMRCBentity a
                            inner join  T_SON_DPMRCB b on a.FID=b.FID
                            INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=a.F_TRAX_WLBM
                            INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                            INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                            where F_TRAX_WLBM='{wls["Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                                    var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                                    if (mrcb.Count > 0)
                                    {
                                        this.Model.SetValue("F_TRAX_PREPAIDPRICE", mrcb[0]["MRCB"].ToString(), e.Row);
                                        this.Model.SetValue("F_TRAX_MRCB", mrcb[0]["F_TRAX_MRCB"].ToString(), e.Row);

                                    }

                                }
                                else if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 1)
                                {
                                    string cgjmbsql = $@"/*dialect*/
                            select b.FTAXPRICE,
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
                                        this.Model.SetValue("F_TRAX_PREPAIDPRICE", date[0]["MRCB"].ToString(), e.Row);
                                        this.Model.SetValue("F_TRAX_MRCB", date[0]["FTAXPRICE"].ToString(), e.Row);
                                    }
                                }

                            }

                        }
                    }
                    
                }
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z")
            {
                if (((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Number"].ToString() == "QTCKD07_SYS")
                {
                    var gys = (DynamicObject)this.Model.GetValue("F_TRAX_SUPPLIER");
                    var dates = this.Model.DataObject["BillEntry"] as DynamicObjectCollection;
                    if (dates.Count > 0)
                    {
                        foreach (var date in dates)
                        {
                            if (gys != null && date["MaterialId"] != null)
                            {
                                var wl = Utils.LoadBDData(Context, "BD_MATERIAL", ((DynamicObject)date["MaterialId"])["Number"].ToString());
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
                            select * from  T_SON_DPMRCBentity a
                            inner join  T_SON_DPMRCB b on a.FID=b.FID  
                            where F_TRAX_WLBM='{date["MaterialId_Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                                            var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                                            if (mrcb.Count > 0)
                                            {
                                                this.Model.SetValue("F_TRAX_PREPAIDPRICE", mrcb[0]["F_TRAX_MRCB"].ToString(), Convert.ToInt32(date["Seq"].ToString()) - 1);
                                                this.Model.SetValue("F_TRAX_DDJE", Convert.ToDouble(date["Qty"].ToString()) * Convert.ToDouble(mrcb[0]["F_TRAX_MRCB"].ToString()), Convert.ToInt32(date["Seq"].ToString()) - 1);
                                            }

                                        }
                                        else if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 1)
                                        {
                                            string cgjmbsql = $@"/*dialect*/
                                        select  FTAXPRICE from t_PUR_PriceList a
                                        inner join t_PUR_PriceListEntry b on a.fid=b.fid
                                        where FSUPPLIERID='{da["F_TRAX_JGSUPPLIER"]}' and FMATERIALID='{date["MaterialId_Id"].ToString()}' and a.FDOCUMENTSTATUS='C'";
                                            var cgjmb = DBUtils.ExecuteDynamicObject(Context, cgjmbsql);
                                            if (cgjmb.Count > 0)
                                            {
                                                this.Model.SetValue("F_TRAX_PREPAIDPRICE", cgjmb[0]["FTAXPRICE"].ToString(), Convert.ToInt32(date["Seq"].ToString()) - 1);
                                                this.Model.SetValue("F_TRAX_DDJE", Convert.ToDouble(date["Qty"].ToString()) * Convert.ToDouble(cgjmb[0]["FTAXPRICE"].ToString()), Convert.ToInt32(date["Seq"].ToString()) - 1);
                                            }
                                        }

                                    }

                                }
                            }

                        }
                    }
                }
            }
        }
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            if (((DynamicObject)this.Model.GetValue("FBILLTYPEID"))["Number"].ToString() == "QTCKD07_SYS")
            {
                var dates = this.Model.DataObject["BillEntry"] as DynamicObjectCollection;
                foreach (var date in dates)
                {
                    if (Convert.ToDouble(date["F_TRAX_PrepaidPrice"]) == 0)
                    {
                        throw new KDBusinessException("", "代垫单价为0不允许保存");
                    }
                }
            }
        }
    }
}
