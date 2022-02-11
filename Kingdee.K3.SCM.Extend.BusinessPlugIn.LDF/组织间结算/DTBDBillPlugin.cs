using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn
{
    [Description("单击按钮弹出对话框")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class DTBDBillPlugin:AbstractBillPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("TRAX_PLTZ"))
            {
                DynamicFormShowParameter parameter = new DynamicFormShowParameter();
                parameter.OpenStyle.ShowType = ShowType.Floating;
                parameter.FormId = "TRAX_NBJGPLTZ";
                parameter.MultiSelect = false;
                //获取返回的值
                this.View.ShowForm(parameter,delegate(FormResult result)
                {
                    string[] date = (string[])result.ReturnData;
                    int entitycount = this.Model.GetEntryRowCount("FEntity");
                    //DynamicObjectCollection entitydate = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                    if (date[2] == "品牌")
                    {
                       for(int i=0;i<entitycount;i++)
                        {
                            var pp =(DynamicObject) this.Model.GetValue("F_TRAX_PINPAI", i);
                            if (pp!=null)
                            {
                                if (pp["Id"].ToString() == date[1])
                                {
                                    double hsdj=double.Parse(this.Model.GetValue("FTAXPRICE", i).ToString());
                                    this.Model.SetValue("F_TRAX_TAXINCLUDEDPRICE",hsdj, i);
                                    this.Model.SetValue("F_TRAX_DECIMAL", date[0],i);
                                    this.Model.SetValue("FTAXPRICE",hsdj*(1+ double.Parse(date[0])/100), i);
                                    //this.View.InvokeFieldUpdateService("F_TRAX_Decimal", i);
                                    //this.View.InvokeFieldUpdateService("FTaxPrice", i);

                                }
                            }
                        }
                        this.View.UpdateView("FEntity");
                    }
                    else if (date[2] == "系列")
                    {
                        for (int i = 0; i < entitycount; i++)
                        {
                            var xl = (DynamicObject)this.Model.GetValue("F_TRAX_XILIE", i);
                            if (xl != null)
                            {
                                if (xl["Id"].ToString() == date[1])
                                {
                                    double hsdj = double.Parse(this.Model.GetValue("FTAXPRICE", i).ToString());
                                    this.Model.SetValue("F_TRAX_TAXINCLUDEDPRICE", hsdj, i);
                                    this.Model.SetValue("F_TRAX_DECIMAL", date[0], i);
                                    this.Model.SetValue("FTAXPRICE", hsdj * (1 + double.Parse(date[0]) / 100), i);
                                    //this.View.InvokeFieldUpdateService("F_TRAX_Decimal", i);
                                    //this.View.InvokeFieldUpdateService("FTaxPrice", i);
                                }
                            }
                        }
                        this.View.UpdateView("FEntity");
                    }
                    else if (date[2] == "物料")
                    {
                        for (int i = 0; i < entitycount; i++)
                        {
                            var wl = (DynamicObject)this.Model.GetValue("FMATERIALID",i);
                            if (wl != null)
                            {
                                if (wl["Id"].ToString() == date[1])
                                {
                                    double hsdj = double.Parse(this.Model.GetValue("FTAXPRICE", i).ToString());
                                    this.Model.SetValue("F_TRAX_TAXINCLUDEDPRICE", hsdj, i);
                                    this.Model.SetValue("F_TRAX_DECIMAL", date[0], i);
                                    this.Model.SetValue("FTAXPRICE", hsdj * (1 + double.Parse(date[0]) / 100), i);
                                    //this.View.InvokeFieldUpdateService("F_TRAX_Decimal", i);
                                    //this.View.InvokeFieldUpdateService("FTaxPrice", i);
                                }
                            }
                        }
                        this.View.UpdateView("FEntity");
                    }
                }
                  );
            }
            
        }
       
    }
}
