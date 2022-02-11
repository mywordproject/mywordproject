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


namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("销售价目表特价-审核更新失效状态"), HotUpdate]
    public class XSJMBTJ : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FPriceType");//价格类型
            e.FieldKeys.Add("FMaterialId");//物料编码
            e.FieldKeys.Add("FEntryForbidStatus");//失效
            e.FieldKeys.Add("FCustID");//客户
        }
        /// <summary>
        /// 操作结束后功能处理
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (DynamicObject o in e.DataEntitys)
            {
                DynamicObject PriceType= o["PriceType"] as DynamicObject;
                if ((o["PriceType"] as DynamicObject)["FDataValue"].ToString()=="特价")
                {
                    var DJBH = o["Number"].ToString();
                    var XSZZID = (o["CreateOrgId"] as DynamicObject)["id"].ToString();
                    DynamicObjectCollection KH = o["SAL_APPLYCUSTOMER"] as DynamicObjectCollection;
                    foreach (var item in KH)
                    {
                        var KHBM = (item["CustID"] as DynamicObject)["Number"].ToString();
                        string jtkh = $@"SELECT FGROUPCUSTID FROM T_BD_CUSTOMER
WHERE FNUMBER='{KHBM}'
AND FISGROUP='1'
AND FUSEORGID='{XSZZID}'";
                        var data = DBUtils.ExecuteDynamicObject(Context, jtkh);
                        if (data.Count > 0)
                        {

                            var DYKHID = data[0]["FGROUPCUSTID"];
                            string strs = "";
                            if (DYKHID != null)
                            {
                                string WLBM = $@"SELECT DISTINCT a.FCUSTID,a.FNUMBER FROM T_BD_CUSTOMER a
                                INNER JOIN T_SAL_APPLYCUSTOMER SYKH ON a.FCUSTID=SYKH.FCUSTID
                                INNER JOIN T_SAL_PRICELISTENTRY JGMX ON  SYKH.FID=SYKH.FID
                                INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=JGMX.FMATERIALID
                                WHERE a.FGROUPCUSTID='{DYKHID}'
                                AND a.FUSEORGID='{XSZZID}'";
                                var date = DBUtils.ExecuteDynamicObject(Context, WLBM);
                                foreach (var kh in date)
                            {
                                    strs += kh["FCUSTID"].ToString()+",";
                            }
                                var jgmxs = o["SAL_PRICELISTENTRY"] as DynamicObjectCollection;
                                string str = strs.Trim(',');
                                foreach (var jgmx in jgmxs)
                                {
                                    string ID = $@"
                                     UPDATE T_SAL_PRICELISTENTRY SET FFORBIDSTATUS='B' 
                                     WHERE FMATERIALID = '{jgmx["MaterialId_Id"]}'
                                     and FENTRYID IN(SELECT b.FENTRYID FROM T_SAL_PRICELIST a
                                     INNER JOIN T_SAL_PRICELISTENTRY b ON a.FID=b.FID
                                     INNER JOIN T_SAL_APPLYCUSTOMER C ON A.FID=C.FID
                                     INNER JOIN T_ORG_Organizations D ON A.FSALEORGID=D.FORGID
                                     WHERE a.FNUMBER!='{DJBH}'
                                     AND 
                                     C.FCUSTID IN ({str}))";
                                    DBUtils.Execute(Context, ID);
                                }
                        }

                        }
                    }
                   
                }
            }
        }
    }
}
