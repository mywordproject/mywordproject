using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF
{
    [Description("销售价目表---物料特价互斥，取消生效和取消业务--服务单"), HotUpdate]
    public class XSJMBServerPlugin: AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach (var date in e.DataEntitys)
            {
                //客户信息
                string xsjmb = date["Number"].ToString();
                //查询销售价目表
                string xsjmbsql = $@"/*dialect*/
                                select b.FMATERIALID,c.FCUSTID,e.FNUMBER,b.FEFFECTIVEDATE,b.FEXPRIYDATE from T_SAL_PRICELIST a
                                INNER JOIN T_SAL_PRICELISTENTRY b on a.FID = b.FID
                                inner join T_SAL_APPLYCUSTOMER c on a.FID = c.FID
                                INNER JOIN T_BD_MATERIAL e ON e.FMATERIALID=B.FMATERIALID
                                inner join T_BAS_ASSISTANTDATAENTRY f on f.FENTRYID=a.FPRICETYPE
					            inner join T_BAS_ASSISTANTDATA g on f.FID=g.FID 
                                where a.FNUMBER='{xsjmb}' and f.FNUMBER='02'  and g.FNUMBEr='SAL_PriceType' ";
                var xsjmbdates = DBUtils.ExecuteDynamicObject(Context, xsjmbsql);
                if (xsjmbdates.Count > 0)
                {
                    foreach (var xsjmbdate in xsjmbdates)
                    {
                        //查询销售价目表是否有该物料作为特价
                        string xsjmbsqls = $@"/*dialect*/
                                select a.* from T_SAL_PRICELIST a
                                INNER JOIN T_SAL_PRICELISTENTRY b on a.FID = b.FID
                                inner join T_SAL_APPLYCUSTOMER c on a.FID = c.FID
				                inner join T_BD_CUSTOMER d on d.FCUSTID=C.FCUSTID
				                INNER JOIN T_BD_MATERIAL e ON e.FMATERIALID=B.FMATERIALID
                                inner join T_BAS_ASSISTANTDATAENTRY f on f.FENTRYID=a.FPRICETYPE
					            inner join T_BAS_ASSISTANTDATA g on f.FID=g.FID 
                                where b.FMATERIALID='{xsjmbdate["FMATERIALID"].ToString()}' and f.FNUMBER='02'  and g.FNUMBEr='SAL_PriceType' and
                                 c.FCUSTID='{xsjmbdate["FCUSTID"].ToString()}' and a.FDOCUMENTSTATUS='C'and  a.FFORBIDSTATUS='A' AND b.FFORBIDSTATUS='A'   and a.FNUMBER<>'{xsjmb}'
                                and  ((b.FEFFECTIVEDATE<=to_date('{xsjmbdate["FEFFECTIVEDATE"].ToString()}','yyyy-mm-dd hh24:mi:ss') and b.FEXPRIYDATE>=to_date('{xsjmbdate["FEFFECTIVEDATE"].ToString()}','yyyy-mm-dd hh24:mi:ss')) or
                                (b.FEFFECTIVEDATE<=to_date('{xsjmbdate["FEXPRIYDATE"].ToString()}','yyyy-mm-dd hh24:mi:ss') and b.FEXPRIYDATE>=to_date('{xsjmbdate["FEXPRIYDATE"].ToString()}','yyyy-mm-dd hh24:mi:ss')))
                                ";
                        var dates = DBUtils.ExecuteDynamicObject(Context, xsjmbsqls);
                        if (dates.Count > 0)
                        {
                            throw new KDBusinessException("", "该" + xsjmbdate["FNUMBER"].ToString() + "物料在销售价目表有特价");
                        }
                    }
                }                                   
            }
        }
    }
}
