using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.Mobile.Objects.GoldenTax;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.ServicePlugIn
{
    [Description("销售订单-保存、删除-反写促销活动")]
    [HotUpdate]
    public class XSDDOperationServicePlugIn4CXHD : CommonOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FTAXPRICE");
            e.FieldKeys.Add("F_TRAX_CJZK1");
            e.FieldKeys.Add("F_TRAX_CJZKBL");
            e.FieldKeys.Add("F_TRAX_GSZK1");
            e.FieldKeys.Add("F_TRAX_GSZKBL");
            e.FieldKeys.Add("F_TRAX_CSYJE");
            e.FieldKeys.Add("F_TRAX_CSYKYJE");
            e.FieldKeys.Add("F_TRAX_GSYJE");
            e.FieldKeys.Add("F_TRAX_GSYKYJE");
            e.FieldKeys.Add("FENTRYID");
            e.FieldKeys.Add("F_TRAX_YDFLID");
            e.FieldKeys.Add("FBILLNO");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);

            if (FormOperation.Operation.EqualsIgnoreCase("Save"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //获取客户主键
                    int custId = dataEntity.GetVal<DynamicObject>("CustId").GetVal<int>("Id");
                    //销售订单编号
                    string billNo = dataEntity.GetVal<string>("BillNo");

                    //获取赠品商品明细
                    DynamicObjectCollection orderCollection = dataEntity.GetVal<DynamicObjectCollection>("SaleOrderEntry");
                    List<DynamicObject> zpCollection = orderCollection.Where(order => order.GetVal<bool>("IsFree")).ToList();
                    if (zpCollection.IsEmpty()) return;

                    var zpList = zpCollection.Select(order => new ZP()
                    {
                        F_TRAX_CXZCID = order.GetVal<int>("SPMENTRYID"),
                        F_TRAX_KHID = custId,
                        F_TRAX_XSDDBH = billNo,
                        F_TRAX_WLID = order.GetVal<DynamicObject>("MaterialId").GetVal<int>("Id"),
                        F_TRAX_ZSSL = order.GetVal<decimal>("Qty"),
                    }).ToList();

                    //补全促销活动Id
                    sql = $@"
select distinct b.FENTRYID SPMENTRYID,a.FID, a.F_TRAX_CXHDHH 
from T_SPM_PromotionPolicy a
inner join T_SPM_PromotionPolicyEntry b
on a.FID=b.FID
where b.FENTRYID in ({string.Join(",", zpList.Select(zp => zp.F_TRAX_CXZCID))})
";
                    DynamicObjectCollection cxzcCollection = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (cxzcCollection.HasValue())
                    {
                        foreach (ZP zp in zpList)
                        {
                            var cxzc = cxzcCollection.FirstOrDefault(czxc =>
                                czxc.GetVal<int>("SPMENTRYID") == zp.F_TRAX_CXZCID);
                            if (cxzc == null) throw new KDBusinessException(null, "未找到赠品对应的促销政策");

                            zp.FENTRYID = cxzc.GetVal<int>("F_TRAX_CXHDHH");
                        }
                    }

                    //0设置
                    sql = $@"
update T_SPM_Promotions_SLKZ
set F_TRAX_ZSSL=0
where F_TRAX_XSDDBH='{billNo}'
";
                    DBUtils.Execute(this.Context, sql);

                    //反写促销活动
                    foreach (ZP zp in zpList)
                    {
                        //更新
                        sql = $@"
update T_SPM_Promotions_SLKZ
set F_TRAX_KHID={zp.F_TRAX_KHID},
F_TRAX_WLID={zp.F_TRAX_WLID},
F_TRAX_ZSSL={zp.F_TRAX_ZSSL}
where FENTRYID={zp.FENTRYID} 
and F_TRAX_XSDDBH='{zp.F_TRAX_XSDDBH}'
and F_TRAX_CXZCID={zp.F_TRAX_CXZCID}
";
                        //不能根据销售订单来锁定明细，需要根据销售订单的明细来锁定明细
                        if (DBUtils.Execute(this.Context, sql) < 1)
                        {
                            //新增
                            sql = $@"
insert into T_SPM_Promotions_SLKZ(
FDETAILID,
FENTRYID,
F_TRAX_CXZCID,
F_TRAX_KHID,
F_TRAX_XSDDBH,
F_TRAX_WLID,
F_TRAX_ZSSL)
values(
seq_yxk.nextval,
{zp.FENTRYID} ,
{zp.F_TRAX_CXZCID},
{zp.F_TRAX_KHID},
'{zp.F_TRAX_XSDDBH}',
{zp.F_TRAX_WLID},
{zp.F_TRAX_ZSSL})
";
                            DBUtils.Execute(this.Context, sql);
                        }
                    }

                    //0删除
                    sql = $@"
delete T_SPM_Promotions_SLKZ
where F_TRAX_XSDDBH='{billNo}'
and F_TRAX_ZSSL=0
";
                    DBUtils.Execute(this.Context, sql);

                    //
                    List<int> entryIdList = zpList.Select(zp => zp.FENTRYID).Distinct().ToList();

                    //获取单据编号相关FID
                    sql = $@"
select distinct b.FID
from T_SPM_Promotions_SLKZ a
inner join T_SPM_Promotionsentry b 
on a.FENTRYID=b.FENTRYID
where a.F_TRAX_XSDDBH='{billNo}'
";
                    List<int> partFidList1 = DBUtils.ExecuteDynamicObject(this.Context, sql).GetColumns<int>("FID");
                    //获取EntryId相关FID
                    sql = $@"
select distinct FID
from T_SPM_Promotionsentry 
where FENTRYID in ({string.Join(",", entryIdList)})
";
                    List<int> partFidList2 = DBUtils.ExecuteDynamicObject(this.Context, sql).GetColumns<int>("FID");

                    List<int> fidList = new List<int>();
                    fidList.AddRange(partFidList1);
                    fidList.AddRange(partFidList2);
                    fidList = fidList.Distinct().ToList();

                    //促销活动整单汇总
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_TOTALGIVEAWAYS,a.F_TRAX_REMAININGGIFTS,b.F_TRAX_ZSSL
from T_SPM_Promotions a
inner join
(
select b.FID,SUM(a.F_TRAX_ZSSL) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
inner join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fidList)})
group by b.FID
) b
on a.FID=b.FID
) t
set t.F_TRAX_REMAININGGIFTS=t.F_TRAX_TOTALGIVEAWAYS-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);
                    //校验
                    sql = $@"
select count(1)
from T_SPM_Promotions 
where FID in ({string.Join(",", fidList)})
and F_TRAX_TOTALGIVEAWAYS>0
and F_TRAX_REMAININGGIFTS<0
";
                    if (DBUtils.ExecuteScalar(this.Context, sql, 0) > 0)
                    {
                        throw new KDBusinessException(null, "赠品数量超出限制");
                    }

                    //汇总正品明细
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_LIMITEDNUMBER,a.F_TRAX_XZSYQTY,b.F_TRAX_ZSSL
from T_SPM_Promotionsentry a
inner join
(
select b.FENTRYID,SUM(a.F_TRAX_ZSSL) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
inner join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fidList)})
group by b.FENTRYID
) b
on a.FENTRYID=b.FENTRYID
) t
set t.F_TRAX_XZSYQTY=t.F_TRAX_LIMITEDNUMBER-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);
                    //约量合并校验
                    sql = $@"
select count(1) from
(
select TRIM(F_TRAX_YLHB) F_TRAX_YLHB,F_TRAX_LIMITEDNUMBER,F_TRAX_XZSYQTY
from T_SPM_Promotionsentry 
where FID in ({string.Join(",", fidList)})
and TRIM(F_TRAX_YLHB) is not null
) t
group by t.F_TRAX_YLHB
having sum(F_TRAX_LIMITEDNUMBER)>0
and sum(F_TRAX_XZSYQTY)<0
";
                    if (DBUtils.ExecuteScalar(this.Context, sql, 0) > 0)
                    {
                        throw new KDBusinessException(null, "赠品数量超出限制");
                    }
                    //非约量合并校验
                    sql = $@"
select count(1)
from T_SPM_Promotionsentry
where FID in ({string.Join(",", fidList)})
and F_TRAX_LIMITEDNUMBER>0
and F_TRAX_XZSYQTY<0
and TRIM(F_TRAX_YLHB) is null
";
                    if (DBUtils.ExecuteScalar(this.Context, sql, 0) > 0)
                    {
                        throw new KDBusinessException(null, "赠品数量超出限制");
                    }

                    //汇总客户明细
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_LIMITGIFTQTY,a.F_TRAX_REMAININGLIMITQTY,b.F_TRAX_ZSSL
from T_SPM_PromotionsCustomer a
inner join
(
select a.FENTRYID,a.F_TRAX_KHID,SUM(a.F_TRAX_ZSSL) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
inner join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fidList)})
group by a.FENTRYID,a.F_TRAX_KHID
) b
on a.FENTRYID=b.FENTRYID
and a.F_TRAX_CUSTOMERCODE=b.F_TRAX_KHID
) t
set t.F_TRAX_REMAININGLIMITQTY=t.F_TRAX_LIMITGIFTQTY-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);
                    //校验
                    sql = $@"
select count(1)
from T_SPM_PromotionsCustomer a
inner join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fidList)})
and a.F_TRAX_LIMITGIFTQTY>0
and a.F_TRAX_REMAININGLIMITQTY<0
";
                    if (DBUtils.ExecuteScalar(this.Context, sql, 0) > 0)
                    {
                        throw new KDBusinessException(null, "赠品数量超出限制");
                    }
                }
            }
            else if (FormOperation.Operation.EqualsIgnoreCase("Delete"))
            {
                foreach (var dataEntity in e.DataEntitys)
                {
                    string sql;

                    //获取客户主键
                    int custId = dataEntity.GetVal<DynamicObject>("CustId").GetVal<int>("Id");
                    //销售订单编号
                    string billNo = dataEntity.GetVal<string>("BillNo");

                    //
                    sql = $@"
select distinct b.FID
from T_SPM_Promotions_SLKZ a
inner join T_SPM_Promotionsentry b 
on a.FENTRYID=b.FENTRYID
where a.F_TRAX_XSDDBH='{billNo}'
";
                    List<int> fIdList = DBUtils.ExecuteDynamicObject(this.Context, sql).GetColumns<int>("FID");

                    //
                    sql = $@"
delete T_SPM_Promotions_SLKZ
where F_TRAX_XSDDBH='{billNo}'
";
                    DBUtils.Execute(this.Context, sql);

                    //促销活动整单汇总
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_TOTALGIVEAWAYS,a.F_TRAX_REMAININGGIFTS,b.F_TRAX_ZSSL
from T_SPM_Promotions a
inner join
(
select b.FID,nvl(SUM(a.F_TRAX_ZSSL),0) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
right join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fIdList)})
group by b.FID
) b
on a.FID=b.FID
) t
set t.F_TRAX_REMAININGGIFTS=t.F_TRAX_TOTALGIVEAWAYS-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);

                    //汇总正品明细
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_LIMITEDNUMBER,a.F_TRAX_XZSYQTY,b.F_TRAX_ZSSL
from T_SPM_Promotionsentry a
inner join
(
select b.FENTRYID,nvl(SUM(a.F_TRAX_ZSSL),0) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
right join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fIdList)})
group by b.FENTRYID
) b
on a.FENTRYID=b.FENTRYID
) t
set t.F_TRAX_XZSYQTY=t.F_TRAX_LIMITEDNUMBER-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);

                    //汇总客户明细
                    sql = $@"/*dialect*/
update
(
select a.F_TRAX_LIMITGIFTQTY,a.F_TRAX_REMAININGLIMITQTY,b.F_TRAX_ZSSL
from T_SPM_PromotionsCustomer a
inner join
(
select a.FENTRYID,a.F_TRAX_KHID,nvl(SUM(a.F_TRAX_ZSSL),0) F_TRAX_ZSSL
from T_SPM_Promotions_SLKZ a
right join T_SPM_Promotionsentry b
on a.FENTRYID=b.FENTRYID
where b.FID in ({string.Join(",", fIdList)})
group by a.FENTRYID,a.F_TRAX_KHID
) b
on a.FENTRYID=b.FENTRYID
and a.F_TRAX_CUSTOMERCODE=b.F_TRAX_KHID
) t
set t.F_TRAX_REMAININGLIMITQTY=t.F_TRAX_LIMITGIFTQTY-t.F_TRAX_ZSSL
";
                    DBUtils.Execute(this.Context, sql);
                }
            }
        }
    }

    public class ZP
    {
        //促销活动行号
        public int FENTRYID { get; set; }
        //促销政策Id
        public int F_TRAX_CXZCID { get; set; }
        //客户Id
        public int F_TRAX_KHID { get; set; }
        //销售订单编号
        public string F_TRAX_XSDDBH { get; set; }
        //物料Id
        public int F_TRAX_WLID { get; set; }
        //数量
        public decimal F_TRAX_ZSSL { get; set; }
    }
}
