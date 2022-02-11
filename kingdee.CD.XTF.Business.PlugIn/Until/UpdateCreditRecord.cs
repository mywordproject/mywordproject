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
using YYLK;

namespace Kingdee.CD.STGYL.Plugins.PUR_ReceiveBill
{
    [Description("其他应收单保存后反写客户折扣表"), HotUpdate]
    public class UpdateCreditRecord : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBillNo");//单据编号
            e.FieldKeys.Add("FBillTypeID");//单据类型
            e.FieldKeys.Add("F_TRAX_CKYBL");//厂家折扣比例
            e.FieldKeys.Add("F_TRAX_CSYJE");//厂家折扣金额
            e.FieldKeys.Add("F_TRAX_GKYBL");//公司折扣比例
            e.FieldKeys.Add("F_TRAX_GSYJE");//公司使用金额
            e.FieldKeys.Add("F_TRAX_YDFLID");//源分录ID
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
                var DJLX = (o["BillTypeID"] as DynamicObject)["Name"].ToString();
                var CJRQ = o["CreateDate"].ToString();
                DynamicObjectCollection DiscountEntity = o["DiscountEntity"] as DynamicObjectCollection;//其他应收折扣表信息
                List<long> entryIdList = DiscountEntity.GetColumns<long>("F_TRAX_YDFLID");
                foreach (var item in DiscountEntity)
                {
                    //更新
                    string sql = $@"/*dialect*/
                        update T_SAL_KHZKBENTRY_SYJL set F_TRAX_CJZKBL='{item["F_TRAX_CKYBL"].ToString()}',
                        F_TRAX_CJZKJE='{item["F_TRAX_CSYJE"].ToString()}',
                        F_TRAX_GSZKBL='{item["F_TRAX_GKYBL"].ToString()}',
                        F_TRAX_GSZKJE='{item["F_TRAX_GSYJE"].ToString()}',
                        F_TRAX_DJSJ='{CJRQ}'
                        where FENTRYID='{item["F_TRAX_YDFLID"].ToString()}'
                        and F_TRAX_DJLX = '其他应收单'
                        and F_TRAX_DJBH = '{DJBH}'";
                    if (DBUtils.Execute(this.Context, sql) < 1)
                    {
                        sql = $@"insert into T_SAL_KHZKBENTRY_SYJL(
FDETAILID,
FENTRYID,
F_TRAX_DJLX,
F_TRAX_DJBH,
F_TRAX_CJZKBL,
F_TRAX_CJZKJE,
F_TRAX_GSZKBL,
F_TRAX_GSZKJE,
F_TRAX_DJSJ)
values(
seq_yxk.nextval,
{item["F_TRAX_YDFLID"].ToString()} ,
'其他应收单',
'{DJBH}',
{item["F_TRAX_CKYBL"].ToString()},
{item["F_TRAX_CSYJE"].ToString()},
{item["F_TRAX_GKYBL"].ToString()},
{item["F_TRAX_GSYJE"].ToString()},
'{CJRQ}')
";
                        DBUtils.Execute(this.Context, sql);
                    }
                    //当前单据对应上游的所有EntryId
                    sql = $@"
select FENTRYID
from T_SAL_KHZKBENTRY_SYJL
where F_TRAX_DJLX='其他应收单' 
and F_TRAX_DJBH='{DJBH}'
";
                    DynamicObjectCollection allEntryIdCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    List<string> allEntryIdList = allEntryIdCollection.Select(c => c.GetVal<string>("FENTRYID")).ToList();
                    if (allEntryIdList.IsEmpty()) return;

                    if (entryIdList.HasValue())
                    {
                        //删除为折扣金额为0的记录
                        sql = $@"
delete from T_SAL_KHZKBENTRY_SYJL 
where F_TRAX_CJZKJE=0 
and F_TRAX_GSZKJE=0
and F_TRAX_DJLX='其他应收单' 
and F_TRAX_DJBH='{DJBH}'
";
                        DBUtils.Execute(this.Context, sql);
                    }
                    else
                    {
                        //删除折扣记录
                        sql = $@"
delete from T_SAL_KHZKBENTRY_SYJL 
where F_TRAX_DJLX='其他应收单' 
and F_TRAX_DJBH='{DJBH}'
";
                        DBUtils.Execute(this.Context, sql);
                    }
                    //汇总
                    sql = $@"/*dialect*/
update
(
select a.FENTRYID,a.F_TRAX_CZKZE,a.F_TRAX_CSYKYJE,a.F_TRAX_GZKZE,a.F_TRAX_GSYKYJE,nvl(b.F_TRAX_CJZKJE,0) F_TRAX_CJZKJE,nvl(b.F_TRAX_GSZKJE,0) F_TRAX_GSZKJE
from T_SAL_KHZKBENTRY a
left join
(
select FENTRYID,sum(F_TRAX_CJZKJE) F_TRAX_CJZKJE,sum(F_TRAX_GSZKJE) F_TRAX_GSZKJE 
from T_SAL_KHZKBENTRY_SYJL
group by FENTRYID
) b on a.FENTRYID=b.FENTRYID
where a.FENTRYID in ({string.Join(",", allEntryIdList)})
) t
set t.F_TRAX_CSYKYJE=t.F_TRAX_CZKZE-t.F_TRAX_CJZKJE,
t.F_TRAX_GSYKYJE=t.F_TRAX_GZKZE-t.F_TRAX_GSZKJE
";
                    DBUtils.Execute(this.Context, sql);
                }
            }
        }
    }
}