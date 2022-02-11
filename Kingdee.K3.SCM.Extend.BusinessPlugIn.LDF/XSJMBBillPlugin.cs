using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
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
    [Description("销售价目表---物料特价互斥--表单"), HotUpdate]
    public class XSJMBBillPlugin: AbstractBillPlugIn
    {
        //public override void BeforeSave(BeforeSaveEventArgs e)
        //{
        //    base.BeforeSave(e);
        //    //客户信息
        //    var khdates = (DynamicObjectCollection)this.Model.DataObject["SAL_APPLYCUSTOMER"];
        //    foreach(var khdate in khdates)
        //    {
        //        if (khdate["CustID"] != null)
        //        {
        //            //客户
        //            var kh = (DynamicObject)khdate["CustID"];
        //            //分录信息
        //            var entrydates = (DynamicObjectCollection)this.Model.DataObject["SAL_PRICELISTENTRY"];
        //            foreach (var entrydate in entrydates)
        //            {
        //                if (entrydate["MaterialId"] !=null && entrydate["EffectiveDate"] != null && entrydate["ExpiryDate"] != null)
        //                {
        //                //物料
        //                 var wl = (DynamicObject)entrydate["MaterialId"];
        //                 //查询销售价目表是否有该物料作为特价
        //                 string xsjmbsql = $@"/*dialect*/
        //                select c.* from T_SAL_PRICELIST a
        //                        INNER JOIN T_SAL_PRICELISTENTRY b on a.FID = b.FID
        //                        inner join T_SAL_APPLYCUSTOMER c on a.FID = c.FID
				    //            inner join T_BD_CUSTOMER d on d.FCUSTID=C.FCUSTID
				    //            INNER JOIN T_BD_MATERIAL e ON e.FMATERIALID=B.FMATERIALID
        //                        inner join T_BAS_ASSISTANTDATAENTRY f on f.FENTRYID=a.FPRICETYPE
					   //         inner join T_BAS_ASSISTANTDATA g on f.FID=g.FID 
        //                        where b.FMATERIALID='{wl["Id"].ToString()}' and f.FNUMBER='02'  and g.FNUMBEr='SAL_PriceType' and
        //                         c.FCUSTID='{kh["Id"].ToString()}' and a.FDOCUMENTSTATUS='C'and  a.FFORBIDSTATUS='A' AND b.FFORBIDSTATUS='A' 
        //                and  ((b.FEFFECTIVEDATE<=to_date('{entrydate["EffectiveDate"].ToString()}','yyyy-mm-dd hh24:mi:ss') 
        //                   and b.FEXPRIYDATE>=to_date('{entrydate["EffectiveDate"].ToString()}','yyyy-mm-dd hh24:mi:ss')) or
        //                (b.FEFFECTIVEDATE<=to_date('{entrydate["ExpiryDate"].ToString()}','yyyy-mm-dd hh24:mi:ss') 
        //                and b.FEXPRIYDATE>=to_date('{entrydate["ExpiryDate"].ToString()}','yyyy-mm-dd hh24:mi:ss')))
        //                ";
        //                var xsjmbdate = DBUtils.ExecuteDynamicObject(Context, xsjmbsql);
        //                    if (xsjmbdate.Count > 0)
        //                    {
        //                        throw new KDBusinessException("", "该" +wl["Number"].ToString()+"物料在销售价目表有特价");
        //                    }
        //                }             
        //            }
        //        }              
        //    }
        //}
       //public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
       //{
       //    base.AfterEntryBarItemClick(e);
       //    //客户信息
       //    var khdates = (DynamicObjectCollection)this.Model.DataObject["SAL_APPLYCUSTOMER"];
       //    string bm = this.Model.GetValue("FNUMBER").ToString();
       //    foreach (var khdate in khdates)
       //    {
       //        if (khdate["CustID"] != null)
       //        {
       //            //客户
       //            var kh = (DynamicObject)khdate["CustID"];
       //            //分录信息
       //            var entrydates = (DynamicObjectCollection)this.Model.DataObject["SAL_PRICELISTENTRY"];
       //            foreach (var entrydate in entrydates)
       //            {
       //                if (entrydate["MaterialId"] != null && entrydate["EffectiveDate"] != null && entrydate["ExpiryDate"] != null)
       //                {
       //                    //物料
       //                    var wl = (DynamicObject)entrydate["MaterialId"];
       //                    //查询销售价目表是否有该物料作为特价
       //                    string xsjmbsql = $@"/*dialect*/
       //                select c.* from T_SAL_PRICELIST a
       //                        INNER JOIN T_SAL_PRICELISTENTRY b on a.FID = b.FID
       //                        inner join T_SAL_APPLYCUSTOMER c on a.FID = c.FID
	   //	                inner join T_BD_CUSTOMER d on d.FCUSTID=C.FCUSTID
		//	                INNER JOIN T_BD_MATERIAL e ON e.FMATERIALID=B.FMATERIALID
       //                        inner join T_BAS_ASSISTANTDATAENTRY f on f.FENTRYID=a.FPRICETYPE
		//		            inner join T_BAS_ASSISTANTDATA g on f.FID=g.FID 
       //                        where b.FMATERIALID='{wl["Id"].ToString()}' and f.FNUMBER='02'  and g.FNUMBEr='SAL_PriceType' and a.FNUMBER<>'{bm}' AND
       //                         c.FCUSTID='{kh["Id"].ToString()}' and a.FDOCUMENTSTATUS='C'and  a.FFORBIDSTATUS='A' AND b.FFORBIDSTATUS='A' 
       //                and  ((b.FEFFECTIVEDATE<=to_date('{entrydate["EffectiveDate"].ToString()}','yyyy-mm-dd hh24:mi:ss') 
       //                   and b.FEXPRIYDATE>=to_date('{entrydate["EffectiveDate"].ToString()}','yyyy-mm-dd hh24:mi:ss')) or
       //                (b.FEFFECTIVEDATE<=to_date('{entrydate["ExpiryDate"].ToString()}','yyyy-mm-dd hh24:mi:ss') 
       //                and b.FEXPRIYDATE>=to_date('{entrydate["ExpiryDate"].ToString()}','yyyy-mm-dd hh24:mi:ss')))
       //                ";
       //                    var xsjmbdate = DBUtils.ExecuteDynamicObject(Context, xsjmbsql);
       //                    if (xsjmbdate.Count > 0)
       //                    {
       //                        throw new KDBusinessException("", "该" + wl["Number"].ToString() + "物料在销售价目表有特价");
       //                    }
       //                }
       //            }
       //        }
       //    }
       //}
    }
}
