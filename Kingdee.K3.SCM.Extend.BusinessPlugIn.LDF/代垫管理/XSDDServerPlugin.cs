using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF.代垫管理
{
    [Description("销售订单--删除时，删除买点或者扣率记录"),HotUpdate]
    public class XSDDServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FDate","FBillNo","FCustId","FMaterialId"};
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
                var entitys = date["SaleOrderEntry"] as DynamicObjectCollection;
                foreach (var entity in entitys)
                {
                    string klsql = $@"/*dialect*/
                      select * from T_SON_KVcontract a
                      inner join T_SON_KVcontractEntry b on a.FID=b.FID
                      LEFT join T_SON_SYcontractEntry c on b.FENTRYID=c.FENTRYID
                      WHERE F_TRAX_MATERIALID='{entity["MaterialId_Id"].ToString()}' 
                      AND F_TRAX_CUSTOMER='{date["CustId_Id"]}' and a.FDOCUMENTSTATUS='C'
                      and F_TRAX_STARTDATE<=to_date('{date["Date"]}','yyyy-mm-dd hh24:mi:ss') 
                     and F_TRAX_ENDDATE>=to_date('{date["Date"]}','yyyy-mm-dd hh24:mi:ss')
                     And F_TRAX_DDBH='{date["BillNo"]}' and F_TRAX_DDHH='{entity["Seq"]}'";
                    var klhtdate = DBUtils.ExecuteDynamicObject(Context, klsql);
                    if (klhtdate.Count > 0)
                    {
                        string delsql = $@"/*dialect*/
                        delete T_SON_SYcontractEntry where FDETAILID='{klhtdate[0]["FDETAILID"] }'";
                        DBUtils.Execute(Context, delsql);
                     }
                    else
                    {
                        //买点合同
                        string mdsql = $@"/*dialect*/
                    select * from T_SON_MDHT a
                    inner join T_SON_MDHTentity b on a.FID=b.FID
                    left JOIN T_SON_SYXXbEntity c on b.FENTRYID=c.FENTRYID
                    WHERE F_TRAX_WLBM='{entity["MaterialId_Id"].ToString()}' 
                   AND F_TRAX_CUSTOMER='{date["CustId_Id"]}' and a.FDOCUMENTSTATUS='C'
                    and F_TRAX_SXRQM<=to_date('{date["Date"]}','yyyy-mm-dd hh24:mi:ss') 
                    and  F_TRAX_SXRQQM>=to_date('{date["Date"]}','yyyy-mm-dd hh24:mi:ss')
                     And F_TRAX_DDBH='{date["BillNo"]}' and F_TRAX_DDHH='{entity["Seq"]}'";
                        var mdhtdate = DBUtils.ExecuteDynamicObject(Context, mdsql);
                        if (mdhtdate.Count > 0)
                        {
                            string delesql = $@"/*dialect*/
                        delete T_SON_SYXXbEntity where FDETAILID='{mdhtdate[0]["FDETAILID"] }'";
                            DBUtils.Execute(Context, delesql);
                        }
                    }
                }
            }
        }
    }
}
