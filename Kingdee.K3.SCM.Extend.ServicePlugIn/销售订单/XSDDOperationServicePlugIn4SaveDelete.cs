using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.ServicePlugIn
{
    [Description("销售订单-保存、删除-反写客户折扣池")]
    [HotUpdate]
    public class XSDDOperationServicePlugIn4SaveDelete : CommonOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FTAXPRICE");
            e.FieldKeys.Add("F_TRAX_CJZKBL");
            e.FieldKeys.Add("F_TRAX_GSZKBL");
            e.FieldKeys.Add("F_TRAX_CSYJE");
            e.FieldKeys.Add("F_TRAX_CSYKYJE");
            e.FieldKeys.Add("F_TRAX_GSYJE");
            e.FieldKeys.Add("F_TRAX_GSYKYJE");
            e.FieldKeys.Add("FENTRYID");
            e.FieldKeys.Add("F_TRAX_YDFLID");
            e.FieldKeys.Add("FBILLNO");
            e.FieldKeys.Add("FDATE");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);

            if (FormOperation.Operation.EqualsIgnoreCase("Save"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //订单明细
                    DynamicObjectCollection orderCollection = dataEntity.GetVal<DynamicObjectCollection>("SaleOrderEntry");

                    //抽取订单明细的数据
                    List<decimal> qtyList = orderCollection.GetColumns<decimal>("Qty");
                    List<decimal> priceList = orderCollection.GetColumns<decimal>("TAXPRICE");
                    List<decimal> totalList = new List<decimal>();
                    for (int i = 0; i < qtyList.Count; i++)
                    {
                        decimal qty = qtyList[i];
                        decimal price = priceList[i];
                        totalList.Add(qty * price);
                    }
                    decimal sum = totalList.Sum();
                    if (sum == 0) throw new KDBusinessException(null, "销售订单金额为0");

                    //折扣明细
                    DynamicObjectCollection discountCollection = dataEntity.GetVal<DynamicObjectCollection>("SaleOrderDiscount");

                    decimal cSum = 0;
                    decimal gSum = 0;
                    //校验使用金额不能大于剩余可用金额
                    foreach (var d in discountCollection)
                    {
                        decimal ckybl = d.GetVal<decimal>("F_TRAX_CKYBL");
                        decimal csyje = d.GetVal<decimal>("F_TRAX_CSYJE");
                        if (csyje > 0)
                        {
                            cSum += (csyje * 100 / ckybl).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                        }

                        decimal gkybl = d.GetVal<decimal>("F_TRAX_GKYBL");
                        decimal gsyje = d.GetVal<decimal>("F_TRAX_GSYJE");
                        if (gsyje > 0)
                        {
                            gSum += (gsyje * 100 / gkybl).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                        }

                        if (d.GetVal<decimal>("F_TRAX_CSYJE") > d.GetVal<decimal>("F_TRAX_CSYKYJE"))
                        {
                            throw new KDBusinessException(null, "厂家折扣，使用金额不能大于剩余可用金额");
                        }
                        if (d.GetVal<decimal>("F_TRAX_GSYJE") > d.GetVal<decimal>("F_TRAX_GSYKYJE"))
                        {
                            throw new KDBusinessException(null, "公司折扣，使用金额不能大于剩余可用金额");
                        }
                    }
                    if (discountCollection.Count != 0)
                    {
                        if (cSum > sum)
                        {
                            throw new KDBusinessException(null, "厂家折扣超出限制");
                        }

                        if (gSum > sum)
                        {
                            throw new KDBusinessException(null, "公司折扣超出限制");
                        }
                    }

                    List<long> entryIdList = discountCollection.GetColumns<long>("F_TRAX_YDFLID");

                    //反写折扣池
                    foreach (var d in discountCollection)
                    {
                        //更新
                        sql = $@"
update T_SAL_KHZKBENTRY_SYJL
set F_TRAX_CJZKBL={d.GetVal<decimal>("F_TRAX_CKYBL")},
F_TRAX_CJZKJE={d.GetVal<decimal>("F_TRAX_CSYJE")},
F_TRAX_GSZKBL={d.GetVal<decimal>("F_TRAX_GKYBL")},
F_TRAX_GSZKJE={d.GetVal<decimal>("F_TRAX_GSYJE")},
F_TRAX_DJSJ='{dataEntity.GetVal<string>("Date")}'
where FENTRYID={d.GetVal<long>("F_TRAX_YDFLID")} 
and F_TRAX_DJLX='销售订单' 
and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
";
                        if (DBUtils.Execute(this.Context, sql) < 1)
                        {
                            //新增
                            sql = $@"
insert into T_SAL_KHZKBENTRY_SYJL(
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
{d.GetVal<long>("F_TRAX_YDFLID")} ,
'销售订单',
'{dataEntity.GetVal<string>("BillNo")}',
{d.GetVal<decimal>("F_TRAX_CKYBL")},
{d.GetVal<decimal>("F_TRAX_CSYJE")},
{d.GetVal<decimal>("F_TRAX_GKYBL")},
{d.GetVal<decimal>("F_TRAX_GSYJE")},
'{dataEntity.GetVal<string>("Date")}')
";
                            DBUtils.Execute(this.Context, sql);
                        }
                    }

                    //当前单据对应上游的所有EntryId
                    sql = $@"
select FENTRYID
from T_SAL_KHZKBENTRY_SYJL
where F_TRAX_DJLX='销售订单' 
and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
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
and F_TRAX_DJLX='销售订单' 
and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
";
                        DBUtils.Execute(this.Context, sql);
                    }
                    else
                    {
                        //删除折扣记录
                        sql = $@"
delete from T_SAL_KHZKBENTRY_SYJL 
where F_TRAX_DJLX='销售订单' 
and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
";
                        DBUtils.Execute(this.Context, sql);
                    }

                    //汇总
                    string zhgxsj = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
            else if (FormOperation.Operation.EqualsIgnoreCase("Delete"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //当前单据对应上游的所有EntryId
                    sql = $@"
select FENTRYID
from T_SAL_KHZKBENTRY_SYJL
where F_TRAX_DJLX='销售订单' 
and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
";
                    DynamicObjectCollection allEntryIdCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    List<string> allEntryIdList = allEntryIdCollection.Select(c => c.GetVal<string>("FENTRYID")).ToList();
                    if (allEntryIdList.IsEmpty()) return;

                    //折扣明细
                    DynamicObjectCollection discountCollection = dataEntity.GetVal<DynamicObjectCollection>("SaleOrderDiscount");

                    //删除折扣记录
                    sql = $@"
delete from T_SAL_KHZKBENTRY_SYJL 
where F_TRAX_DJLX='销售订单' and F_TRAX_DJBH='{dataEntity.GetVal<string>("BillNo")}'
";
                    DBUtils.Execute(this.Context, sql);

                    //汇总
                    List<long> entryIdList = discountCollection.GetColumns<long>("F_TRAX_YDFLID");
                    string zhgxsj = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
