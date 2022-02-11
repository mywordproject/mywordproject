using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.ServicePlugIn
{
    [Description("销售订单新变更单-审核、反审核-反写客户折扣池")]
    [HotUpdate]
    public class XSDDXBGDOperationServicePlugIn4AuditUnAudit : CommonOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FBILLNO");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);

            if (FormOperation.Operation.EqualsIgnoreCase("Audit"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //当前单据折扣明细
                    string billNo = dataEntity.GetVal<string>("BillNo");
                    sql = $@"
select b.FENTRYID,b.FSEQ,b.F_TRAX_CSYJE,b.F_TRAX_GSYJE
from T_SAL_XORDER a
inner join T_SAL_Discount b
on a.FID= b.FID
where FBILLNO = '{billNo}'
order by b.FSEQ
";
                    DynamicObjectCollection discountCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    //源单折扣明细，减去释放金额
                    string sourceBillNo = billNo.Split('_')[0];
                    sql = $@"
select b.FENTRYID,b.FSEQ,b.F_TRAX_CSYJE,b.F_TRAX_GSYJE
from T_SAL_ORDER a
inner join T_SAL_Discount b
on a.FID=b.FID
where FBILLNO='{sourceBillNo}'
order by b.FSEQ
";
                    DynamicObjectCollection sourceDiscountCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    //更新源单折扣明细
                    for (int i = 0; i < sourceDiscountCollection.Count; i++)
                    {
                        DynamicObject discount = discountCollection[i];
                        DynamicObject sourceDiscount = sourceDiscountCollection[i];

                        sql = $@"
update T_SAL_Discount
set F_TRAX_CSYJE={discount.GetVal<decimal>("F_TRAX_CSYJE")},
F_TRAX_GSYJE={discount.GetVal<decimal>("F_TRAX_GSYJE")}
where FENTRYID={sourceDiscount.GetVal<int>("FENTRYID")}
";
                        DBUtils.Execute(this.Context, sql);
                    }
                }
            }
            else if (FormOperation.Operation.EqualsIgnoreCase("UnAudit"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //当前单据折扣明细
                    string billNo = dataEntity.GetVal<string>("BillNo");
                    sql = $@"
select b.FENTRYID,b.FSEQ,b.F_TRAX_CSYJE,b.F_TRAX_GSYJE
from T_SAL_XORDER a
inner join T_SAL_Discount b
on a.FID= b.FID
where FBILLNO = '{billNo}'
order by b.FSEQ
";
                    DynamicObjectCollection discountCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    //源单折扣明细
                    string sourceBillNo = billNo.Split('_')[0];
                    sql = $@"
select b.FENTRYID,b.FSEQ,b.F_TRAX_CSYJE,b.F_TRAX_GSYJE
from T_SAL_ORDER a
inner join T_SAL_Discount b
on a.FID=b.FID
where FBILLNO='{sourceBillNo}'
order by b.FSEQ
";
                    DynamicObjectCollection sourceDiscountCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    //更新源单折扣明细，将加回去的释放金额减掉
                    for (int i = 0; i < sourceDiscountCollection.Count; i++)
                    {
                        DynamicObject discount = discountCollection[i];
                        DynamicObject sourceDiscount = sourceDiscountCollection[i];

                        sql = $@"
update T_SAL_Discount
set F_TRAX_CSYJE={discount.GetVal<decimal>("F_TRAX_CSYJE") + discount.GetVal<decimal>("F_TRAX_CSFJE")},
F_TRAX_GSYJE={discount.GetVal<decimal>("F_TRAX_GSYJE") + discount.GetVal<decimal>("F_TRAX_CFFJE")}
where FENTRYID={sourceDiscount.GetVal<int>("FENTRYID")}
";
                        DBUtils.Execute(this.Context, sql);
                    }
                }
            }
        }
    }
}
