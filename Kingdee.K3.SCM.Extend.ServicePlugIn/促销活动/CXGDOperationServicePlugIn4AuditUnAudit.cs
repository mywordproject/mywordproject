using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.Client;
using MyExtend;
using Newtonsoft.Json.Linq;

namespace Kingdee.K3.SCM.Extend.ServicePlugIn
{
    [Description("促销活动-审核、反审核-下推促销政策")]
    [HotUpdate]
    public class CXGDOperationServicePlugIn4AuditUnAudit : CommonOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FEntity");
            e.FieldKeys.Add("T_SPM_PromotionsGifts");
            e.FieldKeys.Add("T_SPM_PromotionsCustomer");
            e.FieldKeys.Add("FENTRYID");
            e.FieldKeys.Add("F_TRAX_OrgId");
            e.FieldKeys.Add("F_TRAX_PromotionMode");
            e.FieldKeys.Add("F_TRAX_GiftRestrictions");
            e.FieldKeys.Add("F_TRAX_VarietiesNumber");
            e.FieldKeys.Add("F_TRAX_PromotionsName");
            e.FieldKeys.Add("F_TRAX_Currency");
            e.FieldKeys.Add("F_TRAX_PromotionalContent");
            e.FieldKeys.Add("F_TRAX_CYZZBM");
            e.FieldKeys.Add("F_TRAX_StartTime");
            e.FieldKeys.Add("F_TRAX_EndTime");
            e.FieldKeys.Add("T_SPM_PromotionsCustomer");
            e.FieldKeys.Add("FGroupNumber");
            e.FieldKeys.Add("F_TRAX_ZHFS");
            e.FieldKeys.Add("F_TRAX_WLBM");
            e.FieldKeys.Add("F_TRAX_FZSX");
            e.FieldKeys.Add("F_TRAX_JLDW");
            e.FieldKeys.Add("F_TRAX_PurchasedQuantity");
            e.FieldKeys.Add("F_TRAX_PurchaseAmount");
            e.FieldKeys.Add("F_TRAX_PromotionalBasis");
            e.FieldKeys.Add("F_TRAX_GiveBase");
            e.FieldKeys.Add("F_TRAX_GiftRestrictions");
            e.FieldKeys.Add("F_TRAX_VarietiesNumber");
            e.FieldKeys.Add("F_TRAX_DiscountManner");
            e.FieldKeys.Add("F_TRAX_DiscountRate");
            e.FieldKeys.Add("F_TRAX_Discount");
            e.FieldKeys.Add("F_TRAX_ZDZKL");
            e.FieldKeys.Add("F_TRAX_ZDZKE");
            e.FieldKeys.Add("F_TRAX_Note");
            e.FieldKeys.Add("F_TRAX_JBGMSL");
            e.FieldKeys.Add("F_TRAX_JBZSSL");
            e.FieldKeys.Add("F_TRAX_JBJLDW");

            e.FieldKeys.Add("F_TRAX_ZHFS");
            e.FieldKeys.Add("F_TRAX_ZPBM");
            e.FieldKeys.Add("F_TRAX_Flex");
            e.FieldKeys.Add("F_TRAX_GJLDW");
            e.FieldKeys.Add("F_TRAX_GiftType");
            e.FieldKeys.Add("F_TRAX_GiftQty");
            e.FieldKeys.Add("F_TRAX_GNote");
            e.FieldKeys.Add("F_TRAX_GJBGMSL");
            e.FieldKeys.Add("F_TRAX_GJBZSSL");
            e.FieldKeys.Add("F_TRAX_GJBJLDW");

            e.FieldKeys.Add("F_TRAX_CustomerCode");
            
            e.FieldKeys.Add("F_TRAX_CJCDJE");
            e.FieldKeys.Add("F_TRAX_GSCDJE");
            e.FieldKeys.Add("F_TRAX_GCJCDBL");
            e.FieldKeys.Add("F_TRAX_GGSCDBL");
            e.FieldKeys.Add("F_TRAX_Price");
            e.FieldKeys.Add("F_TRAX_KHLB");
            e.FieldKeys.Add("F_TRAX_ParticipationOrg");
            e.FieldKeys.Add("F_TRAX_SaleOrgID");
            e.FieldKeys.Add("F_TRAX_StartDate");
            e.FieldKeys.Add("F_TRAX_EndDate");
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            foreach (DynamicObject dataEntity in e.DataEntitys)
            {
                //明细信息
                DynamicObjectCollection detailCollection = dataEntity.GetVal<DynamicObjectCollection>("FEntity");
                //组织信息
                DynamicObjectCollection ZZCollection = dataEntity.GetVal<DynamicObjectCollection>("ParticipationOrg");


                foreach (DynamicObject detail in detailCollection)
                {
                    //赠品信息
                    DynamicObjectCollection zpCollection = detail.GetVal<DynamicObjectCollection>("T_SPM_PromotionsGifts");
                    //参与客户
                    DynamicObjectCollection khCollection = detail.GetVal<DynamicObjectCollection>("T_SPM_PromotionsCustomer");

                    JObject modelJObject = new JObject();
                    //单据状态：FDocumentStatus 
                    //审核人：FApproverId 
                    //审核日期：FApproveDate 
                    //创建人：FCreatorId 
                    //失效人：FForbiderId 
                    //失效日期：FForbidDate 
                    //失效状态：FForbidStatus 
                    ////modelJObject.Add("FID", 0);//实体主键：FID 
                    ////modelJObject.Add("FBillNo", "");//促销政策编码：FBillNo
                    string str;
                    decimal dec;
                    int i;
                    modelJObject.Add("FCompanyOrgUnitID", "FNumber", str = dataEntity.GetVal<DynamicObject>("F_TRAX_OrgId").GetVal<string>("Number"));//创建组织：FCompanyOrgUnitID  (必填项)
                    modelJObject.Add("FPromotionModeID", "FNUMBER", str = detail.GetVal<DynamicObject>("F_TRAX_PromotionMode").GetVal<string>("FNumber"));//促销模式：FPromotionModeID  (必填项)
                    modelJObject.Add("FPresentLimit", str = detail.GetVal<string>("F_TRAX_GiftRestrictions"));//赠品种类限制：FPresentLimit  (必填项)
                    modelJObject.Add("FPresentQty", i = detail.GetVal<int>("F_TRAX_VarietiesNumber"));//赠送限制数量：FPresentQty 
                    modelJObject.Add("FJudgementModel", "1");//多组号执行策略：FJudgementModel  (必填项)
                    modelJObject.Add("FNAME", str = detail.GetVal<string>("F_TRAX_PromotionsName"));//主题：FNAME  (必填项)
                    modelJObject.Add("FCurrencyID", "FNUMBER", str = dataEntity.GetVal<DynamicObject>("F_TRAX_Currency").GetVal<string>("Number"));//币别：FCurrencyID  (必填项)
                    modelJObject.Add("FContent", str = detail.GetVal<string>("F_TRAX_PromotionalContent"));//促销内容：FContent  (必填项)
                    ////modelJObject.Add("FPromotionApply", "FNUMBER", "");//来自促销申请：FPromotionApply 
                    ////modelJObject.Add("FCreateDate", "1900-01-01");//创建日期：FCreateDate 
                    ////modelJObject.Add("FModifierId", "FUserID", "");//最后修改人：FModifierId 
                    ////modelJObject.Add("FModifyDate", "1900-01-01");//最后修改日期：FModifyDate 
                    modelJObject.Add("FIsDoubleDiscount", detail.GetVal<string>("F_TRAX_SFZCZSZZ") == "1" ? "true" : "false");//促销折扣是否支持折上折：FIsDoubleDiscount 
                    ////modelJObject.Add("FIsUseSumPromotion", "false");//赠品促销使用累计数量促销：FIsUseSumPromotion 
                    ////modelJObject.Add("FIsCountInitData", "false");//统计初始累计数量：FIsCountInitData
                    modelJObject.Add("F_TRAX_CXHDBH", dataEntity.GetVal<string>("BillNo"));
                    modelJObject.Add("F_TRAX_CXHDHH", detail.GetVal<int>("Id"));

                    //组织信息
                    //JArray zzJArray = new JArray();
                    //JObject zzJObject = new JObject();
                    ////销售组织名称：FSaleOrgName 
                    //////zzJObject.Add("FEntryID", 0);//实体主键：FEntryID 
                    //zzJObject.Add("FSaleOrgID", "FNumber", str = dataEntity.GetVal<DynamicObject>("F_TRAX_CYZZBM").GetVal<string>("Number"));//销售组织编码：FSaleOrgID  (必填项)
                    //zzJObject.Add("FStartTime", str = detail.GetVal<string>("F_TRAX_StartTime"));//开始时间：FStartTime  (必填项)
                    //zzJObject.Add("FEndTime", str = detail.GetVal<string>("F_TRAX_EndTime"));//结束时间：FEndTime  (必填项)
                    //////zzJObject.Add("FInitStartTime", "1900-01-01");//初始累计开始时间：FInitStartTime 
                    //////zzJObject.Add("FInitEndTime", "1900-01-01");//初始累计结束时间：FInitEndTime 
                    //////zzJObject.Add("FIsGiveUp", "false");//是否补赠：FIsGiveUp 
                    //zzJArray.Add(zzJObject);
                    //modelJObject.Add("FPromotionSaleOrg", zzJArray);

                    //客户信息
                    JArray khJArray = new JArray();
                    foreach (var kh in khCollection)
                    {
                        JObject khJObject = new JObject();
                        //客户名称：FCustomerName 
                        //客户类别编码：FCustomerTypeNumber 
                        ////khJObject.Add("FEntryID", 0);//实体主键：FEntryID 
                        if (kh["F_TRAX_CustomerCode"]!=null)
                        {
                            khJObject.Add("FCustomerID", "FNUMBER", str = kh.GetVal<DynamicObject>("F_TRAX_CustomerCode").GetVal<string>("Number"));//客户编码：FCustomerID 
                        }
                        if (kh["F_TRAX_KHLB"] != null)
                        {
                            khJObject.Add("FCustomerTypeId", "FNumber", str = kh.GetVal<DynamicObject>("F_TRAX_KHLB").GetVal<string>("FNumber"));//客户类别：FCustomerTypeId 
                        }
                        //khJObject.Add("FMasterID", "");//客户内码：FMasterID 
                        khJArray.Add(khJObject);
                    }
                    modelJObject.Add("FPromotionCustomer", khJArray);

                    int groupNumber = 1;
                    //促销信息
                    JArray detailJArray = new JArray();
                    JObject detailJObject = null;

                    //赠品
                    foreach (DynamicObject zp in zpCollection)
                    {
                        detailJObject = new JObject();
                        //物料名称：FMaterialName 
                        //规格型号：FMaterialModel 
                        //detailJObject.Add("FEntryID", 0);//实体主键：FEntryID 
                        detailJObject.Add("FGroupNumber", groupNumber);//组号：FGroupNumber 
                        detailJObject.Add("FCompoundMode", str = detail.GetVal<string>("F_TRAX_ZHFS"));//组合方式：FCompoundMode 

                        detailJObject.Add("FMaterialID", "FNUMBER", str = zp.GetVal<DynamicObject>("F_TRAX_ZPBM").GetVal<string>("Number"));//物料编码：FMaterialID 
                        //detailJObject.Add("FMaterialGroupID", "FNumber", str = zp.GetVal<DynamicObject>("F_TRAX_ZPBM").GetVal<DynamicObject>("MaterialGroup").GetVal<string>("Number"));//物料分组：FMaterialGroupID 
                        //detailJObject.Add("FAssistPropertyID", str = zp.GetVal<string>("F_TRAX_Flex"));//辅助属性：FAssistPropertyID 
                        //detailJObject.Add("FMatMasterID", i = zp.GetVal<DynamicObject>("F_TRAX_ZPBM").GetVal<int>("msterID"));//物料内码：FMatMasterID 

                        //detailJObject.Add("FBaseUnitID", "FNumber", str = zp.GetVal<DynamicObject>("F_TRAX_GJBJLDW").GetVal<string>("Number"));//基本计量单位：FBaseUnitID 
                        detailJObject.Add("FUnitID", "FNumber", str = zp.GetVal<DynamicObject>("F_TRAX_GJLDW").GetVal<string>("Number"));//计量单位：FUnitID 
                        //detailJObject.Add("FBasePurchaseQty", 0);//基本购买数量：FBasePurchaseQty 
                        //detailJObject.Add("FPurchaseQty", 0);//购买数量：FPurchaseQty 
                        //detailJObject.Add("FPurchaseAmount", 0);//购买金额：FPurchaseAmount 
                        detailJObject.Add("FRemark", str = zp.GetVal<string>("F_TRAX_GNote"));//备注：FRemark 

                        detailJObject.Add("FPresentType", str = zp.GetVal<string>("F_TRAX_GiftType"));//赠品：FPresentType 
                        detailJObject.Add("FEntryPresentQty", dec = zp.GetVal<decimal>("F_TRAX_GiftQty"));//赠送数量：FEntryPresentQty 
                        detailJObject.Add("FBasePresentQty", dec = zp.GetVal<decimal>("F_TRAX_GJBZSSL"));//基本赠送数量：FBasePresentQty 

                        //detailJObject.Add("FPresentCycleType", "");//赠送依据：FPresentCycleType 
                        //detailJObject.Add("FPresentCycleQty", 0);//赠送基数：FPresentCycleQty 
                        //detailJObject.Add("FEntryPresentLimit", "");//赠送品种限制：FEntryPresentLimit 
                        //detailJObject.Add("FEntryPresentLimitQty", 0);//品种数：FEntryPresentLimitQty 

                        //detailJObject.Add("FPromotionPrice", 0);//促销价格：FPromotionPrice 

                        //detailJObject.Add("FDiscountType", "");//折扣方式：FDiscountType 
                        //detailJObject.Add("FDiscountRate", 0);//折扣率%：FDiscountRate 
                        //detailJObject.Add("FDiscountAmount", 0);//折扣额：FDiscountAmount 
                        //detailJObject.Add("FBillDiscountRate", 0);//整单折扣率%：FBillDiscountRate 
                        //detailJObject.Add("FBillDiscountAmount", 0);//整单折扣额：FBillDiscountAmount 

                        detailJObject.Add("F_TRAX_CJCDBL", zp.GetVal<decimal>("F_TRAX_GCJCDBL"));//厂家承担比例：F_TRAX_CJCDBL
                        detailJObject.Add("F_TRAX_GSCDBL", zp.GetVal<decimal>("F_TRAX_GGSCDBL"));//公司承担比例：F_TRAX_GSCDBL

                        detailJArray.Add(detailJObject);
                    }

                    //基本商品
                    detailJObject = new JObject();
                    //物料名称：FMaterialName 
                    //规格型号：FMaterialModel 
                    //detailJObject.Add("FEntryID", 0);//实体主键：FEntryID 
                    detailJObject.Add("FGroupNumber", groupNumber);//组号：FGroupNumber 
                    detailJObject.Add("FCompoundMode", str = detail.GetVal<string>("F_TRAX_ZHFS"));//组合方式：FCompoundMode 

                    detailJObject.Add("FMaterialID", "FNUMBER", str = detail.GetVal<DynamicObject>("F_TRAX_WLBM").GetVal<string>("Number"));//物料编码：FMaterialID 
                    //detailJObject.Add("FMaterialGroupID", "FNumber", str = detail.GetVal<DynamicObject>("F_TRAX_WLBM").GetVal<DynamicObject>("MaterialGroup").GetVal<string>("Number"));//物料分组：FMaterialGroupID 
                    //detailJObject.Add("FAssistPropertyID", str = detail.GetVal<string>("F_TRAX_FZSX"));//辅助属性：FAssistPropertyID 
                    //detailJObject.Add("FMatMasterID", i = detail.GetVal<DynamicObject>("F_TRAX_WLBM").GetVal<int>("msterID"));//物料内码：FMatMasterID

                    //detailJObject.Add("FBaseUnitID", "FNumber", str = detail.GetVal<DynamicObject>("F_TRAX_JBJLDW").GetVal<string>("Number"));//基本计量单位：FBaseUnitID 
                    detailJObject.Add("FUnitID", "FNumber", str = detail.GetVal<DynamicObject>("F_TRAX_JLDW").GetVal<string>("Number"));//计量单位：FUnitID 
                    detailJObject.Add("FBasePurchaseQty", dec = detail.GetVal<decimal>("F_TRAX_JBGMSL"));//基本购买数量：FBasePurchaseQty 
                    detailJObject.Add("F_TRAX_Price", detail.GetVal<decimal>("F_TRAX_Price"));//单价
                    if (detail.GetVal<decimal>("F_TRAX_PurchasedQuantity") > 0)
                    {
                        detailJObject.Add("FPurchaseQty", dec = detail.GetVal<decimal>("F_TRAX_PurchasedQuantity"));//购买数量：FPurchaseQty 
                    }
                    if (detail.GetVal<decimal>("F_TRAX_PurchaseAmount") > 0)
                    {
                        detailJObject.Add("FPurchaseAmount", dec = detail.GetVal<decimal>("F_TRAX_PurchaseAmount"));//购买金额：FPurchaseAmount 
                    }
                    detailJObject.Add("FRemark", str = detail.GetVal<string>("F_TRAX_Note"));//备注：FRemark 

                    //detailJObject.Add("FPresentType", "");//赠品：FPresentType 
                    //detailJObject.Add("FEntryPresentQty", 0);//赠送数量：FEntryPresentQty 
                    //detailJObject.Add("FBasePresentQty", 0);//基本赠送数量：FBasePresentQty 

                    detailJObject.Add("FPresentCycleType", str = detail.GetVal<string>("F_TRAX_PromotionalBasis"));//赠送依据：FPresentCycleType 
                    detailJObject.Add("FPresentCycleQty", dec = detail.GetVal<decimal>("F_TRAX_GiveBase"));//赠送基数：FPresentCycleQty 
                    detailJObject.Add("FEntryPresentLimit", str = detail.GetVal<string>("F_TRAX_GiftRestrictions"));//赠送品种限制：FEntryPresentLimit 
                    detailJObject.Add("FEntryPresentLimitQty", dec = detail.GetVal<int>("F_TRAX_VarietiesNumber"));//品种数：FEntryPresentLimitQty 

                    detailJObject.Add("FPromotionPrice", dec = detail.GetVal<decimal>("F_TRAX_CXJG"));//促销价格：FPromotionPrice 

                    detailJObject.Add("FDiscountType", str = detail.GetVal<string>("F_TRAX_DiscountManner"));//折扣方式：FDiscountType 
                    detailJObject.Add("FDiscountRate", dec = detail.GetVal<decimal>("F_TRAX_DiscountRate"));//折扣率%：FDiscountRate 
                    detailJObject.Add("FDiscountAmount", dec = detail.GetVal<decimal>("F_TRAX_Discount"));//折扣额：FDiscountAmount 
                    detailJObject.Add("FBillDiscountRate", dec = detail.GetVal<decimal>("F_TRAX_ZDZKL"));//整单折扣率%：FBillDiscountRate 
                    detailJObject.Add("FBillDiscountAmount", dec = detail.GetVal<decimal>("F_TRAX_ZDZKE"));//整单折扣额：FBillDiscountAmount 

                    detailJObject.Add("F_TRAX_CJCDJE", detail.GetVal<decimal>("F_TRAX_CJCDJE"));//厂家承担金额：F_TRAX_CJCDJE
                    detailJObject.Add("F_TRAX_GSCDJE", detail.GetVal<decimal>("F_TRAX_GSCDJE"));//公司承担金额：F_TRAX_GSCDJE

                    detailJArray.Add(detailJObject);
                    //组织信息单据体
                    JArray n = new JArray();
                    foreach (var item in ZZCollection)
                    {
                        JObject info = new JObject();
                        JObject FSALEORGID = new JObject();
                        FSALEORGID.Add("FNUMBER",((DynamicObject) item["F_TRAX_SaleOrgID"])["Number"].ToString());
                        info.Add("FSALEORGID", FSALEORGID);
                        info.Add("FSTARTTIME",item["F_TRAX_StartDate"].ToString());
                        info.Add("FENDTIME",item["F_TRAX_EndDate"].ToString());
                        n.Add(info);//上游体属性
                    }
                    modelJObject.Add("FPromotionSaleOrg", n); //下游体属性
                    //FPromotionSaleOrg

                    modelJObject.Add("FPromotionPolicyEntry", detailJArray);

                    K3CloudApiClient client = new K3CloudApiClient("http://localhost/K3Cloud/");
                    //var loginResult = client.ValidateLogin("61e8b37dd131dc", "Administrator", "888888@m", 2052);
                    //var loginResult = client.ValidateLogin("61da1cd6be55ef", "Administrator", "888888@m", 2052);
                    var loginResult = client.ValidateLogin("61eb6ddc8a27a2", "Administrator", "888888@m", 2052);
                    var resultType = JObject.Parse(loginResult)["LoginResultType"].Value<int>();
                    if (resultType == 1)
                    {
                        JObject obj = new JObject();
                        obj.Add("IsAutoSubmitAndAudit", "true");
                        obj.Add("Model", modelJObject);
                        string dataString = obj.ToString();
                        string result = client.Save("SPM_PromotionPolicy", dataString);

                        MyExtend.CommonUtil.Log(nameof(dataString), dataString);
                        MyExtend.CommonUtil.Log(nameof(result), result);

                        Data data = Newtonsoft.Json.JsonConvert.DeserializeObject<Data>(result);
                        if (!data.Result.ResponseStatus.IsSuccess)
                        {
                            throw new KDBusinessException(null, string.Join(" ", data.Result.ResponseStatus.Errors.Select(_ => _.Message)));
                        }
                    }
                }
            }
        }
    }

    public class Error
    {
        public string Message { get; set; }
    }

    public class ResponseStatus
    {
        public bool IsSuccess { get; set; }
        public List<Error> Errors { get; set; }
    }

    public class Result
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class Data
    {
        public Result Result { get; set; }
    }
}
