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

namespace kingdee.CD.XTF.Business.PlugIn.Until
{
    [Description("其他应收单删除后，清空折扣表反写信息"),HotUpdate]
    public class DELTEZKB: AbstractOperationServicePlugIn
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
                string sql;

                //当前单据对应上游的所有EntryId
                sql = $@"
select FENTRYID
from T_SAL_KHZKBENTRY_SYJL
where F_TRAX_DJLX='其他应收单' 
and F_TRAX_DJBH='{o["BillNo"].ToString()}'
";
                DynamicObjectCollection allEntryIdCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);
                List<string> allEntryIdList = allEntryIdCollection.Select(c => c.GetVal<string>("FENTRYID")).ToList();
                if (allEntryIdList.IsEmpty()) return;

                //折扣明细
                DynamicObjectCollection discountCollection = o.GetVal<DynamicObjectCollection>("SaleOrderDiscount");

                //删除折扣记录
                sql = $@"
delete from T_SAL_KHZKBENTRY_SYJL 
where F_TRAX_DJLX='其他应收单' and F_TRAX_DJBH='{o["BillNo"].ToString()}'
";
                DBUtils.Execute(this.Context, sql);

                //汇总
                //List<long> entryIdList = discountCollection.GetColumns<long>("F_TRAX_YDFLID");
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
