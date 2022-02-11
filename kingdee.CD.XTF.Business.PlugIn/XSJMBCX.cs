using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using YYLK;

namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("销售价目表查询"), HotUpdate]
    public class XSJMBCX : SysReportBaseService
    {
        String tempTableName;
        //初始化
        public override void Initialize()
        {
            base.Initialize();
            // 简单账表类型：普通、树形、分页
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.IsCreateTempTableByPlugin = true;
            //是否分组汇总
            this.ReportProperty.IsGroupSummary = true;
        }
        /**
        * 获取过滤条件信息(构造单据信息)
        * */
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles reportTitles = new ReportTitles();
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            //系列过滤
            if (customFilter["F_TRAX_XL"] != null)
            {
                DynamicObjectCollection B = customFilter["F_TRAX_XL"] as DynamicObjectCollection;//系列
                var XLMC = "";
                XLMC += "" + (customFilter["F_TRAX_XL"] as DynamicObject)["Name"].ToString() + ",";
                reportTitles.AddTitle("F_TRAX_XL", XLMC.Trim(','));
            }
            else
            {
                reportTitles.AddTitle("F_TRAX_XL", "");
            }

            //品牌过滤
            if (customFilter["F_TRAX_PP"] != null)
            {
                DynamicObjectCollection A = customFilter["F_TRAX_PP"] as DynamicObjectCollection;//品牌
                var PPMC = "";
                foreach (var item in A)
                {
                    PPMC += "" + (item["F_TRAX_PP"] as DynamicObject)["Name"].ToString() + ",";
                }
                reportTitles.AddTitle("F_TRAX_PP", PPMC.Trim(','));
            }
            else
            {
                reportTitles.AddTitle("F_TRAX_PP", "");
            }

            //价格类型过滤
            if (customFilter["F_TRAX_JGLX"] != null)
            {
                DynamicObjectCollection a = customFilter["F_TRAX_JGLX"] as DynamicObjectCollection;
                var JGLXMC = "";
                foreach (var item in a)
                {
                    JGLXMC += "" + (item["F_TRAX_JGLX"] as DynamicObject)["FDataValue"].ToString() + ",";
                }
                reportTitles.AddTitle("F_TRAX_JGLX", JGLXMC.Trim(','));
            }
            else
            {
                reportTitles.AddTitle("F_TRAX_JGLX", "");
            }
            return reportTitles;
        }
        /**
       * 设置单据列
       **/
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader reportHeader = new ReportHeader();
            //设置列
            reportHeader.AddChild("WLBM", new LocaleValue("物料&物料编码", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("WLMC", new LocaleValue("物料&物料名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("PPMC", new LocaleValue("物料&品牌", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("XLMC", new LocaleValue("物料&系列", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("JBDW", new LocaleValue("物料&单位", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            if (customFilter["F_TRAX_JGLX"] != null)
            {
                DynamicObjectCollection a = customFilter["F_TRAX_JGLX"] as DynamicObjectCollection;
                foreach (var item in a)
                {
                    string FDATAVALUE = item.GetVal<DynamicObject>("F_TRAX_JGLX").GetVal<object>("FDataValue").ToString();
                    if (FDATAVALUE == "特价") continue;
                    reportHeader.AddChild(FDATAVALUE, new LocaleValue(FDATAVALUE, this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                }
            }

            return reportHeader;
        }
        /**
 * 构造取数sql
 * */
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;

            string str="";
            if (customFilter["F_TRAX_JGLX"] != null)
            {
                DynamicObjectCollection a = customFilter["F_TRAX_JGLX"] as DynamicObjectCollection;
                foreach (var item in a)
                {
                    string FDATAVALUE = item.GetVal<DynamicObject>("F_TRAX_JGLX").GetVal<object>("FDataValue").ToString();
                    str += "sum(" + FDATAVALUE + ")" + ',';
                }
            }

            //物料
            var WLBM = "";
            if (customFilter["F_TRAX_WL"] != null)
            {
                WLBM = (customFilter["F_TRAX_WL"] as DynamicObject)["Number"].ToString();
            }

            //系列 
            var XLBM = "";
            if (customFilter["F_TRAX_XL"] != null)
            {
                XLBM += "'" + (customFilter["F_TRAX_XL"] as DynamicObject)["Number"].ToString() + "'" + ",";
            }

            //品牌
            DynamicObjectCollection A = customFilter["F_TRAX_PP"] as DynamicObjectCollection;
            var PPBM = "";
            foreach (var item in A)
            {
                PPBM += "'" + (item["F_TRAX_PP"] as DynamicObject)["Number"].ToString() + "'" + ",";
            }

            //价格类型
            List<string> strList = new List<string>();
            var JGLXBM = "";
            //string JGLXfliter = "";
            if (customFilter["F_TRAX_JGLX"] != null)
            {
                DynamicObjectCollection a = customFilter["F_TRAX_JGLX"] as DynamicObjectCollection;
                foreach (var item in a)
                {
                    JGLXBM += "'" + (item["F_TRAX_JGLX"] as DynamicObject)["FNumber"].ToString() + "'" + ",";
                    string FDATAVALUE = item.GetVal<DynamicObject>("F_TRAX_JGLX").GetVal<object>("FDataValue").ToString();
                    if (FDATAVALUE == "特价") continue;
                    strList.Add($@"'{FDATAVALUE}' {FDATAVALUE}");
                }
            }

            //物料
            string wlfliter = "";
            if (WLBM != "")
            {
                wlfliter = "WLBM.FNUMBER='" + WLBM + "'";
            }
            else
                wlfliter = "WLBM.FNUMBER LIKE '%%'";

            //系列
            string xlfliter = "";
            if (XLBM != "")
            {
                xlfliter = "XL.FNUMBER in (" + XLBM.Trim(',') + ")";
            }
            else
                xlfliter = "XL.FNUMBER LIKE '%%'";

            //品牌
            string ppfliter = "";
            if (PPBM != "")
            {
                ppfliter = " PP.FNUMBER in (" + PPBM.Trim(',') + ")";
            }
            else
                ppfliter = " PP.FNUMBER LIKE '%%'";

            //价格类型
            string JGLXfliter = "";
            if (JGLXBM != "")
            {
                JGLXfliter = " FZZL.FNUMBER in (" + JGLXBM.Trim(',') + ")";
            }
            else
                JGLXfliter = " FZZL.FNUMBER LIKE '%%'";

            string sql = $@"/*dialect*/Create table {tableName} as 
select 
RowNum as FIDENTITYID * from
(
SELECT 
WLBM.FNUMBER AS WLBM,
WLMC.FNAME AS WLMC,
ppmc.FNAME AS PPMC,
XLMC.FNAME AS XLMC,
FZZLMC.FDATAVALUE AS JGLX,
DWMC.FNAME AS JBDW,
CASE WHEN DW.FNUMBER = 'JIAN' THEN (XSJMBMX.FPRICE/WLHS.FCONVERTNUMERATOR/WLHS.FCONVERTDENOMINATOR) ELSE XSJMBMX.FPRICE END AS XSJG
FROM  T_SAL_PRICELISTENTRY XSJMBMX
INNER JOIN T_BD_UNIT DW ON XSJMBMX.FUNITID=DW.FUNITID
INNER JOIN T_BD_UNIT_L DWMC ON DWMC.FUNITID=DW.FUNITID
INNER JOIN T_SAL_PRICELIST XSJMBT ON XSJMBMX.FID=XSJMBT.FID
INNER JOIN T_BD_MATERIAL  WLBM ON XSJMBMX.FMATERIALID=WLBM.FMATERIALID
INNER JOIN T_BD_MATERIAL_L WLMC ON WLBM.FMATERIALID=WLMC.FMATERIALID
INNER JOIN T_BD_Brand PP ON WLBM.F_TRAX_BRAND=PP.FID
INNER JOIN T_BD_BRAND_L PPMC ON PPMC.FID=PP.FID
INNER JOIN T_BD_SERIES XL ON WLBM.F_TRAX_SERIES=XL.FID
INNER JOIN T_BD_SERIES_L XLMC ON XL.FID=XLMC.FID
LEFT JOIN T_BD_UNITCONVERTRATE WLHS ON XSJMBMX.FMATERIALID=WLHS.FMATERIALID  
and WLHS.FDOCUMENTSTATUS='C'
LEFT JOIN T_BAS_ASSISTANTDATAENTRY FZZL ON XSJMBT.FPRICETYPE=FZZL.FENTRYID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L FZZLMC ON FZZL.FENTRYID=FZZLMC.FENTRYID
INNER JOIN T_BAS_ASSISTANTDATA FZZLLLB ON FZZLLLB.FID=FZZL.FID
AND FZZL.FNUMBER!='02'
AND XSJMBMX.FEFFECTIVEDATE<=to_date(SYSDATE) 
AND XSJMBMX.FEXPRIYDATE>=to_date(SYSDATE)
AND XSJMBT.FAUDITSTATUS='A'
AND {wlfliter}
AND {xlfliter}
AND {ppfliter}
AND {JGLXfliter}
) temp
pivot (sum(xsjg) for jglx in ({string.Join(",", strList)}))
";

            DBUtils.Execute(this.Context, sql);
            tempTableName = tableName;
        }
        /**
       * 删除临时表
       * */
        public override void CloseReport()
        {
            base.CloseReport();
            Boolean flag = DBUtils.IsExistTable(this.Context, tempTableName);
            String[] tempName = { tempTableName };
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<Kingdee.BOS.Contracts.IDBService>();
            if (flag)
            {
                dbService.DeleteTemporaryTableName(this.Context, tempName);
            }
        }
    }
}
