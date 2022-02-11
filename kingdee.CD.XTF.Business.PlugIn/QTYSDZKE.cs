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

namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("获取折扣信息-逐行计算-使用金额分录分摊"), HotUpdate]
    public class QTYSDZKE : AbstractBillPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            double ZJE = Convert.ToDouble(this.View.Model.GetValue("F_TRAX_HJZKJE").ToString());
            if (ZJE != 0)
            {
                if (e.BarItemKey.Equals("TRAX_SYZK", StringComparison.OrdinalIgnoreCase))
                {
                    var XSZZ = (this.View.Model.GetValue("FSALEORGID") as DynamicObject)["Id"].ToString();
                    var PP = "";
                    if (View.Model.GetValue("F_TRAX_PP") != null)
                        PP = (this.View.Model.GetValue("F_TRAX_PP") as DynamicObject)["Id"].ToString();
                    else
                        View.ShowWarnningMessage("请维护品牌或者客户信息");
                    var KH = "";
                    if (View.Model.GetValue("FCONTACTUNIT") != null)
                        KH = (this.View.Model.GetValue("FCONTACTUNIT") as DynamicObject)["Id"].ToString();
                    else
                        View.ShowWarnningMessage("请维护品牌或者客户信息");

                    //执行SQL
                    string sql = $@"/*dialect*/SELECT a.F_TRAX_SALZZ,a.F_TRAX_BRAND,b.F_TRAX_CUSTOMER,b.FENTRYID,b.F_TRAX_DDBH,b.F_TRAX_CZKZE,b.F_TRAX_CKYBL,b.F_TRAX_CSYKYJE,b.F_TRAX_GZKZE,b.F_TRAX_GKYBL
,b.F_TRAX_GSYKYJE
FROM T_SAL_KHZKB a
INNER JOIN T_SAL_KHZKBENTRY b
on a.FID=b.FID
where a.F_TRAX_SALZZ='{XSZZ}'
AND a.F_TRAX_BRAND='{PP}'
AND b.F_TRAX_CUSTOMER='{KH}'";
                    var dt = DBUtils.ExecuteDynamicObject(Context, sql);
                    int i = 0;
                    this.Model.DeleteEntryData("F_TRAX_Entity");
                    //进行赋值
                    if (dt.Count > 0)
                    {
                        foreach (var item in dt)
                        {
                            this.Model.CreateNewEntryRow("F_TRAX_Entity");
                            this.View.Model.SetValue("F_TRAX_DDBH", item["F_TRAX_DDBH"].ToString(), i);//代垫编号
                            this.View.Model.SetValue("F_TRAX_CZKZE", item["F_TRAX_CZKZE"].ToString(), i);//厂家承担&折扣总额
                            this.View.Model.SetValue("F_TRAX_CKYBL", item["F_TRAX_CKYBL"].ToString(), i);//厂家承担&可用比例
                            this.View.Model.SetValue("F_TRAX_CSYKYJE", item["F_TRAX_CSYKYJE"].ToString(), i);//厂家承担&剩余可用金额
                            this.View.Model.SetValue("F_TRAX_GZKZE", item["F_TRAX_GZKZE"].ToString(), i);//公司承担&折扣总额
                            this.View.Model.SetValue("F_TRAX_GKYBL", item["F_TRAX_GKYBL"].ToString(), i);//公司承担&可用比例
                            this.View.Model.SetValue("F_TRAX_GSYKYJE", item["F_TRAX_GSYKYJE"].ToString(), i);//公司承担&剩余可用金额
                            this.View.Model.SetItemValueByID("F_TRAX_YDFLID", item["FENTRYID"].ToString(), i);//源单分录id
                            i++;
                        }
                        this.View.UpdateView("F_TRAX_Entity");
                    }
                    //获取折扣单据体信息
                    var ZKS = (DynamicObjectCollection)this.Model.DataObject["DiscountEntity"];
                    if (ZKS.Count != 0)
                    {
                        //获取最小折扣比例
                        double cjmin = Convert.ToDouble(ZKS[0]["F_TRAX_CKYBL"].ToString());
                        double gsmin = Convert.ToDouble(ZKS[0]["F_TRAX_GKYBL"].ToString());
                   
                    foreach (var zk in ZKS)
                    {
                        if (cjmin > Convert.ToDouble(zk["F_TRAX_CKYBL"].ToString()))
                        {
                            cjmin = Convert.ToDouble(zk["F_TRAX_CKYBL"].ToString());
                        }
                        this.View.Model.SetValue("F_TRAX_ZXCSCDBL", cjmin);
                        if (gsmin > Convert.ToDouble(zk["F_TRAX_GKYBL"].ToString()))
                        {
                            gsmin = Convert.ToDouble(zk["F_TRAX_GKYBL"].ToString());
                        }
                        this.View.Model.SetValue("F_TRAX_ZXGSCDBL", gsmin);
                    }
                    //获取后进行赋值剩余金额
                    int j = 0;
                    double CSZK = ZJE;//cjmin * (ZJE / 100);比例之后厂商
                    double GSZK = ZJE;//gsmin * (ZJE / 100);比例之后公司
                    int index = 0;
                    int index1 = 0;
                    double jian = CSZK;
                    double GSJN = GSZK;
                        for (int f = 0; f < View.Model.GetEntryRowCount("F_TRAX_Entity"); f++)
                        {
                            if (jian > Convert.ToDouble(View.Model.GetValue("F_TRAX_CSYKYJE", f)) && index == 0)
                            {
                                jian = jian - Convert.ToDouble(View.Model.GetValue("F_TRAX_CSYKYJE", f));
                                View.Model.SetValue("F_TRAX_CSYJE", Convert.ToDouble(View.Model.GetValue("F_TRAX_CSYKYJE", f)), f);
                            }
                            else
                            {
                                if (index <= 0)
                                {
                                    index = f++;
                                    View.Model.SetValue("F_TRAX_CSYJE", jian, index);
                                }
                            }

                            for (int h = 0; h < View.Model.GetEntryRowCount("F_TRAX_Entity"); h++)
                            {
                                if (GSJN > Convert.ToDouble(View.Model.GetValue("F_TRAX_GSYKYJE", h)) && index1 == 0)
                                {
                                    GSJN = GSJN - Convert.ToDouble(View.Model.GetValue("F_TRAX_GSYKYJE", h));
                                    View.Model.SetValue("F_TRAX_GSYJE", Convert.ToDouble(View.Model.GetValue("F_TRAX_GSYKYJE", h)), h);
                                }
                                else
                                {
                                    if (index1 <= 0)
                                    {
                                        index1 = h++;
                                        View.Model.SetValue("F_TRAX_GSYJE", GSJN, index1);
                                    }
                                }
                            }
                        }

                        this.View.UpdateView("F_TRAX_Entity");
                    }
                }
                else if (ZJE == 0)
                {
                    this.View.ShowMessage("请维护分录总金额");
                }
            }
        }
        //字段变化后删除折扣单据体
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FAMOUNTFOR")
            {
                this.Model.DeleteEntryData("F_TRAX_Entity");
                //this.View.Model.SetValue("F_TRAX_CJZKDETAIL", "");
                //this.View.Model.SetValue("F_TRAX_GSZKDETAIL", "");
            }
        }
    }
}
