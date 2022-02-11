using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Model.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.Web.DynamicForm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using YYLK;

namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("销售订单匹配促销"), HotUpdate]
    public class XSDDCX : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            //是否赠品变更后取价
            if (e.Field.Key.EqualsIgnoreCase("FMaterialId"))
            {
                //物料
                string materialId = e.NewValue?.ToString();
                if (materialId.IsEmpty()) return;

                SetPrice(e.Row, materialId);
            }
        }
        #region 私有方法
        ///// <summary>
        ///// 获取客户开单信息上的价格类型
        ///// </summary>
        //private List<string> GetPriceTypeList(string customerId, string brandId, out string jsbzjg)
        //{
        //    //价格类型列表
        //    List<string> priceTypeList = new List<string>();

        //    //客户开单信息
        //    string sql = $@"
        //select a.F_TRAX_CUSTOMER,b.F_TRAX_BRAND,b.F_TRAX_PRICETYPE,b.F_TRAX_PRICETYPE1,b.F_TRAX_PRICETYPE2,b.F_TRAX_JSBZJG
        //from T_SAL_KHKDXX a
        //inner join T_SAL_KHKDXXEntry b on a.FID=b.FID
        //where a.F_TRAX_CUSTOMER={customerId} 
        //and b.F_TRAX_BRAND={brandId}
        //and a.F_TRAX_STARTDATE<=to_date(TO_CHAR(SYSDATE,'yyyy-MM-dd'), 'yyyy-MM-dd') 
        //and a.F_TRAX_ENDDATE>=to_date(TO_CHAR(SYSDATE,'yyyy-MM-dd'), 'yyyy-MM-dd')
        //and a.FDOCUMENTSTATUS='C'
        //";
        //    DynamicObjectCollection collection = DBUtils.ExecuteDynamicObject(this.Context, sql);
        //    if (collection.Count == 0)
        //    {
        //        jsbzjg = null;
        //        return priceTypeList;
        //    }
        //    DynamicObject d = collection[0];

        //    //价格类型
        //    string pt = d.GetVal<string>("F_TRAX_PRICETYPE");
        //    if (pt.HasValue())
        //    {
        //        priceTypeList.Add(pt);
        //    }

        //    //价格类型1
        //    string pt1 = d.GetVal<string>("F_TRAX_PRICETYPE1");
        //    if (pt1.HasValue())
        //    {
        //        priceTypeList.Add(pt1);
        //    }

        //    //价格类型2
        //    string pt2 = d.GetVal<string>("F_TRAX_PRICETYPE2");
        //    if (pt2.HasValue())
        //    {
        //        priceTypeList.Add(pt2);
        //    }

        //    jsbzjg = d.GetVal<string>("F_TRAX_JSBZJG");
        //    return priceTypeList;
        //}

        /// <summary>
        /// 取价
        /// </summary>
        /// <param name="row">行号</param>
        /// <param name="materialId">物料</param>
        private void SetPrice(int row, string materialId)
        {
            if (Convert.ToBoolean(this.Model.GetValue("FISFREE", row).ToString()) == true)
            {
                //销售价目表

                var ENID = "";
                var FID = "";
                for (int i = 0; i < this.View.Model.GetEntryRowCount("FSaleOrderEntry"); i++)
                {
                    ENID = this.View.Model.GetValue("FSPMENTRYID", row).ToString();
                    //查询促销政策单价
                    string sql = $@"select a.FID from T_SPM_PromotionPolicy a
        inner join T_SPM_PromotionPolicyEntry b on a.FID=b.FID where FENTRYID='{ENID}'";
                    FID = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows[0]["FID"].GetString();

                    string cxsql = $@"SELECT WL.FNUMBER,b.F_TRAX_PRICE FROM T_SPM_PromotionPolicy a
INNER JOIN T_SPM_PromotionPolicyEntry b ON a.FID=b.FID
INNER JOIN T_BD_MATERIAL WL ON WL.FMATERIALID=b.FMATERIALID
where a.FID='{FID}'
AND b.F_TRAX_PRICE!=0";
                    //WLID = DBUtils.ExecuteDataSet(this.Context, sql1).Tables[0].Rows[0]["FMATERIALID"].GetString();
                    var cxdate = DBUtils.ExecuteDynamicObject(Context, cxsql);

                    var MX = (DynamicObjectCollection)this.Model.DataObject["SaleOrderEntry"];

                    foreach (var item in MX)
                    {
                       string WL = item["MaterialId"] ==null?"" :(item["MaterialId"] as DynamicObject)["Number"].ToString();
                        if (WL == cxdate[0]["FNUMBER"].ToString())
                        {
                            this.Model.SetValue("FTAXPRICE", cxdate[0]["F_TRAX_PRICE"], Convert.ToInt32(item["Seq"].ToString()) - 1);
                           return;
                        }
                    }
                }
                //price = DBUtils.ExecuteScalar(this.Context, sql2, 0M);
                //if (price > 0)
                //{
                //    SetPrice(price, row, "");
                //    return;
                //}
            }
        }
    }
}
#endregion

//    / <summary>
//    / 设置含税单价
//    / </summary>
//    private void SetPrice(decimal price, int row, string priceType)
//    {
//        设置含税单价
//        this.View.Model.SetValue("FTAXPRICE", price, row);
//        this.View.InvokeFieldUpdateService("FTAXPRICE", row);

//        this.View.Model.SetValue("F_TRAX_JGLX", priceType, row);
//        this.View.InvokeFieldUpdateService("F_TRAX_JGLX", row);
//    }
//    #endregion
//}
//}
