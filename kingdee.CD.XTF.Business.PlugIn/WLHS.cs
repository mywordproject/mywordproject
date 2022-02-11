using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using System.ComponentModel;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;

namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("物料换算删除"),HotUpdate]
    public class WLHS: AbstractListPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals("tbDelete", StringComparison.OrdinalIgnoreCase))
            {
                ListSelectedRowCollection a = this.ListView.SelectedRowsInfo;
                foreach(var dates in a)
                {
                    var date = dates.DataRow;
                   string WLHS= date["FMaterialId_Id"].ToString();
                   string sql = $@"/*dialect*/UPDATE T_BD_MATERIAL SET F_TRAX_SFSCWLHS = '0' WHERE FMATERIALID ='{WLHS}'";
                   DBUtils.Execute(Context, sql);
                }
            }

        }
    }
}
