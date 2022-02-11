using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;

namespace Kingdee.CD.STGYL
{
    public class CreditRecord
    {

        /// <summary>
        /// 操作返回结果
        /// </summary>
        public class Result
        {
            public bool IsSuccess = false;
            public string Msg = string.Empty;
            public object Data;
        }

        /// <summary>
        /// 操作单据类型
        /// </summary>
        public enum BillType
        {
            /// <summary>
            /// 临时档案
            /// </summary>
            KHZKB = 0,
            ///// <summary>
            ///// 收料通知单
            ///// </summary>
            //SLTZ,
            ///// <summary>
            ///// 应付单
            ///// </summary>
            //YFD,
            ///// <summary>
            ///// 付款单
            ///// </summary>
            //FKD,
            ///// <summary>
            ///// 付款退款单
            ///// </summary>
            //FKTKD
        }

        /// <summary>
        /// 单据表名
        /// 临时档案
        /// 收料通知单
        /// 应付单
        /// 付款单
        /// </summary>
        /// 反写单据体标识
        private List<string> billtablename = new List<string>() { "FKHZKBENTRYSYJL" };

        /// <summary>
        /// 信用额度增减
        /// </summary>
        /// <param name="billtype">单据类型</param>
        /// <param name="list">数据集-供应商ID,分录内码,单据内码,金额方向,数量,金额</param>
        /// <param name="context">当前上下文</param>
        /// <returns></returns>
        /// 加载单据对象
        public Result Operation(BillType billtype, List<object> list, Context context)
        {
            Result res = new Result();
            if (list is null || context is null)
                return res;

            try
            {
                //折扣表单据标识
                FormMetadata meta = (FormMetadata)MetaDataServiceHelper.Load(context, "TRAX_SAL_KHZKB");
                QueryBuilderParemeter queryParameter = new QueryBuilderParemeter()
                {
                    BusinessInfo = meta.BusinessInfo,
                    //过滤界面
                    FilterClauseWihtKey = $""
                };
                DynamicObject[] bills = BusinessDataServiceHelper.Load(context, meta.BusinessInfo.GetDynamicObjectType(), queryParameter);
                //if (bills.Length < 1)
                //{
                //    res.IsSuccess = false;
                //    res.Msg = "供应商档案不存在或已过期";
                //    return res;
                //}
                DynamicObject bill = bills[0];

                //switch (billtype)
                //{
                //    case BillType.LSDA:
                //        bill = LSDA(bills[0], list, context);
                //        break;
                //    case BillType.SLTZ:
                //        bill = SLTZ(bills[0], list, context);
                //        break;
                //    case BillType.YFD:
                //        bill = YFD(bills[0], list, context);
                //        break;
                //    case BillType.FKD:
                //        bill = FKD(bills[0], list, context);
                //        break;
                //    case BillType.FKTKD:
                //        bill = FKTKD(bills[0], list, context);
                //        break;
                //    default:
                //        res.IsSuccess = false;
                //        res.Msg = "单据类型不存在";
                //        break;
                //}
                IOperationResult r = BusinessDataServiceHelper.Save(context, meta.BusinessInfo, bill);
                res.IsSuccess = r.IsSuccess;
                res.Msg = r.OperateResult[0].Message;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Msg = ex.Message;
            }
            return res;
        }

        /// <summary>
        /// 客户折扣表
        /// </summary>
        /// <param name="list">单据类型,单据编号,厂家折扣比例,厂家折扣金额,公司折扣比例,公司折扣金额,单据时间</param>
        /// <param name="context"></param>
        /// <returns></returns>
        private DynamicObject LSDA(DynamicObject bill, List<object> list, Context context)
        {
            try
            {
                DynamicObjectCollection obj = bill["FLSDAEntity"] as DynamicObjectCollection;
                DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);

                data["F_TRAX_DJLX"] = list[1];//单据类型
                data["F_TRAX_DJBH"] = list[2];//单据编号
                data["F_TRAX_CJZKBL"] = list[3];//厂家折扣比例
                data["F_TRAX_CJZKJE"] = list[4];//厂家折扣金额
                data["F_TRAX_GSZKBL"] = list[5];//公司折扣比例
                data["F_TRAX_GSZKJE"] = list[6];//公司折扣金额
                data["F_TRAX_DJSJ"] = list[7];//单据时间
                obj.Add(data);

                //var amount = Convert.ToDouble(list[4]);
                //bill["FLSSUMCREDITAMOUNT"] = Convert.ToDouble(bill["FLSSUMCREDITAMOUNT"]) + amount;
                //bill["FCURCREDITAMOUNT"] = Convert.ToDouble(bill["FCREDITAMOUNT"]) + amount;
            }
            catch { throw; }
            return bill;
        }

        //    /// <summary>
        //    /// 收料通知信用额度更新
        //    /// </summary>
        //    /// <param name="list">供应商ID,单据行内码,单据内码,金额方向,数量,金额,使用组织，供应商，结算方</param>
        //    /// <param name="context"></param>
        //    /// <returns></returns>
        //    private DynamicObject SLTZ(DynamicObject bill, List<object> list, Context context)
        //    {
        //        try
        //        {
        //            DynamicObjectCollection obj = bill["FSLTZEntity"] as DynamicObjectCollection;
        //            DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);
        //            data["FSLTZENTRY"] = list[1];
        //            data["FSLTZFID"] = list[2];
        //            data["FSLTZDIRECTION"] = list[3];
        //            data["FSLTZBASEUNITQTY"] = list[4];
        //            data["FSLTZCREDITAMOUNT"] = list[5];
        //            data["FSLTZUPDATEDATE"] = DateTime.Now;
        //            data["FSLTZUPDATEUSERID_Id"] = context.UserId;
        //            data["FSLTZUPDATENOTE"] = list[6];
        //            data["FSLTZORGID_Id"] = list[7];
        //            data["FSLTZGYS_Id"] = list[8];
        //            data["FSLTZJSF_Id"] = list[9];
        //            obj.Add(data);
        //            var amount = Convert.ToDouble(list[5]);
        //            bill["FSLTZSUMCREDITAMOUNT"] = Convert.ToDouble(bill["FSLTZSUMCREDITAMOUNT"]) + amount;
        //            bill["FCURCREDITAMOUNT"] = Convert.ToDouble(bill["FCREDITAMOUNT"]) + Convert.ToDouble(bill["FLSSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FSLTZSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FYFDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKTKDSUMCREDITAMOUNT"]);
        //        }
        //        catch { throw; }
        //        return bill;
        //    }

        //    /// <summary>
        //    /// 应付单信用额度更新
        //    /// </summary>
        //    /// <param name="list">供应商ID,单据行内码,单据内码,金额方向,数量,金额，使用组织，供应商，结算方</param>
        //    /// <param name="context"></param>
        //    /// <returns></returns>
        //    private DynamicObject YFD(DynamicObject bill, List<object> list, Context context)
        //    {
        //        try
        //        {
        //            DynamicObjectCollection obj = bill["FYFDEntity"] as DynamicObjectCollection;
        //            DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);
        //            data["FYFDENTRY"] = list[1];
        //            data["FYFDFID"] = list[2];
        //            data["FYFDDIRECTION"] = list[3];
        //            data["FYFDBASEUNITQTY"] = list[4];
        //            data["FYFDCREDITAMOUNT"] = list[5];
        //            data["FYFDUPDATEDATE"] = DateTime.Now;
        //            data["FYFDUPDATEUSERID_Id"] = context.UserId;
        //            data["FYFDUPDATENOTE"] = list[6];
        //            data["FYFDORGID_Id"] = list[7];
        //            data["FYFDGYS_Id"] = list[8];
        //            data["FYFDJSF_Id"] = list[9];
        //            obj.Add(data);
        //            var amount = Convert.ToDouble(list[5]);
        //            bill["FYFDSUMCREDITAMOUNT"] = Convert.ToDouble(bill["FYFDSUMCREDITAMOUNT"]) + amount;
        //            bill["FCURCREDITAMOUNT"] = Convert.ToDouble(bill["FCREDITAMOUNT"]) + Convert.ToDouble(bill["FLSSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FSLTZSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FYFDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKTKDSUMCREDITAMOUNT"]);
        //        }
        //        catch { throw; }
        //        return bill;
        //    }

        //    /// <summary>
        //    /// 付款单信用额度更新
        //    /// </summary>
        //    /// <param name="list">供应商ID,单据行内码,单据内码,金额方向,数量,金额，使用组织，供应商，结算方</param>
        //    /// <param name="context"></param>
        //    /// <returns></returns>
        //    private DynamicObject FKD(DynamicObject bill, List<object> list, Context context)
        //    {
        //        try
        //        {
        //            DynamicObjectCollection obj = bill["FFKDEntity"] as DynamicObjectCollection;
        //            DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);

        //            data["FFKDENTRY"] = list[1];
        //            data["FFKDFID"] = list[2];
        //            data["FFKDDIRECTION"] = list[3];
        //            data["FFKDBASEUNITQTY"] = list[4];
        //            data["FFKDCREDITAMOUNT"] = list[5];
        //            data["FFKDUPDATEDATE"] = DateTime.Now;
        //            data["FFKDUPDATEUSERID_Id"] = context.UserId;
        //            data["FFKDUPDATENOTE"] = list[6];
        //            data["FFKDORGID_Id"] = list[7];
        //            data["FFKDGYS_Id"] = list[8];
        //            data["FFKDJSF_Id"] = list[9];
        //            obj.Add(data);

        //            var amount = Convert.ToDouble(list[5]);
        //            bill["FFKDSUMCREDITAMOUNT"] = Convert.ToDouble(bill["FFKDSUMCREDITAMOUNT"]) + amount;
        //            bill["FCURCREDITAMOUNT"] = Convert.ToDouble(bill["FCREDITAMOUNT"]) + Convert.ToDouble(bill["FLSSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FSLTZSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FYFDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKTKDSUMCREDITAMOUNT"]);
        //        }
        //        catch { throw; }
        //        return bill;
        //    }

        //    /// <summary>
        //    /// 付款退款单信用额度更新
        //    /// </summary>
        //    /// <param name="list">供应商ID,单据行内码,单据内码,金额方向,数量,金额，使用组织，供应商，结算方</param>
        //    /// <param name="context"></param>
        //    /// <returns></returns>
        //    private DynamicObject FKTKD(DynamicObject bill, List<object> list, Context context)
        //    {
        //        try
        //        {
        //            DynamicObjectCollection obj = bill["FFKTKDEntity"] as DynamicObjectCollection;
        //            DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);

        //            data["FFKTKDENTRY"] = list[1];
        //            data["FFKTKDFID"] = list[2];
        //            data["FFKTKDDIRECTION"] = list[3];
        //            data["FFKTKDBASEUNITQTY"] = list[4];
        //            data["FFKTKDCREDITAMOUNT"] = list[5];
        //            data["FFKTKDUPDATEDATE"] = DateTime.Now;
        //            data["FFKTKDUPDATEUSERID_Id"] = context.UserId;
        //            data["FFKTKDUPDATENOTE"] = list[6];
        //            data["FFKTKDORGID_Id"] = list[7];
        //            data["FFKTKDGYS_Id"] = list[8];
        //            data["FFKTKDJSF_Id"] = list[9];
        //            obj.Add(data);

        //            var amount = Convert.ToDouble(list[5]);
        //            bill["FFKTKDSUMCREDITAMOUNT"] = Convert.ToDouble(bill["FFKTKDSUMCREDITAMOUNT"]) + amount;
        //            bill["FCURCREDITAMOUNT"] = Convert.ToDouble(bill["FCREDITAMOUNT"]) + Convert.ToDouble(bill["FLSSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FSLTZSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FYFDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKDSUMCREDITAMOUNT"]) + Convert.ToDouble(bill["FFKTKDSUMCREDITAMOUNT"]);
        //        }
        //        catch { throw; }
        //        return bill;
        //    }

        //    /// <summary>
        //    /// 新建供应商信用档案
        //    /// </summary>
        //    /// <param name="list">数据集</param>
        //    /// <param name="context">上下文</param>
        //    /// <returns></returns>
        //    public Result CreatCredit(List<object> list, Context context)
        //    {
        //        Result res = new Result();
        //        try
        //        {
        //            //判断已存在供应商档案
        //            var isexist = DBUtils.ExecuteDynamicObject(context, $"select a.* from T_PROCRE_XYDN a  left join T_PROCRE_XYDNKZFSENTRY b on a.FID = b.FID where a.FSUBINVALIDDATE>GETDATE()  and a.FOBJECTID ='{list[0]}' and a.FISFC=0").Count > 0;
        //            if (isexist)
        //            {
        //                res.IsSuccess = false;
        //                res.Msg = "供应商信用档案已存在";
        //                return res;
        //            }

        //            FormMetadata meta = (FormMetadata)MetaDataServiceHelper.Load(context, "ora_CRE_SUPPARCHIVES");
        //            BusinessInfo business = meta.BusinessInfo;
        //            DynamicObject bill = new DynamicObject(business.GetDynamicObjectType());

        //            //币别
        //            bill["FCURRENCYID_Id"] = "1";

        //            //供应商
        //            bill["FOBJECTID_Id"] = list[0];

        //            //生效日期
        //            bill["FSUBEFFECTIVEDATE"] = DateTime.Now;

        //            //失效日期
        //            bill["FSUBINVALIDDATE"] = DateTime.Now.AddDays(364);

        //            DynamicObjectCollection obj = bill["FKZFSENTRY"] as DynamicObjectCollection;
        //            DynamicObject data = new DynamicObject(obj.DynamicCollectionItemPropertyType);
        //            data["FUSERORGID_Id"] = list[1];
        //            data["FZYFS"] = "全额占用";
        //            obj.Add(data);

        //            IOperationResult r = BusinessDataServiceHelper.Save(context, business, bill);
        //            if (r.IsSuccess)
        //            {
        //                foreach (var v in r.SuccessDataEnity)
        //                {
        //                    var id = long.Parse(v["Id"].ToString());
        //                    OperateOption saveOption = OperateOption.Create();
        //                    IOperationResult sres = BusinessDataServiceHelper.Submit(context, business, new object[] { id }, "Submit");
        //                    if (sres.IsSuccess)
        //                    {
        //                        IOperationResult ares = BusinessDataServiceHelper.Audit(context, business, new object[] { id }, null);
        //                        res.Msg = ares.IsSuccess ? "供应商信用档案建立成功！" : ares.OperateResult[0].Message;
        //                        return res;
        //                    }
        //                    res.Msg = sres.OperateResult[0].Message;
        //                    return res;
        //                }
        //            }
        //            res.IsSuccess = r.IsSuccess;
        //            res.Msg = r.ValidationErrors[0].Message;
        //        }
        //        catch { throw; }
        //        return res;
        //    }

        //    /// <summary>
        //    /// 获取对象的控制方式以及金额
        //    /// </summary>
        //    /// <param name="sup"> 控制对象</param>
        //    /// <param name="context"></param>
        //    /// <returns></returns>
        //    public object GetCreditType(object orgid, object sup, Context context)
        //    {
        //        return "";
        //    }



        //    /// <summary>
        //    /// 获取单据对应信用额度金额
        //    /// </summary>
        //    /// <param name="billtype">单据类型</param>
        //    /// <param name="list">数据集</param>
        //    /// <param name="context">上下文</param>
        //    /// <returns></returns>
        //    public Result GetCreditAmount(object sup, Context context)
        //    {
        //        Result res = new Result();
        //        try
        //        {
        //            string sql = $@"
        //                    select FCREDITAMOUNT + sum(isnull(b.FLSCREDITAMOUNT,0)+ isnull(c.FSLTZCREDITAMOUNT,0)+ isnull(d.FYFDCREDITAMOUNT,0)+ isnull(e.FFKDCREDITAMOUNT,0)+ isnull(f.FFKTKDCREDITAMOUNT,0) ) FBILLAMOUNT
        //                    from T_PROCRE_XYDN a 
        //                    left join T_PROCRE_XYDNLSDAENTRY b on a.FID = b.FID  
        //                    left join T_PROCRE_XYDNSLTZENTRY c on a.FID = c.FID                                                         
        //                    left join T_PROCRE_XYDNYFDENTRY d on a.FID = d.FID  
        //                    left join T_PROCRE_XYDNFKDENTRY e on a.FID = e.FID 
        //                    left join T_PROCRE_XYDNFKTKDENTRY f on a.FID = f.FID  
        //                    where FSUBINVALIDDATE>GETDATE() and FOBJECTID={sup} and a.FISFC=0
        //                    group by a.FCREDITAMOUNT";
        //            var obj = DBUtils.ExecuteDynamicObject(context, sql);
        //            if (obj.Count > 0)
        //            {
        //                res.IsSuccess = true;
        //                res.Data = obj[0][0];
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            res.Msg = ex.Message;
        //        }
        //        return res;
        //    }
    }
}
