using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.组织间结算
{
    [Description("动态表单---内部价格批量调整")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class NBJGPLTZFormPlugin: AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_TRAX_QD"))
            {
                if (Int32.Parse(this.Model.GetValue("F_TRAX_JJBL").ToString())!=0)
                {
                    string jjbl=this.Model.GetValue("F_TRAX_JJBL").ToString();
                    var pp = ((DynamicObject)this.Model.GetValue("F_TRAX_PP"));
                    var xl = ((DynamicObject)this.Model.GetValue("F_TRAX_XL"));
                    var wl = ((DynamicObject)this.Model.GetValue("F_TRAX_WL"));
                    string[] rs =new string[3];
                    if (pp != null)
                    {
                        rs[0] = jjbl;
                        rs[1] = pp["Id"].ToString();
                        rs[2] = "品牌";
                        this.View.ReturnToParentWindow(rs);                     
                    }
                    else if (xl!= null)
                    {
                        rs[0] = jjbl;
                        rs[1] = xl["Id"].ToString();
                        rs[2] = "系列";
                        this.View.ReturnToParentWindow(rs);
                    }
                    else if (wl != null)
                    {
                        rs[0] = jjbl;
                        rs[1] = wl["Id"].ToString();
                        rs[2] = "物料";
                        this.View.ReturnToParentWindow(rs);
                    }
                    else
                    {
                        throw new KDBusinessException("", "物料、品牌、系列请选择一个");
                    }
                   
                    this.View.Close();                   
                }
                else
                {
                    throw new KDBusinessException("", "加价比例不能为空");
                }
               
            }
           
        }
    }
}
