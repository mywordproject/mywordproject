using System;
using System.Collections.Generic;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [HotUpdate]
    [Description("销售订单-表单插件")]
    public class XSDDBillPlugIn : CommonBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            //物料变化后执行取价
            if (e.Field.Key.EqualsIgnoreCase("FMaterialId"))
            {

                for (int i = 0; i < View.Model.GetEntryRowCount("FSaleOrderEntry"); i++)
                {
                    this.View.Model.SetValue("FTAXPRICE", "",e.Row);
                }
                this.View.UpdateView("FSaleOrderEntry");
                //物料
                string materialId = e.NewValue?.ToString();
                if (materialId.IsEmpty()) return;

                SetPrice(e.Row, materialId);
            }
            //订单明细发生变化，当单据为销售订单时，清空折扣明细以及单据头的折扣信息
            if (e.Field.Key.EqualsIgnoreCase("FQty") || e.Field.Key.EqualsIgnoreCase("FTAXPRICE"))
            {
                this.View.Model.DeleteEntryData("FSaleOrderDiscount");
            }
        }

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            //使用折扣
            if (e.BarItemKey.EqualsIgnoreCase("TRAX_SYZK"))
            {
                //清空折扣明细
                this.View.Model.DeleteEntryData("FSaleOrderDiscount");

                string saleOrgId = this.View.Model.GetVal<DynamicObject>("SaleOrgId").GetVal<string>("Id");
                if (saleOrgId.IsEmpty())
                {
                    throw new KDBusinessException(null, "未选择销售组织");
                }

                string customerId = this.View.Model.GetVal<DynamicObject>("CUSTID").GetVal<string>("Id");
                if (customerId.IsEmpty())
                {
                    throw new KDBusinessException(null, "未选择客户");
                }

                string brandId = this.View.Model.GetVal<DynamicObject>("F_TRAX_BRAND").GetVal<string>("Id");
                if (brandId.IsEmpty())
                {
                    throw new KDBusinessException(null, "未选择品牌");
                }

                string sql = $@"
select b.*
from T_SAL_KHZKB a
inner join T_SAL_KHZKBENTRY b on a.FID=b.FID
where a.F_TRAX_BRAND={brandId} 
and b.F_TRAX_CUSTOMER={customerId}
and ((b.F_TRAX_CKYBL>0 and b.F_TRAX_CSYKYJE>0) or (b.F_TRAX_GKYBL>0 and b.F_TRAX_GSYKYJE>0))
and a.FDOCUMENTSTATUS='C'
and a.F_TRAX_SALZZ={saleOrgId}
order by b.FENTRYID
";
                MyExtend.CommonUtil.Log(nameof(sql), sql);
                DynamicObjectCollection collection = DBUtils.ExecuteDynamicObject(this.Context, sql);

                //加载折扣明细
                for (int i = 0; i < collection.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("FSaleOrderDiscount");

                    DynamicObject d = collection[i];

                    this.View.Model.SetValue("F_TRAX_DDBH", d.GetVal<string>("F_TRAX_DDBH"), i);//代垫编号
                    this.View.Model.SetValue("F_TRAX_YDFLID", d.GetVal<string>("FENTRYID"), i);//源单分录ID

                    this.View.Model.SetValue("F_TRAX_CZKZE", d.GetVal<string>("F_TRAX_CZKZE"), i);//厂家承担&折扣总额
                    this.View.Model.SetValue("F_TRAX_CKYBL", d.GetVal<string>("F_TRAX_CKYBL"), i);//厂家承担&可用比例
                    this.View.Model.SetValue("F_TRAX_CSYKYJE", d.GetVal<string>("F_TRAX_CSYKYJE"), i);//厂家承担&剩余可用金额

                    this.View.Model.SetValue("F_TRAX_GZKZE", d.GetVal<string>("F_TRAX_GZKZE"), i);//公司承担&折扣总额
                    this.View.Model.SetValue("F_TRAX_GKYBL", d.GetVal<string>("F_TRAX_GKYBL"), i);//公司承担&可用比例
                    this.View.Model.SetValue("F_TRAX_GSYKYJE", d.GetVal<string>("F_TRAX_GSYKYJE"), i);//公司承担&剩余可用金额
                }

                this.View.UpdateView("FSaleOrderDiscount");

                //订单明细
                DynamicObjectCollection orderCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderEntry");

                //抽取订单明细的数据
                List<decimal> qtyList = orderCollection.GetColumns<decimal>("Qty");
                List<decimal> priceList = orderCollection.GetColumns<decimal>("TAXPRICE");
                List<decimal> totalList = new List<decimal>();
                for (int i = 0; i < qtyList.Count; i++)
                {
                    decimal qty = qtyList[i];
                    decimal price = priceList[i];
                    totalList.Add((qty * price).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2));
                }
                decimal sum = totalList.Sum();
                if (sum == 0) return;

                //折扣明细
                DynamicObjectCollection discountCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderDiscount");

                decimal cSum = sum;
                for (int i = 0; i < discountCollection.Count; i++)
                {
                    DynamicObject discount = discountCollection[i];

                    //厂家承担&可用比例
                    decimal ckybl = discount.GetVal<decimal>("F_TRAX_CKYBL");
                    //厂家承担&剩余可用金额
                    decimal csykyje = discount.GetVal<decimal>("F_TRAX_CSYKYJE");

                    decimal partSum = (csykyje * 100 / ckybl).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                    if (partSum >= cSum)
                    {
                        this.View.Model.SetValue("F_TRAX_CDYDDJE", cSum, i);
                        this.View.Model.SetValue("F_TRAX_CSYJE", (cSum * ckybl / 100).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2), i);
                        break;
                    }
                    else
                    {
                        this.View.Model.SetValue("F_TRAX_CDYDDJE", partSum, i);
                        this.View.Model.SetValue("F_TRAX_CSYJE", csykyje, i);
                        cSum = cSum - partSum;
                    }
                }

                decimal gSum = sum;
                for (int i = 0; i < discountCollection.Count; i++)
                {
                    DynamicObject discount = discountCollection[i];

                    //公司承担&可用比例
                    decimal gkybl = discount.GetVal<decimal>("F_TRAX_GKYBL");
                    //公司承担&剩余可用金额
                    decimal gsykyje = discount.GetVal<decimal>("F_TRAX_GSYKYJE");
                    if (gkybl != 0)
                    {
                        decimal partSum = (gsykyje * 100 / gkybl).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                        if (partSum >= gSum)
                        {
                            this.View.Model.SetValue("F_TRAX_GDYDDJE", gSum, i);
                            this.View.Model.SetValue("F_TRAX_GSYJE", (gSum * gkybl / 100).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2), i);
                            break;
                        }
                        else
                        {
                            this.View.Model.SetValue("F_TRAX_GDYDDJE", partSum, i);
                            this.View.Model.SetValue("F_TRAX_GSYJE", gsykyje, i);
                            gSum = gSum - partSum;
                        }
                    }
                }

                this.View.UpdateView("FSaleOrderDiscount");
            }
        }

        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            //订单明细
            DynamicObjectCollection orderCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderEntry");

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
            if (sum == 0) return;

            //折扣明细
            DynamicObjectCollection discountCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderDiscount");//

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
            //if (cSum > sum)
            //{
            //    throw new KDBusinessException(null, "厂家折扣超出限制");
            //}

            //if (gSum > sum)
            //{
            //    throw new KDBusinessException(null, "公司折扣超出限制");
            //}

            //操作当前单据，分摊厂家折扣，分摊公司折扣，设置折扣额
            //decimal cjzkOut = 0M;
            //decimal gszkOut = 0M;
            //for (int i = 0; i < totalList.Count; i++)
            //{
            //    decimal cjzkDetail;
            //    decimal gszkDetail;

            //    if (i < totalList.Count - 1)
            //    {
            //        decimal total = totalList[i];
            //        decimal percent = total / sum;

            //        cjzkDetail = (cSum * percent).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
            //        gszkDetail = (gSum * percent).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);

            //        cjzkOut += cjzkDetail;
            //        gszkOut += gszkDetail;
            //    }
            //    else
            //    {
            //        cjzkDetail = cSum - cjzkOut;
            //        gszkDetail = gSum - gszkOut;
            //    }

            //    this.View.Model.SetValue("F_TRAX_CJZKDetail", cjzkDetail, i);
            //    this.View.Model.SetValue("F_TRAX_GSZKDetail", gszkDetail, i);
            //    this.View.Model.SetValue("FDISCOUNT", cjzkDetail + gszkDetail, i);
            //}
            double CJSZZE = Convert.ToDouble(this.View.Model.GetValue("F_TRAX_CJSYZKZE").ToString());
            double GSSZZE = Convert.ToDouble(this.View.Model.GetValue("F_TRAX_GSSYZKZE").ToString());
            var CW = (DynamicObjectCollection)this.Model.DataObject["SaleOrderFinance"];
            double ZJSHJ = 0;
            foreach (var a in CW)
            {
                ZJSHJ = Convert.ToDouble(a["BillAllAmount"].ToString());
                var MX = (DynamicObjectCollection)this.Model.DataObject["SaleOrderEntry"];
                double MXJSHJ = 0;
                int i = 0;
                foreach (var item in MX)
                {
                    MXJSHJ = Convert.ToDouble(item["AllAmount"].ToString());
                   
                   // for (int i = 0; i < this.View.Model.GetEntryRowCount("FSaleOrderEntry"); i++)
                  //  {
                        ZJSHJ = Convert.ToDouble(a["BillAllAmount"].ToString());
                        MXJSHJ = Convert.ToDouble(item["AllAmount"].ToString());
                        double CJZK = CJSZZE * (MXJSHJ / ZJSHJ);
                        double GSZK = GSSZZE * (MXJSHJ / ZJSHJ);
                        this.View.Model.SetValue("F_TRAX_CJZKDETAIL", CJZK, i);
                        this.View.Model.SetValue("F_TRAX_GSZKDETAIL", GSZK, i);
                        this.View.Model.SetValue("FDISCOUNT", CJZK + GSZK, i);
                    //  }
                    i++;
                }
            }
            this.View.UpdateView("FSaleOrderEntry");
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z")
            {
                DynamicObjectCollection orderCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderEntry");
                for (int i = 0; i < orderCollection.Count; i++)
                {
                    DynamicObject order = orderCollection[i];
                    string materialId = order.GetVal<DynamicObject>("MaterialId").GetVal<string>("Id");
                    if (materialId.IsEmpty()) return;
                    SetPrice(i, materialId);
                }
            }
        }
            #region 私有方法
            /// <summary>
            /// 获取客户开单信息上的价格类型
            /// </summary>
            private List<string> GetPriceTypeList(string customerId, string brandId, out string jsbzjg)
        {
            //价格类型列表
            List<string> priceTypeList = new List<string>();

            //客户开单信息
            string sql = $@"
select a.F_TRAX_CUSTOMER,b.F_TRAX_BRAND,b.F_TRAX_PRICETYPE,b.F_TRAX_PRICETYPE1,b.F_TRAX_PRICETYPE2,b.F_TRAX_JSBZJG
from T_SAL_KHKDXX a
inner join T_SAL_KHKDXXEntry b on a.FID=b.FID
where a.F_TRAX_CUSTOMER={customerId} 
and b.F_TRAX_BRAND={brandId}
and a.F_TRAX_STARTDATE<=to_date(TO_CHAR(SYSDATE,'yyyy-MM-dd'), 'yyyy-MM-dd') 
and a.F_TRAX_ENDDATE>=to_date(TO_CHAR(SYSDATE,'yyyy-MM-dd'), 'yyyy-MM-dd')
and a.FDOCUMENTSTATUS='C'
and a.F_TRAX_FORBIDSTATUS='A'
";
            DynamicObjectCollection collection = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (collection.Count == 0)
            {
                jsbzjg = null;
                return priceTypeList;
            }
            DynamicObject d = collection[0];

            //价格类型
            string pt = d.GetVal<string>("F_TRAX_PRICETYPE");
            if (pt.HasValue())
            {
                priceTypeList.Add(pt);
            }

            //价格类型1
            string pt1 = d.GetVal<string>("F_TRAX_PRICETYPE1");
            if (pt1.HasValue())
            {
                priceTypeList.Add(pt1);
            }

            //价格类型2
            string pt2 = d.GetVal<string>("F_TRAX_PRICETYPE2");
            if (pt2.HasValue())
            {
                priceTypeList.Add(pt2);
            }

            jsbzjg = d.GetVal<string>("F_TRAX_JSBZJG");
            return priceTypeList;
        }

        /// <summary>
        /// 取价
        /// </summary>
        /// <param name="row">行号</param>
        /// <param name="materialId">物料</param>
        private void SetPrice(int row, string materialId)
        {
            if (Convert.ToBoolean(this.Model.GetValue("FISFREE", row).ToString()) == true) return;

            //特价
            //string tj = "619afa315d32a3";
            //string tj = "61e91a001837f7";
            string tj = "61ee01e0bdb15e";
            string MDHT = "买点合同";
            string ZLHT = "折率合同";

            //客户
            string customerId = this.View.Model.GetVal<DynamicObject>("CustId").GetVal<string>("Id");
            if (customerId.IsEmpty()) return;

            //集团客户
            string sql = $@"
select FGROUPCUSTID
from T_BD_CUSTOMER
where FCUSTID={customerId} and FISGROUP=0
";
            string groupCustomerId = DBUtils.ExecuteScalar(this.Context, sql, "");

            //品牌
            sql = $@"select F_TRAX_BRAND from T_BD_MATERIAL where FMATERIALID={materialId}";
            string brandId = DBUtils.ExecuteScalar(this.Context, sql, "");
            if (brandId.IsEmpty()) return;

            //销售价目表
            decimal price = 0M;

            //查询当前客户的特价
            sql = $@"select
ROUND(CASE WHEN a.FUNITID!=WLDW.FBASEUNITID THEN (a.FPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.FPRICE END,6)
from T_SAL_PRICELISTENTRY a
inner join T_BD_MATERIAL b on a.FMATERIALID=b.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=b.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW ON b.FMATERIALID=WLDW.FMATERIALID
where a.FID in
(
select a.FID from T_SAL_PRICELIST a
inner join T_SAL_APPLYCUSTOMER b on a.FID=b.FID
where a.FPRICETYPE='{tj}' 
and b.FCUSTID={customerId}
and a.FEFFECTIVEDATE<=to_date('2022-01-14','yyyy-MM-dd')
and a.FEXPIRYDATE>=to_date('2022-01-14','yyyy-MM-dd')
and a.FDOCUMENTSTATUS='C'
and a.FFORBIDSTATUS='A'
)
and a.FFORBIDSTATUS='A'
and b.FMATERIALID={materialId}";
            price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
            if (price > 0)
            {
                SetPrice(price, row, tj);
                return;
            }

            //查询集团客户的特价
            if (groupCustomerId.HasValue())
            {
                sql = $@"SELECT
ROUND(CASE WHEN a.FUNITID!=WLDW.FBASEUNITID THEN (a.FPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.FPRICE END,6)
from T_SAL_PRICELISTENTRY a
inner join T_BD_MATERIAL b on a.FMATERIALID=b.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=b.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW ON b.FMATERIALID=WLDW.FMATERIALID
where a.FID in
(
select a.FID from T_SAL_PRICELIST a
inner join T_SAL_APPLYCUSTOMER b on a.FID=b.FID
where a.FPRICETYPE='{tj}' 
and b.FCUSTID={groupCustomerId}
and a.FEFFECTIVEDATE<=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FEXPIRYDATE>=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FDOCUMENTSTATUS='C'
and a.FFORBIDSTATUS='A'
)
and a.FFORBIDSTATUS='A'
and b.FMATERIALID={materialId}
";
                price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
                if (price > 0)
                {
                    SetPrice(price, row, tj);
                    return;
                }
            }
            //查询买点合同现价
            sql = $@"SELECT 
ROUND(CASE WHEN MDHTDE.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (MDHTDE.F_TRAX_CURRENT/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE MDHTDE.F_TRAX_CURRENT END,6)
FROM T_SON_MDHT MDHT
INNER JOIN T_SON_MDHTentity MDHTDE
ON MDHT.FID=MDHTDE.FID
INNER JOIN T_BD_MATERIAL WL
ON WL.FMATERIALID=MDHTDE.F_TRAX_WLBM
INNER JOIN T_BD_UNITCONVERTRATE WLHS
ON WLHS.FMASTERID=WL.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW
ON WL.FMATERIALID=WLDW.FMATERIALID
WHERE MDHT.F_TRAX_SXRQM<=to_date('{this.View.Model.GetValue("FDate")}')
AND MDHT.F_TRAX_SXRQQM>=to_date('{this.View.Model.GetValue("FDate")}')
AND MDHT.F_TRAX_CUSTOMER='{customerId}'
AND MDHTDE.F_TRAX_WLBM='{materialId}'";

            price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
            if (price > 0)
            {
                SetPrice(price, row, "");
                this.View.Model.SetValue("F_TRAX_JGLY", MDHT,row);
                return;
            }
            //查询折率合同
            sql = $@"SELECT 
ROUND(CASE WHEN KLHTD.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (KLHTD.F_TRAX_PRESENTPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE KLHTD.F_TRAX_PRESENTPRICE END,6)
FROM T_SON_KVcontract KLHT
INNER JOIN T_SON_KVcontractEntry KLHTD
ON KLHT.FID=KLHTD.FID
INNER JOIN T_BD_MATERIAL WL
ON KLHTD.F_TRAX_MATERIALID=WL.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS
ON WLHS.FMASTERID=WL.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW
ON WL.FMATERIALID=WLDW.FMATERIALID
WHERE KLHT.FDOCUMENTSTATUS='C'
AND KLHT.F_TRAX_STARTDATE<=to_date('{this.View.Model.GetValue("FDate")}')
AND KLHT.F_TRAX_ENDDATE>=to_date('{this.View.Model.GetValue("FDate")}')
AND KLHT.F_TRAX_CUSTOMER='{customerId}'
AND KLHTD.F_TRAX_MATERIALID='{materialId}'";

            price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
            if (price > 0)
            {
                SetPrice(price, row, "");
                this.View.Model.SetValue("F_TRAX_JGLY", ZLHT, row);
                return;
            }

            //处理当前客户的客户开单信息
            string jsbzjg;
            var priceTypeList = GetPriceTypeList(customerId, brandId, out jsbzjg);

            //处理集团客户的客户开单信息
            if (priceTypeList.IsEmpty() && groupCustomerId.HasValue())
            {
                priceTypeList = GetPriceTypeList(groupCustomerId, brandId, out jsbzjg);
            }

            //酒水标准价格
            if (jsbzjg.HasValue())
            {
                sql = $@"select 
ROUND(CASE WHEN a.FUNITID!=WLDW.FBASEUNITID THEN (a.FPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.FPRICE END,6)
from T_SAL_PRICELISTENTRY a
inner join T_BD_MATERIAL b on a.FMATERIALID=b.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=b.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW ON b.FMATERIALID=WLDW.FMATERIALID
where a.FID in
(
select a.FID from T_SAL_PRICELIST a
where a.FPRICETYPE='{jsbzjg}' 
and a.FEFFECTIVEDATE<=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FEXPIRYDATE>=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FDOCUMENTSTATUS='C'
and a.FFORBIDSTATUS='A'
)
and a.FFORBIDSTATUS='A'
and b.FMATERIALID={materialId}
";
                price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
                this.View.Model.SetValue("F_TRAX_DPBTDJ", price, row);
                this.View.UpdateView("F_TRAX_DPBTDJ", row);
            }

            //含税单价
            foreach (var priceType in priceTypeList)
            {
                sql = $@"select 
ROUND(CASE WHEN a.FUNITID!=WLDW.FBASEUNITID THEN (a.FPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE a.FPRICE END,6)
from T_SAL_PRICELISTENTRY a
inner join T_BD_MATERIAL b on a.FMATERIALID=b.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS ON WLHS.FMASTERID=b.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW ON b.FMATERIALID=WLDW.FMATERIALID
where a.FID in
(
select a.FID from T_SAL_PRICELIST a
where a.FPRICETYPE='{priceType}' 
and a.FEFFECTIVEDATE<=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FEXPIRYDATE>=to_date('{this.View.Model.GetVal<DateTime>("Date"):yyyy-MM-dd}','yyyy-MM-dd')
and a.FDOCUMENTSTATUS='C'
and a.FFORBIDSTATUS='A'
)
and a.FFORBIDSTATUS='A'
and b.FMATERIALID={materialId}
";
                price = DBUtils.ExecuteScalar(this.Context, sql, 0M);
                if (price > 0)
                {
                    SetPrice(price, row, priceType);
                    break;
                }
            }
        }

        /// <summary>
        /// 设置含税单价
        /// </summary>
        private void SetPrice(decimal price, int row, string priceType)
        {
            //设置含税单价
            this.View.Model.SetValue("FTAXPRICE", price, row);
            this.View.InvokeFieldUpdateService("FTAXPRICE", row);

            this.View.Model.SetValue("F_TRAX_JGLX", priceType, row);
            this.View.InvokeFieldUpdateService("F_TRAX_JGLX", row);
        }
        #endregion

    }
}
