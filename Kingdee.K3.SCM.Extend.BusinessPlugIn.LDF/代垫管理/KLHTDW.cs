using ingdee.K3.SCM.Extend.BusinessPlugIn;
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

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF.代垫管理
{
    [Description("扣率合同单位"),HotUpdate]
    public class KLHTDW:AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_TRAX_MaterialId")
            {
                var wl = (DynamicObject)this.Model.GetValue("F_TRAX_MATERIALID", e.Row);//物料
                var w = Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(wl["Id"].ToString()));
                var kh = (DynamicObject)this.Model.GetValue("F_TRAX_CUSTOMER", e.Row);//客户  


                string mrcbsql = $@"/*dialect*/
                 select a.F_TRAX_DENOMINATIONUNIT,dw.FNUMBER
                 from  T_SON_DPMRCBentity a
                 inner join T_SON_DPMRCB b on a.FID=b.FID
                 INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=a.F_TRAX_WLBM
                 INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                 INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                 INNER JOIN T_BD_UNIT dw on a.F_TRAX_DENOMINATIONUNIT=dw.FUNITID
                 where a.F_TRAX_WLBM='{wl["Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                if (mrcb.Count > 0)
                {
                    this.Model.SetItemValueByID("F_TRAX_JJDW", mrcb[0]["F_TRAX_DENOMINATIONUNIT"], e.Row);
                    //this.Model.SetValue("F_TRAX_JJDW", mrcb[0]["F_TRAX_DENOMINATIONUNIT"], e.Row);
                    //View.Model.SetItemValueByNumber("F_TRAX_JJDW", mrcb[0]["FNUMBER"].ToString(), e.Row);
                    View.InvokeFieldUpdateService("F_TRAX_JJDW", e.Row);
                }
            }
        }
    }
}
