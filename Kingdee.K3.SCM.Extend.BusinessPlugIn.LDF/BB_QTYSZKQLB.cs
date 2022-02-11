using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Contracts;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Model.ReportFilter;
using Kingdee.BOS.Orm.DataEntity;

namespace Kingdee.CD.STGYL.Report
{
    [Description("其他应收账款清理表"), BOS.Util.HotUpdate]
    public class BB_QTYSZKQLB : SysReportBaseService
    {
        //临时表名
        string tempTableName = "";
        public override void Initialize()
        {
            base.Initialize();
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.IsUIDesignerColumns = false;
            this.ReportProperty.PrimaryKeyFieldName = "FIDENTITYID";
            this.ReportProperty.DetailReportType = ReportType.REPORTTYPE_NORMAL;
            this.ReportProperty.DetailReportId = "ora_QTYSZKQLB";
        }

        //构建账表数据
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //获取到过滤界面上的参数
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            string nian = "", yue = "";
            if (customFilter["DeadLine"] != null)
            {
                nian = customFilter["DeadLine"].ToString();
                yue = Convert.ToDateTime(customFilter["DeadLine"]).Month.ToString();
            }

            if (customFilter["Balance"].ToString().Contains("1221") || customFilter["Balance"].ToString().Contains("2241"))
            {
                //加载总账账龄分析表数据
                ISysReportService sysReporSservice = ServiceFactory.GetSysReportService(this.Context);
                IPermissionService permissionService = ServiceFactory.GetPermissionService(this.Context);
                var filterMetadata = FormMetaDataCache.GetCachedFilterMetaData(this.Context);//加载字段比较条件元数据。
                var reportMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, "GL_AgingSchedule");//加载应收款账龄分析表元数据。
                var reportFilterMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, "GL_AgingScheduleFilter");//加载应收款账龄分析表过滤条件元数据。
                var reportFilterServiceProvider = reportFilterMetadata.BusinessInfo.GetForm().GetFormServiceProvider();
                var model = new SysReportFilterModel();
                model.SetContext(this.Context, reportFilterMetadata.BusinessInfo, reportFilterServiceProvider);
                model.FormId = reportFilterMetadata.BusinessInfo.GetForm().Id;
                model.FilterObject.FilterMetaData = filterMetadata;
                model.InitFieldList(reportMetadata, reportFilterMetadata);
                model.GetSchemeList();
                var fananid = DBServiceHelper.ExecuteDataSet(Context, "SELECT FSCHEMEID FROM T_BAS_FILTERSCHEME where FFORMID='GL_AgingSchedule' and  FSCHEMENAME='其他应收模板' and FUSERID=" + Context.UserId + " ").Tables[0].Rows[0]["FSCHEMEID"].ToString();//查询到模板方案标识
                var entity = model.Load(fananid); //服务器专用
                                                  //var entity = model.Load("6093aaf7d311ec");//过滤方案的主键值，可通过该SQL语句查询得到：SELECT * FROM T_BAS_FILTERSCHEME
                var filters = model.GetFilterParameter();
                //填充过滤条件
                filters.CustomFilter["Book_Id"] = customFilter["Book_Id"];//账簿
                filters.CustomFilter["DeadLine"] = customFilter["DeadLine"];//截止日期
                filters.CustomFilter["Balance"] = customFilter["Balance"];//科目编码           
                IRptParams p = new RptParams();
                p.FormId = reportFilterMetadata.BusinessInfo.GetForm().Id;
                p.StartRow = 1;
                p.EndRow = int.MaxValue;//StartRow和EndRow是报表数据分页的起始行数和截至行数，一般取所有数据，所以EndRow取int最大值。
                p.FilterParameter = filters;
                p.FilterFieldInfo = model.FilterFieldInfo;
                p.BaseDataTempTable.AddRange(permissionService.GetBaseDataTempTable(this.Context, reportMetadata.BusinessInfo.GetForm().Id));
                string dtname = sysReporSservice.GetDataTableName(Context, reportMetadata.BusinessInfo, p);


                //加载总账账龄分析表数据
                ISysReportService sysReporSservice1 = ServiceFactory.GetSysReportService(this.Context);
                IPermissionService permissionService1 = ServiceFactory.GetPermissionService(this.Context);
                var filterMetadata1 = FormMetaDataCache.GetCachedFilterMetaData(this.Context);//加载字段比较条件元数据。
                var reportMetadata1 = FormMetaDataCache.GetCachedFormMetaData(this.Context, "GL_AgingSchedule");//加载应收款账龄分析表元数据。
                var reportFilterMetadata1 = FormMetaDataCache.GetCachedFormMetaData(this.Context, "GL_AgingScheduleFilter");//加载应收款账龄分析表过滤条件元数据。
                var reportFilterServiceProvider1 = reportFilterMetadata1.BusinessInfo.GetForm().GetFormServiceProvider();
                var model1 = new SysReportFilterModel();
                model1.SetContext(this.Context, reportFilterMetadata1.BusinessInfo, reportFilterServiceProvider1);
                model1.FormId = reportFilterMetadata1.BusinessInfo.GetForm().Id;
                model1.FilterObject.FilterMetaData = filterMetadata1;
                model1.InitFieldList(reportMetadata1, reportFilterMetadata1);
                model1.GetSchemeList();
                var fananid1 = DBServiceHelper.ExecuteDataSet(Context, "SELECT FSCHEMEID FROM T_BAS_FILTERSCHEME where FFORMID='GL_AgingSchedule' and  FSCHEMENAME='其他应付模板' and FUSERID=" + Context.UserId + " ").Tables[0].Rows[0]["FSCHEMEID"].ToString();//查询到模板方案标识
                var entity1 = model1.Load(fananid1); //服务器专用
                                                     //var entity = model.Load("6093aaf7d311ec");//过滤方案的主键值，可通过该SQL语句查询得到：SELECT * FROM T_BAS_FILTERSCHEME
                var filters1 = model1.GetFilterParameter();
                //填充过滤条件
                filters1.CustomFilter["Book_Id"] = customFilter["Book_Id"];//账簿
                filters1.CustomFilter["DeadLine"] = customFilter["DeadLine"];//截止日期
                filters1.CustomFilter["Balance"] = customFilter["Balance"];//科目编码           
                IRptParams p1 = new RptParams();
                p1.FormId = reportFilterMetadata1.BusinessInfo.GetForm().Id;
                p1.StartRow = 1;
                p1.EndRow = int.MaxValue;//StartRow和EndRow是报表数据分页的起始行数和截至行数，一般取所有数据，所以EndRow取int最大值。
                p1.FilterParameter = filters1;
                p1.FilterFieldInfo = model1.FilterFieldInfo;
                p1.BaseDataTempTable.AddRange(permissionService.GetBaseDataTempTable(this.Context, reportMetadata1.BusinessInfo.GetForm().Id));
                string dtname1 = sysReporSservice.GetDataTableName(Context, reportMetadata1.BusinessInfo, p1);


                //开始构建其他应收账款清理表
                string sql = "";
                base.BuilderReportSqlAndTempTable(filter, tableName);

                sql = string.Format(@"/*dialect*/select IDENTITY(int,1,1) as FIDENTITYID, ALLIN.* ,'' JBR,'' BMFZR, '' BZ  into {0}  from (
                                select fflex6,FACCTNUMBER,FACCTNAME, CONVERT(varchar(7) ,fdate, 120) fdate,FVOUCHERGROUPID,CONVERT(varchar(7) ,fbusdate, 120) fbusdate,fdcname,
                                CONVERT(decimal(20,2),sum(FRESERVEDFOR)) FRESERVEDFOR, CONVERT(decimal(20,2),sum(F3month)) F3month,  CONVERT(decimal(20,2),sum(F3to6month)) F3to6month,CONVERT(decimal(20,2),sum(F6to12month))  F6to12month, CONVERT(decimal(20,2),sum(F12to24month)) F12to24month,CONVERT(decimal(20,2),sum(F24month))  F24month
                                from (
                                select FDATATYPE,FACCOUNTID,FACCTNUMBER ,FACCTNAME,fflex6, fdate,FVOUCHERGROUPID, fbusdate,
                                FCURRENCYID,FCURRENCYNAME,FRESERVEDFOR , FRESERVED,
                                fdcname,FAMOUNTFORDIGITS,FAMOUNTDIGITS,fformid,
                                (case when datediff(month, fbusdate,  GETDATE())<=3 then FRESERVED else 0 end) F3month,
                                (case when datediff(month, fbusdate, GETDATE()) between 3 and 6   then FRESERVED else 0 end) F3to6month,
                                (case when datediff(month, fbusdate,  GETDATE()) between 6 and 12   then FRESERVED else 0 end) F6to12month,
                                (case when datediff(month, fbusdate,  GETDATE()) between 12 and 24   then FRESERVED else 0 end) F12to24month,
                                (case when datediff(month, fbusdate,  GETDATE())>=24  then FRESERVED else 0 end) F24month
                                from {1}   where FACCTNUMBER like '1221%' and  FACCOUNTID not in(999999999)  and  fdcname='借'                              
                                ) qtys 
                                group by  fflex6,FACCTNUMBER,FACCTNAME, CONVERT(varchar(7) ,fdate, 120) ,FVOUCHERGROUPID,CONVERT(varchar(7) ,fbusdate, 120) ,fdcname
                                union all
                                select fflex4,FACCTNUMBER,FACCTNAME, CONVERT(varchar(7) ,fdate, 120) fdate,FVOUCHERGROUPID,CONVERT(varchar(7) ,fbusdate, 120) fbusdate,fdcname,
                                CONVERT(decimal(20,2),sum(FRESERVEDFOR)) FRESERVEDFOR, CONVERT(decimal(20,2),sum(F3month)) F3month,  CONVERT(decimal(20,2),sum(F3to6month)) F3to6month,CONVERT(decimal(20,2),sum(F6to12month))  F6to12month, CONVERT(decimal(20,2),sum(F12to24month)) F12to24month,CONVERT(decimal(20,2),sum(F24month))  F24month
                                from (
                                select FDATATYPE,FACCOUNTID,FACCTNUMBER ,FACCTNAME,fflex4, fdate,FVOUCHERGROUPID, fbusdate,
                                FCURRENCYID,FCURRENCYNAME, FRESERVEDFOR , FRESERVED,
                                fdcname,FAMOUNTFORDIGITS,FAMOUNTDIGITS,fformid,
                                (case when    datediff(month, fbusdate,  GETDATE())<=3 then FRESERVED else 0 end) F3month,
                                (case when datediff(month, fbusdate, GETDATE()) between 3 and 6   then FRESERVED else 0 end) F3to6month,
                                (case when datediff(month, fbusdate,  GETDATE()) between 6 and 12   then FRESERVED else 0 end) F6to12month,
                                (case when datediff(month, fbusdate,  GETDATE()) between 12 and 24   then FRESERVED else 0 end) F12to24month,
                                (case when datediff(month, fbusdate,  GETDATE())>=24  then FRESERVED else 0 end) F24month
                                from {2}   where FACCTNUMBER like '2241%' and  FACCOUNTID not in(999999999)   and  fdcname='借'
                                ) qtyf 
                                group by  fflex4,FACCTNUMBER,FACCTNAME, CONVERT(varchar(7) ,fdate, 120) ,FVOUCHERGROUPID,CONVERT(varchar(7) ,fbusdate, 120) ,fdcname
                                ) ALLIN", tableName, dtname, dtname1);
                tempTableName = tableName;
                DBUtils.ExecuteDynamicObject(Context, sql);
                ServiceFactory.CloseService(sysReporSservice);
                ServiceFactory.CloseService(permissionService);
            }
            else
            {
                throw new KDBusinessException("", "请选择【应收】或者【应付】科目！！");
            }
        }

        //构建账表列
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            header.AddRange(new List<ReportHeader>()
            {
                new ReportHeader(){FieldName="FIDENTITYID",Caption= new LocaleValue("单据ID"), Mergeable=true ,Visible =false } ,
                new ReportHeader(){FieldName="FACCTNUMBER",Caption= new LocaleValue("科目编码"),Width=220, ColIndex=1} ,
                new ReportHeader(){FieldName="FACCTNAME",Caption= new LocaleValue("科目名称"),Width=220, ColIndex=2} ,
                new ReportHeader(){FieldName="fflex6",Caption= new LocaleValue("客户"),Width=220, ColIndex=3} ,
                new ReportHeader(){FieldName="fdate",Caption= new LocaleValue("凭证日期"), ColIndex=4} ,
                new ReportHeader(){FieldName="FVOUCHERGROUPID",Caption= new LocaleValue("凭证字"), ColIndex=5} ,
                new ReportHeader(){FieldName="fbusdate",Caption= new LocaleValue("业务日期"), ColIndex=6} ,
                new ReportHeader(){FieldName="fdcname",Caption= new LocaleValue("方向"), ColIndex=7 ,Visible =true},
                new ReportHeader(){FieldName="FRESERVEDFOR",Caption= new LocaleValue("余额"), ColIndex=8},
                new ReportHeader(){FieldName="F3month",Caption= new LocaleValue("3个月以内"), ColIndex=9},
                new ReportHeader(){FieldName="F3to6month",Caption= new LocaleValue("3-6个月以内"), ColIndex=10},
                new ReportHeader(){FieldName="F6to12month",Caption= new LocaleValue("6个月-1年"), ColIndex=11},
                new ReportHeader(){FieldName="F12to24month",Caption= new LocaleValue("1-2年"), ColIndex=12},
                new ReportHeader(){FieldName="F24month",Caption= new LocaleValue("2年以上"), ColIndex=13},
                new ReportHeader(){FieldName="JBR",Caption= new LocaleValue("经办人"), ColIndex=14,Visible =true},
                new ReportHeader(){FieldName="BMFZR",Caption= new LocaleValue("部门负责人"), ColIndex=15,Visible =true},
                new ReportHeader(){FieldName="BZ",Caption= new LocaleValue("备注"), ColIndex=16,Visible =true}
            });
            return header;
        }

        //删除临时表
        public override void CloseReport()
        {
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<Kingdee.BOS.Contracts.IDBService>();
            string[] tempTableName1 = { tempTableName };
            dbService.DeleteTemporaryTableName(this.Context, tempTableName1);
            base.CloseReport();
        }
    }
}
