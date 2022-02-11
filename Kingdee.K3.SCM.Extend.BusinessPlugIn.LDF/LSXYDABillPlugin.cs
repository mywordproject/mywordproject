using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Model.ReportFilter;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.LDF
{
    [Description("临时信用档案")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LSXYDABillPlugin: AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z")
            {
                string zz = (DynamicObject)this.Model.GetValue("FORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FORGID"))["Id"].ToString();
                var dates = (DynamicObjectCollection)this.Model.DataObject["Entity"];
                if (dates.Count > 0)
                {
                    foreach (var date in dates)
                    {
                        if ((DynamicObject)date["ObjectId"] != null)
                        {
                            K3CloudApiClient client = new K3CloudApiClient("http://192.168.5.5/k3cloud/");
                            var loginResult = client.ValidateLogin("61e8b37dd131dc", "李德飞", "xtf@1111", 2052);//61874464c91d50  619499281b179f
                            var resultType = JObject.Parse(loginResult)["LoginResultType"].Value<int>();
                            //登录结果类型等于1，代表登录成功
                            if (resultType == 1)
                            {
                                JObject Job = new JObject();
                                Job.Add("FieldKeys", "FCREDITAMOUNT ,FTMPCREDITAMOUNT ,FUSECREDITAMOUNT ,FLIMITBALANCE ");
                                Job.Add("SchemeId", "");
                                Job.Add("StartRow", 0);
                                Job.Add("Limit", 500);
                                JObject model = new JObject();
                                model.Add("FSaleOrgList", zz);// 授信组织
                                model.Add("FObjectType", "BD_Customer");// 控制对象类型
                                JObject FStartCustId = new JObject();
                                FStartCustId.Add("FNumber", ((DynamicObject)date["ObjectId"])["Number"].ToString());//信用对象(客户)
                                model.Add("FStartCustId", FStartCustId);
                                JObject FEndCustId = new JObject();
                                FEndCustId.Add("FNumber", ((DynamicObject)date["ObjectId"])["Number"].ToString());//至(客户)
                                model.Add("FEndCustId", FEndCustId);
                                model.Add("FCreditStatus", "ALL");// 信用状态
                                model.Add("FIsExceeds", "-1");// 信用超标
                                model.Add("FMultiSelect", false);// 多选
                                Job.Add("Model", model);
                                var message = client.GetSysReportData("CRE_Rpt_CreditInfo", Job.ToString());
                                //返回的字符串进行序列化，判断是否保存成功
                                string strjson = message.Trim(new char[] { '"' });
                                JObject jsonobj = (JObject)JsonConvert.DeserializeObject(strjson);
                                string Status = jsonobj["Result"]["IsSuccess"].ToString();
                               string[] a=  Regex.Replace(jsonobj["Result"]["Rows"].ToString(), @"\s", "").Trim('[').Trim(']').Split('"') ;
                               
                                if (Status== "True")
                                {
                                    if (Convert.ToInt32(jsonobj["Result"]["RowCount"].ToString()) == 1)
                                    {                                      
                                        //double jgs = Double.Parse(a[1])+ Double.Parse(a[3]==""?"0":a[3]);
                                        this.Model.SetValue("F_TRAX_CURRENTAMOUNT", a[1] == "" ? "0" : a[1], Convert.ToInt32(date["Seq"].ToString()) - 1);//当前额度
                                        this.Model.SetValue("F_TRAX_LSED", a[3] == "" ? "0" : a[3], Convert.ToInt32(date["Seq"].ToString()) - 1);//临时额度
                                       this.Model.SetValue("F_TRAX_AMOUNTUSED", a[5] == "" ? "0" : a[5], Convert.ToInt32(date["Seq"].ToString()) - 1);//已使用额度
                                       this.Model.SetValue("F_TRAX_REMAININGAMOUNT", a[7] == "" ? "0" : a[7], Convert.ToInt32(date["Seq"].ToString()) - 1);//剩余额度
                                    }
                                    else if(Convert.ToInt32(jsonobj["Result"]["RowCount"].ToString())> 1)
                                    {
                                      //double A = 0;
                                      //double B = 0;
                                      //double C = 0;
                                      //string[] jgs = { Rows.Trim(']').Trim('[') };
                                      //foreach (var jg in jgs)
                                      //{
                                      // string[]  i= { jg.Trim('[').Trim(']') };
                                      //    A +=Convert.ToDouble(i[0]) + Convert.ToDouble(i[1]);
                                      //    B += Convert.ToDouble(i[2]);
                                      //    C += Convert.ToDouble(i[3]);
                                      //}
                                      //this.Model.SetValue("F_TRAX_CURRENTAMOUNT", A, Convert.ToInt32(date["Seq"].ToString()) - 1);//当前额度
                                      //this.Model.SetValue("F_TRAX_AMOUNTUSED", B, Convert.ToInt32(date["Seq"].ToString()) - 1);//已使用额度
                                      //this.Model.SetValue("F_TRAX_REMAININGAMOUNT", C, Convert.ToInt32(date["Seq"].ToString()) - 1);//剩余额度
                                    }
                                   
                                  
                                }
                                
                            }
                        }
                    }
                }
            }
            //if (this.Model.GetValue("FDOCUMENTSTATUS").ToString() == "Z")
            //{
            //    string zz = (DynamicObject)this.Model.GetValue("FORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FORGID"))["Id"].ToString();
            //    var dates = (DynamicObjectCollection)this.Model.DataObject["Entity"];
            //    if (dates.Count > 0)
            //    {
            //        foreach (var date in dates)
            //        {
            //            if ((DynamicObject)date["ObjectId"] != null)
            //            {
            //                ISysReportService sysReporSservice = ServiceFactory.GetSysReportService(this.Context);
            //                IPermissionService permissionService = ServiceFactory.GetPermissionService(this.Context);
            //                var filterMetadata = FormMetaDataCache.GetCachedFilterMetaData(this.Context);//加载字段比较条件元数据。
            //                var reportMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, "CRE_Rpt_CreditInfo");//加载应收款账龄分析表元数据。
            //                var reportFilterMetadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, "CRE_Rpt_CreditInfoFilter");//加载应收款账龄分析表过滤条件元数据。
            //                var reportFilterServiceProvider = reportFilterMetadata.BusinessInfo.GetForm().GetFormServiceProvider();
            //                var model = new SysReportFilterModel();
            //                model.SetContext(this.Context, reportFilterMetadata.BusinessInfo, reportFilterServiceProvider);
            //                model.FormId = reportFilterMetadata.BusinessInfo.GetForm().Id;
            //                model.FilterObject.FilterMetaData = filterMetadata;
            //                model.InitFieldList(reportMetadata, reportFilterMetadata);
            //                model.GetSchemeList();
            //                var fananid = DBServiceHelper.ExecuteDataSet(Context, "SELECT FSCHEMEID FROM T_BAS_FILTERSCHEME where FFORMID='CRE_Rpt_CreditInfo' and  FSCHEMENAME='临时信用档案查询'  ").Tables[0].Rows[0]["FSCHEMEID"].ToString();//查询到模板方案标识
            //                var entity = model.Load(fananid); //服务器专用
            //                                                  //var entity = model.Load("6093aaf7d311ec");//过滤方案的主键值，可通过该SQL语句查询得到：SELECT * FROM T_BAS_FILTERSCHEME  and FUSERID=" + Context.UserId + " 
            //                var filters = model.GetFilterParameter();
            //                //填充过滤条件
            //                //filters.CustomFilter["FStartCustId_Id"] = date["ObjectId_Id"];//账簿
            //
            //                IRptParams p = new RptParams();
            //                p.FormId = reportFilterMetadata.BusinessInfo.GetForm().Id;
            //                p.StartRow = 1;
            //                p.EndRow = int.MaxValue;//StartRow和EndRow是报表数据分页的起始行数和截至行数，一般取所有数据，所以EndRow取int最大值。
            //                p.FilterParameter = filters;
            //                p.FilterFieldInfo = model.FilterFieldInfo;
            //                p.BaseDataTempTable.AddRange(permissionService.GetBaseDataTempTable(this.Context, reportMetadata.BusinessInfo.GetForm().Id));
            //                string dtname = sysReporSservice.GetDataTableName(Context, reportMetadata.BusinessInfo, p);
            //
            //                //datatable td = DBServiceHelper(Context, $@"select * from dtname")
            //                string edsumsql = $@"select sum(FCREDITAMOUNT+FTMPCREDITAMOUNT) A,SUM(FUSECREDITAMOUNT) B,SUM(FLIMITBALANCE) C  from {dtname} 
            //                where FCREDITOBJECTCODE='{((DynamicObject)date["ObjectId"])["Number"].ToString()}' AND FORGID='{zz}'";
            //                var edsum = DBUtils.ExecuteDynamicObject(Context, edsumsql);
            //                this.Model.SetValue("F_TRAX_CURRENTAMOUNT", edsum[0]["A"], Convert.ToInt32(date["Seq"].ToString()) - 1);//当前额度
            //                this.Model.SetValue("F_TRAX_AMOUNTUSED", edsum[0]["B"], Convert.ToInt32(date["Seq"].ToString()) - 1);//已使用额度
            //                this.Model.SetValue("F_TRAX_REMAININGAMOUNT", edsum[0]["C"], Convert.ToInt32(date["Seq"].ToString()) - 1);//剩余额度
            //            }
            //        }
            //    }
            //}
        }
    }
}

