using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.SCM.Core.SPM;
using Kingdee.K3.SCM.CP.Business.PlugIn;
using Kingdee.K3.SCM.ServiceHelper;
using MyExtend;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [HotUpdate]
    [Description("销售订单（促销政策匹配）-表单插件")]
    public class XSDDBillPlugIn4CXZCPP : CommonBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            //数量、价格发生变化后，判断是否有促销政策
            if (e.Field.Key.EqualsIgnoreCase("FBaseUnitQty") || e.Field.Key.EqualsIgnoreCase("FTAXPRICE"))
            {
                var conditions = new List<PromotionPolicyMatchData>();

                DynamicObjectCollection orderCollection = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderEntry");
                for (int i = 0; i < orderCollection.Count; i++)
                {
                    DynamicObject order = orderCollection[i];

                    long num = order.GetVal<long>("Id");
                    if (!order.GetVal<bool>("FIsFree"))
                    {
                        PromotionPolicyMatchData promotionPolicyMatchData = new PromotionPolicyMatchData();
                        promotionPolicyMatchData.PageId = "SAL_SaleOrder";
                        promotionPolicyMatchData.BillID = 0;
                        promotionPolicyMatchData.BillEntryID = ((num == 0) ? i : num);
                        promotionPolicyMatchData.SaleOrgID = this.View.Model.GetVal<DynamicObject>("SaleOrgId").GetVal<long>("Id");
                        promotionPolicyMatchData.CustomerID = this.View.Model.GetVal<DynamicObject>("CustId").GetVal<long>("Id");
                        promotionPolicyMatchData.CurrencyID = this.View.Model.GetVal<DynamicObjectCollection>("SaleOrderFinance").First().GetVal<DynamicObject>("SettleCurrId").GetVal<long>("Id");
                        promotionPolicyMatchData.BizDate = this.View.Model.GetVal<DateTime>("Date");
                        promotionPolicyMatchData.MaterialID = order.GetVal<DynamicObject>("MaterialId").GetVal<long>("Id");
                        promotionPolicyMatchData.AttrValueID = order.GetVal<DynamicObject>("AuxPropId").GetVal<long>("Id");
                        promotionPolicyMatchData.UnitID = order.GetVal<DynamicObject>("UnitId").GetVal<long>("Id");
                        promotionPolicyMatchData.BaseUnitID = order.GetVal<DynamicObject>("BaseUnitId").GetVal<long>("Id");
                        promotionPolicyMatchData.PurchaseQty = order.GetVal<decimal>("Qty");
                        promotionPolicyMatchData.PurchaseBaseQty = order.GetVal<decimal>("BaseUnitQty");
                        promotionPolicyMatchData.PurchaseAmount = this.View.Model.GetVal<decimal>("OldAmount");
                        if (promotionPolicyMatchData.UnitID != 0 && promotionPolicyMatchData.SaleOrgID != 0 && promotionPolicyMatchData.CustomerID != 0 && promotionPolicyMatchData.MaterialID != 0 && promotionPolicyMatchData.CurrencyID != 0)
                        {
                            conditions.Add(promotionPolicyMatchData);
                        }
                    }
                }

                List<PromotionPolicyMatchData> list = SPMServiceHelper.PromotionMatch(base.Context, conditions);

                this.View.Model.SetValue("F_TRAX_CXTS", list.HasValue() ? "有促销" : "");
            }
        }
    }
}
