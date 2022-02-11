using ingdee.K3.SCM.Extend.BusinessPlugIn;
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

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.代垫管理
{
    [Description("销售订单--通过物料带出成本价")]
    //热启动,不用重启IIS
    [HotUpdate]
    public class XSDDBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.FieldName == "FMATERIALID")
            {
                var wl = (DynamicObject)this.Model.GetValue("FMATERIALID", e.Row);//物料
                var kh = (DynamicObject)this.Model.GetValue("FCUSTID");//客户
                if (wl != null && kh != null && Convert.ToBoolean(this.Model.GetValue("FISFREE", e.Row).ToString()) == false)
                {
                    DateTime rq = DateTime.Parse(this.Model.GetValue("FDATE").ToString());//开始日期
                    string klsql = $@"/*dialect*/
                      select b.F_TRAX_QDDYPRICE,
ROUND(CASE WHEN b.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (b.F_TRAX_QDDYPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE b.F_TRAX_QDDYPRICE END,6) QDJG
,
b.* 
from T_SON_KVcontract a
inner join T_SON_KVcontractEntry b on a.FID=b.FID
INNER JOIN T_BD_MATERIAL WL
ON b.F_TRAX_MATERIALID=WL.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS
ON WLHS.FMASTERID=WL.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW
ON WL.FMATERIALID=WLDW.FMATERIALID
WHERE F_TRAX_MATERIALID='{wl["Id"]}'
AND F_TRAX_CUSTOMER='{kh["Id"]}'
and a.FDOCUMENTSTATUS='C'
and F_TRAX_STARTDATE<=to_date(SYSDATE) 
and F_TRAX_ENDDATE>=to_date(SYSDATE)";
                    var klhtdate = DBUtils.ExecuteDynamicObject(Context, klsql);
                    if (klhtdate.Count > 0)
                    {
                        this.Model.SetValue("F_TRAX_BODISCOUNT", klhtdate[0]["F_TRAX_BODISCOUNT"].ToString(), e.Row);//BO扣率%
                        this.Model.SetValue("F_TRAX_BOCOST", klhtdate[0]["F_TRAX_BOCOST"].ToString(), e.Row);//BO费用
                        this.Model.SetValue("F_TRAX_51380DISCOUNT", klhtdate[0]["F_TRAX_51380DISCOUNT"].ToString(), e.Row);//51380扣率%
                        this.Model.SetValue("F_TRAX_51380COST", klhtdate[0]["F_TRAX_51380COST"].ToString(), e.Row);//51380费用
                        this.Model.SetValue("F_TRAX_OTHERDISCOUNT", klhtdate[0]["F_TRAX_OTHERDISCOUNT"].ToString(), e.Row);//其他扣率%
                        this.Model.SetValue("F_TRAX_OTHERCOST", klhtdate[0]["F_TRAX_OTHERCOST"].ToString(), e.Row);//其他费用
                        this.Model.SetValue("F_TRAX_MANUFACTURERRATIO", klhtdate[0]["F_TRAX_MANUFACTURERRATIO"].ToString(), e.Row);//厂商承担率%
                        this.Model.SetValue("F_TRAX_MANUFACTURERCOST", klhtdate[0]["F_TRAX_MANUFACTURERCOST"].ToString(), e.Row);//厂商承担金额
                        this.Model.SetValue("F_TRAX_QDDYJ", klhtdate[0]["QDJG"].ToString(), e.Row);//渠道打印价
                    }
                    else if (klhtdate.Count<=0)
                    {
                       var KHID = kh["Id"];
                       string khidsql = $@"/*dialect*/SELECT FGROUPCUSTID FROM T_BD_CUSTOMER WHERE FCUSTID='{KHID}'";
                       var khid = DBUtils.ExecuteDynamicObject(Context, khidsql);
                        if (Convert.ToInt32( khid[0]["FGROUPCUSTID"])!=0)
                        {
                            foreach (var item in khid)
                            {
                                string kljt = $@"/*dialect*/
                      select b.F_TRAX_QDDYPRICE,
ROUND(CASE WHEN b.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (b.F_TRAX_QDDYPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE b.F_TRAX_QDDYPRICE END,6) QDJG
,
b.* 
from T_SON_KVcontract a
inner join T_SON_KVcontractEntry b on a.FID=b.FID
INNER JOIN T_BD_MATERIAL WL
ON b.F_TRAX_MATERIALID=WL.FMATERIALID
INNER JOIN T_BD_UNITCONVERTRATE WLHS
ON WLHS.FMASTERID=WL.FMASTERID
INNER JOIN t_BD_MaterialBase WLDW
ON WL.FMATERIALID=WLDW.FMATERIALID
WHERE F_TRAX_MATERIALID='{wl["Id"]}'
AND F_TRAX_CUSTOMER='{khid}'
and a.FDOCUMENTSTATUS='C'
and F_TRAX_STARTDATE<=to_date(SYSDATE) 
and F_TRAX_ENDDATE>=to_date(SYSDATE)";
                                var kljtdate = DBUtils.ExecuteDynamicObject(Context, kljt);
                                if (kljtdate.Count > 0)
                                {
                                    this.Model.SetValue("F_TRAX_BODISCOUNT", kljtdate[0]["F_TRAX_BODISCOUNT"].ToString(), e.Row);//BO扣率%
                                    this.Model.SetValue("F_TRAX_BOCOST", kljtdate[0]["F_TRAX_BOCOST"].ToString(), e.Row);//BO费用
                                    this.Model.SetValue("F_TRAX_51380DISCOUNT", kljtdate[0]["F_TRAX_51380DISCOUNT"].ToString(), e.Row);//51380扣率%
                                    this.Model.SetValue("F_TRAX_51380COST", kljtdate[0]["F_TRAX_51380COST"].ToString(), e.Row);//51380费用
                                    this.Model.SetValue("F_TRAX_OTHERDISCOUNT", kljtdate[0]["F_TRAX_OTHERDISCOUNT"].ToString(), e.Row);//其他扣率%
                                    this.Model.SetValue("F_TRAX_OTHERCOST", kljtdate[0]["F_TRAX_OTHERCOST"].ToString(), e.Row);//其他费用
                                    this.Model.SetValue("F_TRAX_MANUFACTURERRATIO", kljtdate[0]["F_TRAX_MANUFACTURERRATIO"].ToString(), e.Row);//厂商承担率%
                                    this.Model.SetValue("F_TRAX_MANUFACTURERCOST", kljtdate[0]["F_TRAX_MANUFACTURERCOST"].ToString(), e.Row);//厂商承担金额
                                    this.Model.SetValue("F_TRAX_QDDYJ", kljtdate[0]["QDJG"].ToString(), e.Row);//渠道打印价
                                }
                                //else
                                //{
                                //    this.View.ShowMessage("该物料没有BO扣率%、BO费用、51380扣率%等");
                                //}
                            }
                        }
                    }
                    //买点合同
                    string mdsql = $@"/*dialect*/
                    select 
                    ROUND(CASE WHEN b.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (b.F_TRAX_QDDYJ/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE b.F_TRAX_QDDYJ END,6) QDJG, 
                    b.* from T_SON_MDHT a
                    inner join T_SON_MDHTentity b on a.FID=b.FID
                    INNER JOIN T_BD_MATERIAL WL
                    ON WL.FMATERIALID=b.F_TRAX_WLBM
                    INNER JOIN T_BD_UNITCONVERTRATE WLHS
                    ON WLHS.FMASTERID=WL.FMASTERID
                    INNER JOIN t_BD_MaterialBase WLDW
                    ON WL.FMATERIALID=WLDW.FMATERIALID
                    WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C'
                    and F_TRAX_SXRQM<=to_date(SYSDATE) 
                    and  F_TRAX_SXRQQM>=to_date(SYSDATE)";
                    var mdhtdate = DBUtils.ExecuteDynamicObject(Context, mdsql);
                    if (mdhtdate.Count > 0)
                    {
                        this.Model.SetValue("F_TRAX_BODISCOUNT", mdhtdate[0]["F_TRAX_BOZKL"].ToString(), e.Row);//BO扣率%
                        this.Model.SetValue("F_TRAX_BOCOST", mdhtdate[0]["F_TRAX_BOFY"].ToString(), e.Row);//BO费用
                        this.Model.SetValue("F_TRAX_51380DISCOUNT", mdhtdate[0]["F_TRAX_51380ZKL"].ToString(), e.Row);//51380扣率%
                        this.Model.SetValue("F_TRAX_51380COST", mdhtdate[0]["F_TRAX_51380FY"].ToString(), e.Row);//51380费用
                        this.Model.SetValue("F_TRAX_OTHERDISCOUNT", mdhtdate[0]["F_TRAX_QTKL"].ToString(), e.Row);//其他扣率%
                        this.Model.SetValue("F_TRAX_OTHERCOST", mdhtdate[0]["F_TRAX_QTFY"].ToString(), e.Row);//其他费用
                        this.Model.SetValue("F_TRAX_MANUFACTURERRATIO", mdhtdate[0]["F_TRAX_CJCDL"].ToString(), e.Row);//厂商承担率%
                        this.Model.SetValue("F_TRAX_MANUFACTURERCOST", mdhtdate[0]["F_TRAX_CJCDJE"].ToString(), e.Row);//厂商承担金额
                        this.Model.SetValue("F_TRAX_QDDYJ", mdhtdate[0]["QDJG"].ToString(), e.Row);//渠道打印价
                    }
                    else if (mdhtdate.Count<=0)
                    {
                        var KHID = kh["Id"];
                        string khidsql = $@"/*dialect*/SELECT FGROUPCUSTID FROM T_BD_CUSTOMER WHERE FCUSTID='{KHID}'";
                        var mdkhid = DBUtils.ExecuteDynamicObject(Context, khidsql);
                        if (Convert.ToInt32(mdkhid[0]["FGROUPCUSTID"]) != 0)
                        {
                            foreach (var item in mdkhid)
                            {
                                string mdkhsql = $@"/*dialect*/
                    select 
                    ROUND(CASE WHEN b.F_TRAX_JJDW != WLDW.FBASEUNITID THEN (b.F_TRAX_QDDYJ/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE b.F_TRAX_QDDYJ END,6) QDJG, 
                    b.* from T_SON_MDHT a
                    inner join T_SON_MDHTentity b on a.FID=b.FID
                    INNER JOIN T_BD_MATERIAL WL
                    ON WL.FMATERIALID=b.F_TRAX_WLBM
                    INNER JOIN T_BD_UNITCONVERTRATE WLHS
                    ON WLHS.FMASTERID=WL.FMASTERID
                    INNER JOIN t_BD_MaterialBase WLDW
                    ON WL.FMATERIALID=WLDW.FMATERIALID
                    WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{mdkhid}' and a.FDOCUMENTSTATUS='C'
                    and F_TRAX_SXRQM<=to_date(SYSDATE) 
                    and  F_TRAX_SXRQQM>=to_date(SYSDATE)";
                                var mdkhtdate = DBUtils.ExecuteDynamicObject(Context, mdkhsql);
                                if (mdkhtdate.Count > 0)
                                {
                                    this.Model.SetValue("F_TRAX_BODISCOUNT", mdkhtdate[0]["F_TRAX_BOZKL"].ToString(), e.Row);//BO扣率%
                                    this.Model.SetValue("F_TRAX_BOCOST", mdkhtdate[0]["F_TRAX_BOFY"].ToString(), e.Row);//BO费用
                                    this.Model.SetValue("F_TRAX_51380DISCOUNT", mdkhtdate[0]["F_TRAX_51380ZKL"].ToString(), e.Row);//51380扣率%
                                    this.Model.SetValue("F_TRAX_51380COST", mdkhtdate[0]["F_TRAX_51380FY"].ToString(), e.Row);//51380费用
                                    this.Model.SetValue("F_TRAX_OTHERDISCOUNT", mdkhtdate[0]["F_TRAX_QTKL"].ToString(), e.Row);//其他扣率%
                                    this.Model.SetValue("F_TRAX_OTHERCOST", mdkhtdate[0]["F_TRAX_QTFY"].ToString(), e.Row);//其他费用
                                    this.Model.SetValue("F_TRAX_MANUFACTURERRATIO", mdkhtdate[0]["F_TRAX_CJCDL"].ToString(), e.Row);//厂商承担率%
                                    this.Model.SetValue("F_TRAX_MANUFACTURERCOST", mdkhtdate[0]["F_TRAX_CJCDJE"].ToString(), e.Row);//厂商承担金额
                                    this.Model.SetValue("F_TRAX_QDDYJ", mdkhtdate[0]["QDJG"].ToString(), e.Row);//渠道打印价
                                }
                            }
                        }

                    }
                    //折扣合同
                    string zksql = $@"/*dialect*/
                     select * from T_SON_ZKHT a
                     inner join T_SON_ZKHTEntity b on a.T_SON_ZKHT=b.T_SON_ZKHT
                     WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C'
                     and F_TRAX_SXRQ<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                     and F_TRAX_SXRQ2>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss')";
                    var zkdate = DBUtils.ExecuteDynamicObject(Context, zksql);
                    if (zkdate.Count > 0)
                    {
                        this.Model.SetValue("F_TRAX_DSZK", zkdate[0]["F_TRAX_DSZK"].ToString(), e.Row);//对私折扣
                        this.Model.SetValue("F_TRAX_DSZKL", zkdate[0]["F_TRAX_DSZKL"].ToString(), e.Row);//对私折扣率
                        this.Model.SetValue("F_TRAX_DKHZK", zkdate[0]["F_TRAX_DKHZK"].ToString(), e.Row);//对客户折扣
                        this.Model.SetValue("F_TRAX_DKHZKL", zkdate[0]["F_TRAX_DKHZKL"].ToString(), e.Row); //对客户折扣率
                        this.Model.SetValue("F_TRAX_TDFY", zkdate[0]["F_TRAX_TDFY"].ToString(), e.Row);//TD费用
                        this.Model.SetValue("F_TRAX_TDFYL", zkdate[0]["F_TRAX_TDFYL"].ToString(), e.Row);//TD费用率%
                        this.Model.SetValue("F_TRAX_DDZKJE", zkdate[0]["F_TRAX_DDZKJE"].ToString(), e.Row);//代垫折扣金额
                        this.Model.SetValue("F_TRAX_DDZK", zkdate[0]["F_TRAX_DDZK"].ToString(), e.Row);//代垫折扣%
                        this.Model.SetValue("F_TRAX_ZDZKJE", zkdate[0]["F_TRAX_ZDZKJE"].ToString(), e.Row);//自担折扣金额
                        this.Model.SetValue("F_TRAX_ZDZK", zkdate[0]["F_TRAX_ZDZK"].ToString(), e.Row);//自担折扣%
                        this.Model.SetValue("F_TRAX_HTWTJFLJE", zkdate[0]["F_TRAX_HTWTJFLJE"].ToString(), e.Row);//合同返利无条件返利金额
                        this.Model.SetValue("F_TRAX_HTWTJFL", zkdate[0]["F_TRAX_HTWTJFL"].ToString(), e.Row);//合同返利&无条件返利%
                        this.Model.SetValue("F_TRAX_DSZKDJ", zkdate[0]["F_TRAX_DSZKDJ"].ToString(), e.Row);// 对私折扣单价
                        this.Model.SetValue("F_TRAX_DKHZKDJ", zkdate[0]["F_TRAX_DKHZKDJ"].ToString(), e.Row);//对客户折扣单价
                        this.Model.SetValue("F_TRAX_TDFYDJ", zkdate[0]["F_TRAX_TDFYDJ"].ToString(), e.Row);//TD费用单价
                    }
                    //else
                    //{
                    //    this.View.ShowMessage("该物料没有折扣合同");
                    //}
                }
                else if (wl != null && kh != null && Convert.ToBoolean(this.Model.GetValue("FISFREE", e.Row).ToString()) == true)
                {

                    string cxzcid = this.Model.GetValue("FSPMENTRYID", e.Row) == null ? "" : this.Model.GetValue("FSPMENTRYID", e.Row).ToString();
                    //促销活动
                    string cxhdsql = $@"/*dialect*/
                          select * from T_SPM_PromotionPolicy a
                          inner join T_SPM_PromotionPolicyEntry b on a.FID=b.FID
                          WHERE FENTRYID='{cxzcid}' and FPRESENTTYPE=1 ";
                    var cxhd = DBUtils.ExecuteDynamicObject(Context, cxhdsql);

                    this.Model.SetValue("F_TRAX_GSCDBL", cxhd[0]["F_TRAX_GSCDBL"].ToString(), e.Row);//公司比例
                    this.Model.SetValue("F_TRAX_CJCDBL", cxhd[0]["F_TRAX_CJCDBL"].ToString(), e.Row);//厂家比例
                    var wul = Utils.LoadBDData(Context, "BD_MATERIAL", wl["Number"].ToString());
                    //FCOMBRANDID_CMK F_TRAX_BRAND              
                    string ppsql = $@"/*dialect*/
                    select * from  T_BD_Brand a
                    inner join T_BD_XGSupplierEntity b on a.FID=b.FID 
                    where a.FID='{ wl["F_TRAX_Brand_Id"].ToString()}' 
                    and a.FDOCUMENTSTATUS='C'
                    ";
                    var DATE = DBUtils.ExecuteDynamicObject(Context, ppsql);
                    if (DATE.Count > 0)
                    {
                        foreach (var da in DATE)
                        {//0 单品默认成本，1 指定供应商采购标准价
                            if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 0)
                            {
                                //默认成本
                                string mrcbsql = $@"/*dialect*/
                                    select * from  T_SON_DPMRCBentity a
                                    inner join  T_SON_DPMRCB b on a.FID=b.FID  
                                    where F_TRAX_WLBM='{wl["Id"].ToString()}' and b.FDOCUMENTSTATUS='C'";
                                var mrcb = DBUtils.ExecuteDynamicObject(Context, mrcbsql);
                                if (mrcb.Count > 0)
                                {
                                    this.Model.SetValue("F_TRAX_UNITPRICE", mrcb[0]["F_TRAX_MRCB"].ToString(), e.Row);
                                    this.View.InvokeFieldUpdateService("F_TRAX_UNITPRICE", e.Row);
                                    // this.View.UpdateView("FSaleOrderEntry");
                                }
                            }
                            else if (Convert.ToInt32(da["F_TRAX_DDJGLX"].ToString()) == 1)
                            {
                                string cgjmbsql = $@"/*dialect*/
                                    select  FTAXPRICE from t_PUR_PriceList a
                                    inner join t_PUR_PriceListEntry b on a.fid=b.fid
                                    where  FMATERIALID='{wl["Id"]}' and a.FDOCUMENTSTATUS='C' 
                                    and FSUPPLIERID = '{da["F_TRAX_JGSUPPLIER"]}' ";
                                var cgjmb = DBUtils.ExecuteDynamicObject(Context, cgjmbsql);
                                if (cgjmb.Count > 0)
                                {
                                    this.Model.SetValue("F_TRAX_UNITPRICE", cgjmb[0]["FTAXPRICE"].ToString(), e.Row);
                                    this.View.InvokeFieldUpdateService("F_TRAX_UNITPRICE", e.Row);
                                }
                            }

                        }

                    }

                }
            }
        }

        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            var dates = this.Model.DataObject["SaleOrderEntry"] as DynamicObjectCollection;
            if (dates.Count > 0)
            {
                foreach (var date in dates)
                {
                    var wl = (DynamicObject)this.Model.GetValue("FMATERIALID", Convert.ToInt32(date["Seq"].ToString()) - 1);//物料
                    var kh = (DynamicObject)this.Model.GetValue("FCUSTID");//客户
                    if (wl != null && kh != null && Convert.ToBoolean(this.Model.GetValue("FISFREE", Convert.ToInt32(date["Seq"].ToString()) - 1).ToString()) == false)
                    {
                        DateTime rq = DateTime.Parse(this.Model.GetValue("FDATE").ToString());//开始日期 
                                                                                              //扣率合同
                        string klsql = $@"/*dialect*/
                       select b.FENTRYID,
                         b.F_TRAX_MATERIALID,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_FQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         count(F_TRAX_YYZXLZB) as hs
                         from T_SON_KVcontract a
                         inner join T_SON_KVcontractEntry b on a.FID=b.FID
                         LEFT join T_SON_SYcontractEntry c on b.FENTRYID=c.FENTRYID
                         WHERE F_TRAX_MATERIALID='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C' and F_TRAX_SFKL=1
                         and F_TRAX_STARTDATE<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and F_TRAX_ENDDATE>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss')
                         GROUP BY
                         b.FENTRYID,
                         b.F_TRAX_MATERIALID,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_FQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH ";
                        var klhtdate = DBUtils.ExecuteDynamicObject(Context, klsql);
                        if (klhtdate.Count > 0)
                        {
                            if (klhtdate[0]["F_TRAX_FQKL"].ToString() == "0")
                            {                        
                                    //总数
                                    string zssql = $@"
                                 insert into T_SON_SYcontractEntry
                                 (FENTRYID,FDETAILID,FSEQ,F_TRAX_YYZXLZB,F_TRAX_DDBH,F_TRAX_DDHH)
                                  VALUES
                                 ({Convert.ToInt32(klhtdate[0]["FENTRYID"].ToString())},
                                 seq_yxk.nextval,
                                 {Convert.ToInt32(klhtdate[0]["hs"]) + 1},
                                 {Convert.ToDouble(date["BaseUnitQty"].ToString())},
                                 '{this.Model.GetValue("FBILLNO").ToString()}',
                                 '{date["Seq"].ToString()}') ";
                                    DBUtils.Execute(Context, zssql);
                           
                            }
                            else if (klhtdate[0]["F_TRAX_FQKL"].ToString() == "1")
                            {
                                int mon = Convert.ToInt32(DateTime.Now.Month.ToString()) - 1;//获取当前时间月份
                                string[] yf = {
                                    "F_TRAX_YYJANUARYSALES",
                                    "F_TRAX_YYFEBRUARYSALES",
                                    "F_TRAX_YYMARCHSALES",
                                    "F_TRAX_YYAPRILSALES",
                                    "F_TRAX_YYMAYSALES",
                                    "F_TRAX_YYJUNESALES",
                                    "F_TRAX_YYJULYSALES",
                                    "F_TRAX_YYAUGUSTSALES",
                                    "F_TRAX_YYSEPTEMBERSALES",
                                    "F_TRAX_YYOCTOBERSALES",
                                    "F_TRAX_YYNOVEMBERSALES",
                                    "F_TRAX_YYDECEMBERSALES" };                               
                                    string zssql = $@"
                                 insert into T_SON_SYcontractEntry
                                 (FENTRYID,FDETAILID,FSEQ,{yf[mon]},F_TRAX_DDBH,F_TRAX_DDHH)
                                  VALUES
                                 ({Convert.ToInt32(klhtdate[0]["FENTRYID"].ToString())},
                                 seq_yxk.nextval,
                                 {Convert.ToInt32(klhtdate[0]["hs"]) + 1},
                                 {Convert.ToDouble(date["BaseUnitQty"].ToString())},
                                 '{this.Model.GetValue("FBILLNO").ToString()}',
                                 '{date["Seq"].ToString()}') ";
                                 DBUtils.Execute(Context, zssql);                               
                            }
                        }
                        else
                        {
                            //买点合同
                            string mdsql = $@"/*dialect*/
                         select 
                          b.FENTRYID,
                         b.F_TRAX_WLBM,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_SFFQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         count(F_TRAX_YYZXLZB) as hs
                         from T_SON_MDHT a
                         inner join T_SON_MDHTentity b on a.FID=b.FID
                         left JOIN T_SON_SYXXbEntity c on b.FENTRYID=c.FENTRYID
                          WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C' and F_TRAX_SFKL=1
                         and F_TRAX_SXRQM<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and  F_TRAX_SXRQQM>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss')
                         GROUP BY
                         b.FENTRYID,
                         b.F_TRAX_WLBM,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_SFFQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH ";
                            var mdhtdate = DBUtils.ExecuteDynamicObject(Context, mdsql);
                            if (mdhtdate.Count > 0)
                            {
                                if (mdhtdate[0]["F_TRAX_SFFQKL"].ToString() == "0")
                                {
                                    //校验是否已经插入
                                   // string jysql = $@"SELECT* from T_SON_SYXXbEntity where FENTRYID = '{mdhtdate[0]["FENTRYID"].ToString()}' 
                                   // and F_TRAX_DDHH = '{date["Seq"].ToString()}' and F_TRAX_DDBH = '{this.Model.GetValue("FBILLNO").ToString()}'";
                                  //  var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                  //  if (jy.Count > 0)
                                  //  {//更新数量
                                   //     string gxsql = $@"
                                   //     update T_SON_SYXXbEntity
                                   //     set F_TRAX_YYZXLZB={Convert.ToDouble(date["BaseUnitQty"].ToString())}
                                   //     where FENTRYID = '{mdhtdate[0]["FENTRYID"].ToString()}' 
                                   //     and F_TRAX_DDHH = '{date["Seq"].ToString()}' and
                                   //     F_TRAX_DDBH = '{this.Model.GetValue("FBILLNO").ToString()}'";
                                   //     DBUtils.Execute(Context, gxsql);
                                   // }
                                   // else
                                   // {
                                        //总数
                                        string zssql = $@"
                                 insert into T_SON_SYXXbEntity
                                 (FENTRYID,FDETAILID,FSEQ,F_TRAX_YYZXLZB,F_TRAX_DDBH,F_TRAX_DDHH)
                                  VALUES
                                 ({Convert.ToInt32(mdhtdate[0]["FENTRYID"].ToString())},
                                 seq_yxk.nextval,
                                 {Convert.ToInt32(mdhtdate[0]["hs"]) + 1},
                                 {Convert.ToDouble(date["BaseUnitQty"].ToString())},
                                 '{this.Model.GetValue("FBILLNO").ToString()}',
                                 '{date["Seq"].ToString()}') ";
                                        DBUtils.Execute(Context, zssql);
                                   // }
                                }
                                else if (mdhtdate[0]["F_TRAX_SFFQKL"].ToString() == "1")
                                {
                                    int mon = Convert.ToInt32(DateTime.Now.Month.ToString()) - 1;//获取当前时间月份
                                    string[] yf = {
                                    "F_TRAX_YYJANUARYSALES",
                                    "F_TRAX_YYFEBRUARYSALES",
                                    "F_TRAX_YYMARCHSALES",
                                    "F_TRAX_YYAPRILSALES",
                                    "F_TRAX_YYMAYSALES",
                                    "F_TRAX_YYJUNESALES",
                                    "F_TRAX_YYJULYSALES",
                                    "F_TRAX_YYAUGUSTSALES",
                                    "F_TRAX_YYSEPTEMBERSALES",
                                    "F_TRAX_YYOCTOBERSALES",
                                    "F_TRAX_YYNOVEMBERSALES",
                                    "F_TRAX_YYDECEMBERSALES" };
                                    
                                  string zssql = $@"
                                 insert into T_SON_SYXXbEntity
                                 (FENTRYID,FDETAILID,FSEQ,{yf[mon]},F_TRAX_DDBH,F_TRAX_DDHH)
                                  VALUES
                                 ({Convert.ToInt32(mdhtdate[0]["FENTRYID"].ToString())},
                                 seq_yxk.nextval,
                                 {Convert.ToInt32(mdhtdate[0]["hs"]) + 1},
                                 {Convert.ToDouble(date["BaseUnitQty"].ToString())},
                                 '{this.Model.GetValue("FBILLNO").ToString()}',
                                 '{date["Seq"].ToString()}') ";
                                        DBUtils.Execute(Context, zssql);
                            
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
          //  if(this.Model.GetValue("F_TRAX_SFKL")==True)
            var dates = this.Model.DataObject["SaleOrderEntry"] as DynamicObjectCollection;
            if (dates.Count > 0)
            {
                foreach (var date in dates)
                {
                    var wl = (DynamicObject)this.Model.GetValue("FMATERIALID", Convert.ToInt32(date["Seq"].ToString()) - 1);//物料
                    var kh = (DynamicObject)this.Model.GetValue("FCUSTID");//客户
                    if (wl != null && kh != null && Convert.ToBoolean(this.Model.GetValue("FISFREE", Convert.ToInt32(date["Seq"].ToString()) - 1).ToString()) == false)
                    {
                        DateTime rq = DateTime.Parse(this.Model.GetValue("FDATE").ToString());//开始日期 
                        string kljysql = $@"/*dialect*/
                      select * from T_SON_KVcontract a
                      inner join T_SON_KVcontractEntry b on a.FID=b.FID
                      WHERE F_TRAX_MATERIALID='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and F_TRAX_SFKL=1 and a.FDOCUMENTSTATUS='C'
                         and F_TRAX_STARTDATE<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and F_TRAX_ENDDATE>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss' )";
                        var kljydate = DBUtils.ExecuteDynamicObject(Context, kljysql);
                        if (kljydate.Count > 0)
                        {
                            if (this.Model.GetValue("FBILLNO")!= null)
                            {
                                //校验是否已经插入
                                string jysql = $@"SELECT * from T_SON_SYcontractEntry where FENTRYID = '{kljydate[0]["FENTRYID"].ToString()}' 
                                    and F_TRAX_DDHH = '{date["Seq"].ToString()}' and F_TRAX_DDBH = '{this.Model.GetValue("FBILLNO").ToString()}'";
                                var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                if (jy.Count > 0)
                                {
                                    string scsql = $@"
                                    delete T_SON_SYcontractEntry where FDETAILID='{jy[0]["FDETAILID"]}'";
                                    DBUtils.Execute(Context, scsql);
                                }
                            }
                            
                           
                            //扣率合同
                            string klsql = $@"/*dialect*/
                        select                   b.FENTRYID,
                         b.F_TRAX_MATERIALID,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_FQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         count(F_TRAX_YYZXLZB) as hs,
                         case when b.F_TRAX_ZXLZBBASE is null then 0 else b.F_TRAX_ZXLZBBASE end -case when sum(F_TRAX_YYZXLZB) is null then 0 else sum(F_TRAX_YYZXLZB) end zs,
                         case when b.F_TRAX_JANUARYSALESBASE is null then 0 else b.F_TRAX_JANUARYSALESBASE end -case when sum(F_TRAX_YYJANUARYSALES) is null then 0 else sum(F_TRAX_YYJANUARYSALES) end yi,              
                         case when b.F_TRAX_FEBRUARYSALESBASE is null then 0 else b.F_TRAX_FEBRUARYSALESBASE end- case when sum(F_TRAX_YYFEBRUARYSALES) is null then 0 else sum(F_TRAX_YYFEBRUARYSALES) end er,   
                         case when b.F_TRAX_MARCHSALESBASE is null then 0 else b.F_TRAX_MARCHSALESBASE end- case when sum(F_TRAX_YYMARCHSALES) is null then 0 else sum(F_TRAX_YYMARCHSALES) end san,  
                         case when b.F_TRAX_APRILSALESBASE is null then 0 else b.F_TRAX_APRILSALESBASE end- case when sum(F_TRAX_YYAPRILSALES) is null then 0 else sum(F_TRAX_YYAPRILSALES) end si,      
                         case when b.F_TRAX_MAYSALESBASE is null then 0 else b.F_TRAX_MAYSALESBASE end- case when sum(F_TRAX_YYMAYSALES) is null then 0 else sum(F_TRAX_YYMAYSALES) end wu,     
                         case when b.F_TRAX_JUNESALESBASE is null then 0 else b.F_TRAX_JUNESALESBASE end- case when sum(F_TRAX_YYJUNESALES) is null then 0 else sum(F_TRAX_YYJUNESALES) end liu,
                         case when b.F_TRAX_JULYSALESBASE is null then 0 else b.F_TRAX_JULYSALESBASE end- case when sum(F_TRAX_YYJULYSALES) is null then 0 else sum(F_TRAX_YYJULYSALES) end qi,
                         case when b.F_TRAX_AUGUSTSALESBASE is null then 0 else b.F_TRAX_AUGUSTSALESBASE end- case when sum(F_TRAX_YYAUGUSTSALES) is null then 0 else sum(F_TRAX_YYAUGUSTSALES) end ba,
                         case when b.F_TRAX_SEPTEMBERSALESBASE is null then 0 else b.F_TRAX_SEPTEMBERSALESBASE end- case when sum(F_TRAX_YYSEPTEMBERSALES) is null then 0 else sum(F_TRAX_YYSEPTEMBERSALES) end jiu,   
                         case when b.F_TRAX_OCTOBERSALESBASE is null then 0 else b.F_TRAX_OCTOBERSALESBASE end- case when sum(F_TRAX_YYOCTOBERSALES) is null then 0 else sum(F_TRAX_YYOCTOBERSALES) end shi,
                         case when b.F_TRAX_NOVEMBERSALESBASE is null then 0 else b.F_TRAX_NOVEMBERSALESBASE end- case when sum(F_TRAX_YYNOVEMBERSALES) is null then 0 else sum(F_TRAX_YYNOVEMBERSALES) end shiyi, 
                         case when b.F_TRAX_DECEMBERSALESBASE is null then 0 else b.F_TRAX_DECEMBERSALESBASE end- case when sum(F_TRAX_YYDECEMBERSALES) is null then 0 else sum(F_TRAX_YYDECEMBERSALES) end shier 
                        from T_SON_KVcontract a
                         inner join T_SON_KVcontractEntry b on a.FID=b.FID
                         LEFT join T_SON_SYcontractEntry c on b.FENTRYID=c.FENTRYID
                         WHERE F_TRAX_MATERIALID='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C' and F_TRAX_SFKL=1
                         and F_TRAX_STARTDATE<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and F_TRAX_ENDDATE>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss')
                         GROUP BY
                         b.FENTRYID,
                         b.F_TRAX_MATERIALID,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_FQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         F_TRAX_JANUARYSALESBASE,
                         F_TRAX_FEBRUARYSALESBASE,
                         F_TRAX_MARCHSALESBASE,
                         F_TRAX_APRILSALESBASE,
                         F_TRAX_MAYSALESBASE,
                         F_TRAX_JUNESALESBASE,
                         F_TRAX_JULYSALESBASE,
                         F_TRAX_AUGUSTSALESBASE,
                         F_TRAX_SEPTEMBERSALESBASE,
                         F_TRAX_OCTOBERSALESBASE,
                         F_TRAX_NOVEMBERSALESBASE,
                         F_TRAX_DECEMBERSALESBASE";
                            var klhtdate = DBUtils.ExecuteDynamicObject(Context, klsql);
                            if (klhtdate.Count > 0)
                            {
                                if (klhtdate[0]["F_TRAX_FQKL"].ToString() == "0")
                                {
                                    if (Convert.ToDouble(date["BaseUnitQty"].ToString()) > Convert.ToDouble(klhtdate[0]["zs"].ToString()))
                                    {
                                        throw new KDBusinessException("", "该物料在扣率合同超过销售指标数量");
                                    }
                                }
                                else if (klhtdate[0]["F_TRAX_FQKL"].ToString() == "1")
                                {
                                    int mon = Convert.ToInt32(DateTime.Now.Month.ToString()) - 1;//获取当前时间月份
                                    string[] yf = {
                                     klhtdate[0]["yi"].ToString() ,
                                     klhtdate[0]["er"].ToString(),
                                     klhtdate[0]["san"].ToString(),
                                     klhtdate[0]["si"].ToString(),
                                     klhtdate[0]["wu"].ToString(),
                                     klhtdate[0]["liu"].ToString(),
                                     klhtdate[0]["qi"].ToString(),
                                     klhtdate[0]["ba"].ToString(),
                                     klhtdate[0]["jiu"].ToString(),
                                     klhtdate[0]["shi"].ToString(),
                                     klhtdate[0]["shiyi"].ToString(),
                                     klhtdate[0]["shier"].ToString() };
                                    if (Convert.ToDouble(date["BaseUnitQty"].ToString()) > Convert.ToDouble(yf[mon]))
                                    {
                                        throw new KDBusinessException("", "该物料超过扣率合同" + (mon + 1) + "月销售指标数量");
                                    }
                                }
                            }
                        }
                        else
                        {
                            //买点合同校验
                            string mdjysql = $@"/*dialect*/
                    select * from T_SON_MDHT a
                    inner join T_SON_MDHTentity b on a.FID=b.FID
                     WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and F_TRAX_SFKL=1 and a.FDOCUMENTSTATUS='C'
                         and F_TRAX_SXRQM<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and  F_TRAX_SXRQQM>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss' )";
                            var mdjydate = DBUtils.ExecuteDynamicObject(Context, mdjysql);
                            if (mdjydate.Count > 0)
                            {
                                if (this.Model.GetValue("FBILLNO")!= null)
                                {
                                    //校验是否已经插入
                                    string jymdsql = $@"SELECT* from T_SON_SYXXbEntity where FENTRYID = '{mdjydate[0]["FENTRYID"].ToString()}' 
                                    and F_TRAX_DDHH = '{date["Seq"].ToString()}' and F_TRAX_DDBH = '{this.Model.GetValue("FBILLNO").ToString()}'";
                                    var jymd = DBUtils.ExecuteDynamicObject(Context, jymdsql);
                                    if (jymd.Count > 0)
                                    {
                                        string scmdsql = $@"
                                       delete T_SON_SYXXbEntity where FDETAILID='{jymd[0]["FDETAILID"]}'";
                                        DBUtils.Execute(Context, scmdsql);
                                    }
                                }
                                //买点合同
                                string mdsql = $@"/*dialect*/
                          select 
                          b.FENTRYID,
                         b.F_TRAX_WLBM,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_SFFQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         count(F_TRAX_YYZXLZB) as hs,
                         case when b.F_TRAX_ZXLZBBASE is null then 0 else b.F_TRAX_ZXLZBBASE end -case when sum(F_TRAX_YYZXLZB) is null then 0 else sum(F_TRAX_YYZXLZB) end zs,
                         case when b.F_TRAX_JANUARYSALESBASE is null then 0 else b.F_TRAX_JANUARYSALESBASE end -case when sum(F_TRAX_YYJANUARYSALES) is null then 0 else sum(F_TRAX_YYJANUARYSALES) end yi,              
                         case when b.F_TRAX_FEBRUARYSALESBASE is null then 0 else b.F_TRAX_FEBRUARYSALESBASE end- case when sum(F_TRAX_YYFEBRUARYSALES) is null then 0 else sum(F_TRAX_YYFEBRUARYSALES) end er,   
                         case when b.F_TRAX_MARCHSALESBASE is null then 0 else b.F_TRAX_MARCHSALESBASE end- case when sum(F_TRAX_YYMARCHSALES) is null then 0 else sum(F_TRAX_YYMARCHSALES) end san,  
                         case when b.F_TRAX_APRILSALESBASE is null then 0 else b.F_TRAX_APRILSALESBASE end- case when sum(F_TRAX_YYAPRILSALES) is null then 0 else sum(F_TRAX_YYAPRILSALES) end si,      
                         case when b.F_TRAX_MAYSALESBASE is null then 0 else b.F_TRAX_MAYSALESBASE end- case when sum(F_TRAX_YYMAYSALES) is null then 0 else sum(F_TRAX_YYMAYSALES) end wu,     
                         case when b.F_TRAX_JUNESALESBASE is null then 0 else b.F_TRAX_JUNESALESBASE end- case when sum(F_TRAX_YYJUNESALES) is null then 0 else sum(F_TRAX_YYJUNESALES) end liu,
                         case when b.F_TRAX_JULYSALESBASE is null then 0 else b.F_TRAX_JULYSALESBASE end- case when sum(F_TRAX_YYJULYSALES) is null then 0 else sum(F_TRAX_YYJULYSALES) end qi,
                         case when b.F_TRAX_AUGUSTSALESBASE is null then 0 else b.F_TRAX_AUGUSTSALESBASE end- case when sum(F_TRAX_YYAUGUSTSALES) is null then 0 else sum(F_TRAX_YYAUGUSTSALES) end ba,
                         case when b.F_TRAX_SEPTEMBERSALESBASE is null then 0 else b.F_TRAX_SEPTEMBERSALESBASE end- case when sum(F_TRAX_YYSEPTEMBERSALES) is null then 0 else sum(F_TRAX_YYSEPTEMBERSALES) end jiu,   
                         case when b.F_TRAX_OCTOBERSALESBASE is null then 0 else b.F_TRAX_OCTOBERSALESBASE end- case when sum(F_TRAX_YYOCTOBERSALES) is null then 0 else sum(F_TRAX_YYOCTOBERSALES) end shi,
                         case when b.F_TRAX_NOVEMBERSALESBASE is null then 0 else b.F_TRAX_NOVEMBERSALESBASE end- case when sum(F_TRAX_YYNOVEMBERSALES) is null then 0 else sum(F_TRAX_YYNOVEMBERSALES) end shiyi, 
                         case when b.F_TRAX_DECEMBERSALESBASE is null then 0 else b.F_TRAX_DECEMBERSALESBASE end- case when sum(F_TRAX_YYDECEMBERSALES) is null then 0 else sum(F_TRAX_YYDECEMBERSALES) end shier
                         from T_SON_MDHT a
                         inner join T_SON_MDHTentity b on a.FID=b.FID
                         left JOIN T_SON_SYXXbEntity c on b.FENTRYID=c.FENTRYID
                         WHERE F_TRAX_WLBM='{wl["Id"]}' AND F_TRAX_CUSTOMER='{kh["Id"]}' and a.FDOCUMENTSTATUS='C' and F_TRAX_SFKL=1
                         and F_TRAX_SXRQM<=to_date('{rq}','yyyy-mm-dd hh24:mi:ss') 
                         and  F_TRAX_SXRQQM>=to_date('{rq}','yyyy-mm-dd hh24:mi:ss')
                         GROUP BY
                         b.FENTRYID,
                         b.F_TRAX_WLBM,
                         b.F_TRAX_ZXLZBBASE,
                         a.F_TRAX_SFFQKL,
                         F_TRAX_DDBH,
                         F_TRAX_DDHH,
                         F_TRAX_JANUARYSALESBASE,
                         F_TRAX_FEBRUARYSALESBASE,
                         F_TRAX_MARCHSALESBASE,
                         F_TRAX_APRILSALESBASE,
                         F_TRAX_MAYSALESBASE,
                         F_TRAX_JUNESALESBASE,
                         F_TRAX_JULYSALESBASE,
                         F_TRAX_AUGUSTSALESBASE,
                         F_TRAX_SEPTEMBERSALESBASE,
                         F_TRAX_OCTOBERSALESBASE,
                         F_TRAX_NOVEMBERSALESBASE,
                         F_TRAX_DECEMBERSALESBASE";
                                var mdhtdate = DBUtils.ExecuteDynamicObject(Context, mdsql);
                                if (mdhtdate.Count > 0)
                                {
                                    if (mdhtdate[0]["F_TRAX_SFFQKL"].ToString() == "0")
                                    {
                                        if (Convert.ToDouble(date["BaseUnitQty"].ToString()) > Convert.ToDouble(mdhtdate[0]["zs"].ToString()))
                                        {
                                            throw new KDBusinessException("", "该物料在买点合同超过销售指标数量");
                                        }
                                    }
                                    else if (mdhtdate[0]["F_TRAX_SFFQKL"].ToString() == "1")
                                    {
                                        int mon = Convert.ToInt32(DateTime.Now.Month.ToString()) - 1;//获取当前时间月份
                                        string[] yf = {
                                     mdhtdate[0]["yi"].ToString() ,
                                     mdhtdate[0]["er"].ToString(),
                                     mdhtdate[0]["san"].ToString(),
                                     mdhtdate[0]["si"].ToString(),
                                     mdhtdate[0]["wu"].ToString(),
                                     mdhtdate[0]["liu"].ToString(),
                                     mdhtdate[0]["qi"].ToString(),
                                     mdhtdate[0]["ba"].ToString(),
                                     mdhtdate[0]["jiu"].ToString(),
                                     mdhtdate[0]["shi"].ToString(),
                                     mdhtdate[0]["shiyi"].ToString(),
                                     mdhtdate[0]["shier"].ToString() };

                                        if (Convert.ToDouble(date["BaseUnitQty"].ToString()) > Convert.ToDouble(yf[mon]))
                                        {
                                            throw new KDBusinessException("", "该物料超过" + (mon + 1) + "月销售指标数量");
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }

    }
}
