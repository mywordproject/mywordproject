using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using System.Linq;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF.代垫管理
{
    [Description("其他出库单单保存后计算"), HotUpdate]
    public class QTCKDBCJS : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FMaterialId");//物料编码
            e.FieldKeys.Add("FUnitID");//单位
            e.FieldKeys.Add("F_TRAX_PrepaidPrice");//代垫单价
            e.FieldKeys.Add("F_TRAX_MRCB");//默认成本价
        }
        /// <summary>
        /// 操作结束后功能处理
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (DynamicObject o in e.DataEntitys)
            {
                var DJBH = o["BillNo"].ToString();

                DynamicObjectCollection a = o["BillEntry"] as DynamicObjectCollection;//明细信息
                foreach (var item in a)
                {
                    var WLID = item["MATERIALID_Id"].ToString();
                    var DWID = item["UnitID_Id"].ToString();
                    string cbsql = $@"/*dialect*/select a.FMATERIALID,b.FID,
                    ROUND(CASE WHEN a.FUNITID!=WLDW.FBASEUNITID THEN (a.F_TRAX_MRCB/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.F_TRAX_MRCB END,6) MRCB
                    from  T_STK_MISDELIVERYENTRY a
                    inner join  T_STK_MISDELIVERY b on a.FID=b.FID
                    INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=a.FMATERIALID
                    INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=WL.FMASTERID
                    INNER JOIN t_BD_MaterialBase WLDW ON WL.FMATERIALID=WLDW.FMATERIALID
                    where a.FMATERIALID='{WLID}'";
                    var mrcb = DBUtils.ExecuteDynamicObject(Context, cbsql);
                    foreach (var b in mrcb)
                    {
                        var FMATERIALID = b["FMATERIALID"].ToString();
                        var MRCB = b["MRCB"].ToString();
                        string gxsql = $@"/*dialect*/UPDATE T_STK_MISDELIVERYENTRY SET F_TRAX_PREPAIDPRICE ='{MRCB}' WHERE FMATERIALID='{FMATERIALID}' AND fid ={b["FID"]}";
                        DBUtils.Execute(Context, gxsql);
                    }
                }
            }
        }
    }
}
