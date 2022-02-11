using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [HotUpdate]
    [Description("销售订单新变更单-表单插件")]
    public class XSDDXBGDBillPlugin : CommonBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            //获取订单明细
            DynamicObjectCollection orderCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderEntry");

            //原销售数量
            List<decimal> qtyxList = orderCollection.GetColumns<decimal>("QTYX");
            //销售数量
            List<decimal> qtyList = orderCollection.GetColumns<decimal>("Qty");
            //含税单价
            List<decimal> priceList = orderCollection.GetColumns<decimal>("TaxPrice");
            //变更类型，修改=B，删除=D
            List<string> changeTypeList = orderCollection.GetColumns<string>("ChangeType");

            //校验销售数量只能减少
            for (int i = 0; i < qtyxList.Count; i++)
            {
                decimal qtyx = qtyxList[i];
                decimal qty = qtyList[i];
                if (qty > qtyx)
                {
                    throw new KDBusinessException(null, "销售数量不能大于原销售数量");
                }
            }

            //原总金额
            decimal oldSum = 0;
            //调低总金额
            decimal downSum = 0;

            //计算明细的总额
            for (int i = 0; i < changeTypeList.Count; i++)
            {
                decimal qtyx = qtyxList[i];
                decimal qty = qtyList[i];
                decimal price = priceList[i];

                oldSum += qtyx * price;

                string changeType = changeTypeList[i];
                if (changeType == "B")
                {
                    //修改
                    downSum += (qtyx - qty) * price;
                }
                else if (changeType == "D")
                {
                    //删除
                    downSum += qtyx * price;
                }
            }

            //获取折扣明细
            DynamicObjectCollection discountCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderDiscount");

            //设置释放金额
            for (int i = 0; i < discountCollection.Count; i++)
            {
                DynamicObject d = discountCollection[i];

                decimal cAll = d.GetVal<decimal>("F_TRAX_CSYJE") + d.GetVal<decimal>("F_TRAX_CSFJE");
                decimal csfje = (cAll * downSum / oldSum).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                decimal csyje = cAll - csfje;
                this.View.Model.SetValue("F_TRAX_CSFJE", csfje, i);
                this.View.Model.SetValue("F_TRAX_CSYJE", csyje, i);

                decimal gAll = d.GetVal<decimal>("F_TRAX_GSYJE") + d.GetVal<decimal>("F_TRAX_GSFJE");
                decimal gsfje = (gAll * downSum / oldSum).MathRound(BOSEnums.Enu_RoundType.KdFloor, 2);
                decimal gsyje = gAll - gsfje;
                this.View.Model.SetValue("F_TRAX_GSFJE", gsfje, i);
                this.View.Model.SetValue("F_TRAX_GSYJE", gsyje, i);
            }
            this.View.UpdateView("F_TRAX_CSFJE");
            this.View.UpdateView("F_TRAX_CSYJE");
            this.View.UpdateView("F_TRAX_GSFJE");
            this.View.UpdateView("F_TRAX_GSYJE");
        }
    }
}
