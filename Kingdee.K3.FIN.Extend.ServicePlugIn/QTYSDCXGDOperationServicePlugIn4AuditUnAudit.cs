using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;
using Kingdee.BOS;
using Kingdee.BOS.Apm;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.App.Core.Match;
using Kingdee.K3.FIN.App.Core.Match.Object;
using Kingdee.K3.FIN.App.Core.Match.Utils;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.Core.Match;
using Kingdee.K3.FIN.Core.Match.Object;
using Kingdee.K3.FIN.Core.Object.ARAP;
using Kingdee.K3.FIN.Core.Parameters;
using Kingdee.K3.FIN.ServiceHelper;
using MyExtend;

namespace Kingdee.K3.FIN.Extend.ServicePlugIn
{
    [Description("其他应收单-审核、反审核-自动核销")]
    [HotUpdate]
    public class QTYSDCXGDOperationServicePlugIn4AuditUnAudit : CommonOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FBILLNO");
            e.FieldKeys.Add("FID");
            e.FieldKeys.Add("FENTRYID");
            e.FieldKeys.Add("FSEQ");
            e.FieldKeys.Add("FAMOUNTFOR_D");
            e.FieldKeys.Add("FSourceBillNo");
            e.FieldKeys.Add("FSOURCETYPE");
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            //
            if (FormOperation.Operation.EqualsIgnoreCase("Audit"))
            {
                string sql;

                foreach (DynamicObject dataEntity in e.DataEntitys)
                {
                    //单据编号
                    string billNo = dataEntity.GetVal<string>("BillNo");

                    List<SpecialMatchDateForUser> list = new List<SpecialMatchDateForUser>();

                    //
                    DynamicObjectCollection detailCollection = dataEntity.GetVal<DynamicObjectCollection>("FEntity");
                    if (detailCollection.GetColumns<string>("FSOURCETYPE").Any(str => str != "AR_receivable")) continue;

                    List<string> sourceBillNoList =
                        detailCollection.GetColumns<string>("FSourceBillNo").Distinct().ToList();
                    if (sourceBillNoList.Count != 1) continue;

                    //源单编号
                    string sourceBillNo = sourceBillNoList.First();

                    int seq = 1;
                    foreach (DynamicObject detail in detailCollection)
                    {
                        SpecialMatchDateForUser matchData = new SpecialMatchDateForUser();
                        matchData.FBillId = dataEntity.GetVal<long>("Id");//Convert.ToInt64(obj.get_Item("FID"));
                        matchData.FEntryID = detail.GetVal<long>("Id"); //Convert.ToInt64(obj.get_Item("FENTRYID"));
                        matchData.FTheMatchAmount = detail.GetVal<decimal>("AMOUNTFOR_D");//Convert.ToDecimal(obj.get_Item("FAMOUNT"));
                        matchData.FFormId = BusinessObjectConst.AR_OtherRecAble;
                        matchData.FSeq = seq;
                        matchData.FMatchType = MatchType.RecableRec.ToString();
                        list.Add(matchData);
                        seq++;
                    }

                    //源单明细
                    sql = $@"
select b.FID,b.FENTRYID,b.FSEQ,b.FPAYAMOUNTFOR
from t_AR_receivable a
inner join t_AR_receivablePlan b on a.FID=b.FID
where a.FBILLNO='{sourceBillNo}'
";
                    DynamicObjectCollection sourceDetailCollection = DBUtils.ExecuteDynamicObject(Context, sql);

                    List<SpecialMatchDateForUser> sourceList = new List<SpecialMatchDateForUser>();

                    //
                    foreach (DynamicObject sourceDetail in sourceDetailCollection)
                    {
                        SpecialMatchDateForUser matchData = new SpecialMatchDateForUser();
                        matchData.FBillId = sourceDetail.GetVal<long>("FID");
                        matchData.FEntryID = sourceDetail.GetVal<long>("FENTRYID");
                        matchData.FTheMatchAmount = sourceDetail.GetVal<decimal>("FPAYAMOUNTFOR");
                        matchData.FFormId = BusinessObjectConst.AR_RECEIVABLE;
                        matchData.FSeq = sourceDetail.GetVal<int>("FSEQ");
                        matchData.FMatchType = MatchType.RecableRec.ToString();
                        sourceList.Add(matchData);
                    }

                    decimal sum = list.Sum(l => l.FTheMatchAmount);
                    decimal sourceSum = sourceList.Sum(s => s.FTheMatchAmount);
                    //金额方向相反不进行核销
                    if (!(sum > 0 ^ sourceSum > 0)) return;

                    sum = Math.Abs(sum);
                    sourceSum = Math.Abs(sourceSum);

                    decimal fp = 0;
                    for (int i = 0; i < sourceList.Count; i++)
                    {
                        var source = sourceList[i];
                        if (i != sourceList.Count - 1)
                        {
                            source.FTheMatchAmount = Math.Round((source.FTheMatchAmount / Math.Abs(source.FTheMatchAmount)) * Math.Abs(source.FTheMatchAmount * sum / sourceSum), 2);
                            fp += Math.Abs(source.FTheMatchAmount);
                        }
                        else
                        {
                            source.FTheMatchAmount = (source.FTheMatchAmount / Math.Abs(source.FTheMatchAmount)) * (sum - fp);
                        }
                    }

                    List<SpecialMatchDateForUser> resultList = new List<SpecialMatchDateForUser>();
                    resultList.AddRange(list);
                    resultList.AddRange(sourceList);

                    MatchParameters para = new MatchParameters();
                    para.BatchGeneRecord = true;
                    para.SpecialMatch = true;
                    para.MatchType = "3";
                    para.MatchMethodID = 38;
                    para.UserId = Context.UserId;

                    var result = MatchServiceHelper.SpecialMatchForUser(Context, para, resultList);
                    //MyProcess process = new MyProcess();
                    //var result = process.SpecialMatchForUser(Context, para, resultList);

                    MyExtend.CommonUtil.Log(nameof(para), KDObjectConverter.SerializeObject(para),
                        nameof(resultList), KDObjectConverter.SerializeObject(resultList),
                        nameof(result), KDObjectConverter.SerializeObject(result));

                }
            }
        }
    }
}
