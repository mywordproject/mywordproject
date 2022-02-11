using Kingdee.BOS;
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
    [Description("扣率合同--通过物料带出成本价")]
    //热启动,不用重启IIS
    [HotUpdate]
    public class KLHTBillPlugin: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if(e.Field.FieldName== "F_TRAX_MATERIALID")
            {
                var wl = (DynamicObject)this.Model.GetValue("F_TRAX_MATERIALID", e.Row);//物料
                var kh = (DynamicObject)this.Model.GetValue("F_TRAX_CUSTOMER");//客户
                if(kh==null)
                {
                   /// throw new KDBusinessException("", "客户不能为空！");
                    this.View.ShowMessage("客户不能为空");
                }
                if(this.Model.GetValue("F_TRAX_STARTDATE")==null || this.Model.GetValue("F_TRAX_ENDDATE").ToString() == null)
                {
                    throw new KDBusinessException("", "开始日期和结束日期不能为空！");
                }
                DateTime ksrq = DateTime.Parse(this.Model.GetValue("F_TRAX_STARTDATE").ToString());//开始日期
                DateTime jsrq = DateTime.Parse(this.Model.GetValue("F_TRAX_ENDDATE").ToString());//结束日期
                //查询销售价目表是否有该物料作为特价
                string xsjmbsql = $@"/*dialect*/
                       select c.* from T_SAL_PRICELIST a
                                INNER JOIN T_SAL_PRICELISTENTRY b on a.FID = b.FID
                                inner join T_SAL_APPLYCUSTOMER c on a.FID = c.FID
				                inner join T_BD_CUSTOMER d on d.FCUSTID=C.FCUSTID
				                INNER JOIN T_BD_MATERIAL e ON e.FMATERIALID=B.FMATERIALID
                                inner join T_BAS_ASSISTANTDATAENTRY f on f.FENTRYID=a.FPRICETYPE
					            inner join T_BAS_ASSISTANTDATA g on f.FID=g.FID 
                                where b.FMATERIALID='{wl["Id"].ToString()}' and f.FNUMBER='02'  and g.FNUMBEr='SAL_PriceType' and
                                 c.FCUSTID='{kh["Id"].ToString()}' and a.FDOCUMENTSTATUS='C'and  a.FFORBIDSTATUS='A' AND b.FFORBIDSTATUS='A' 
                and  ((b.FEFFECTIVEDATE<=to_date('{ksrq}','yyyy-mm-dd hh24:mi:ss') and b.FEXPRIYDATE>=to_date('{ksrq}','yyyy-mm-dd hh24:mi:ss')) or
                      (b.FEFFECTIVEDATE<=to_date('{jsrq}','yyyy-mm-dd hh24:mi:ss') and b.FEXPRIYDATE>=to_date('{jsrq}','yyyy-mm-dd hh24:mi:ss')))
                        ";
                var xsjmbdate = DBUtils.ExecuteDynamicObject(Context, xsjmbsql);
                if (xsjmbdate.Count > 0)
                {
                    throw new KDBusinessException("", "该物料在销售价目表有特价");
                }
                //判断买点合同是否有该物料
                //买点合同
                string mdsql = $@"/*dialect*/
                    select * from T_SON_MDHT a
                    inner join T_SON_MDHTentity b on a.FID=b.FID
                    WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C'
              and  ((a.F_TRAX_SXRQM<=to_date('{ksrq}','yyyy-mm-dd hh24:mi:ss') and a.F_TRAX_SXRQQM>=to_date('{ksrq}','yyyy-mm-dd hh24:mi:ss')) or
                  (a.F_TRAX_SXRQM<=to_date('{jsrq}','yyyy-mm-dd hh24:mi:ss') and    a.F_TRAX_SXRQQM>=to_date('{jsrq}','yyyy-mm-dd hh24:mi:ss')))                
                    ";
               // a.F_TRAX_SXRQQM < to_date('{ksrq}', 'yyyy-mm-dd hh24:mi:ss') or a. F_TRAX_SXRQM > to_date('{jsrq}', 'yyyy-mm-dd hh24:mi:ss') )
                var mdhtdate = DBUtils.ExecuteDynamicObject(Context, mdsql);
                if (mdhtdate.Count > 0)
                {
                    throw new KDBusinessException("", "该物料在买点合同里面有！");
                }
                    //默认成本
                 string mrcbsql = $@"/*dialect*/
                 select
                 ROUND(CASE WHEN a.F_TRAX_DENOMINATIONUNIT=WLDW.FBASEUNITID THEN (a.F_TRAX_MRCB/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.F_TRAX_MRCB END,6) MRCB 
                 from  T_SON_DPMRCBentity a
                 inner join  T_SON_DPMRCB b on a.FID=b.FID
                 INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=a.F_TRAX_WLBM
                 INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                 INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                 where F_TRAX_WLBM='{wl["Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                if (mrcb.Count > 0)
                {
                    this.Model.SetValue("F_TRAX_DEFAULTCOST", mrcb[0]["MRCB"], e.Row);
                }
               
            }
        }
    }
}
