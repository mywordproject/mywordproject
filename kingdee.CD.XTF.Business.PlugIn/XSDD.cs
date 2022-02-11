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
    [Description("是否多品牌或品牌改变后删除明细单据体"),HotUpdate]
    public class XSDD:AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_TRAX_SFDPP" || e.Field.Key == "F_TRAX_BRAND")
            {
                this.Model.DeleteEntryData("FSaleOrderEntry");
                this.View.UpdateView("FSaleOrderEntry");
            }
        }
    }
}
