using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Interaction;
using System.Data;
using Kingdee.BOS.App.Data;

namespace kingdee.CD.XTF.Business.PlugIn
{
    [Description("物料保存生成物料换算"), HotUpdate]
    public class SCHSWL : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            //加载ERP字段
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMATERIALID");//物料ID
            e.FieldKeys.Add("FNumber");//物料编码
            e.FieldKeys.Add("FBaseUnitId");//基本单位
            e.FieldKeys.Add("F_TRAX_PieceUnit");//件单位
            e.FieldKeys.Add("F_TRAX_HSJBQty");//换算基本数量
            e.FieldKeys.Add("F_TRAX_SFSCWLHS");//是否

        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            //获取数据包
            base.EndOperationTransaction(e);
            foreach (var a in e.DataEntitys)
            {
                var WLID = a["id"].ToString();//物料ID
                var WLBM = a["Number"].ToString();//物料编码
                var JDW = (a["F_TRAX_PieceUnit"] as DynamicObject)["id"].ToString();//件单位
                var JBSL = a["F_TRAX_HSJBQty"].ToString();//基本单位数量

                DynamicObjectCollection MaterialBase = a["MaterialBase"] as DynamicObjectCollection;//基本
                var JBDW = "";
                foreach (var item in MaterialBase)
                {
                     JBDW = (item["BaseUnitId"] as DynamicObject)["Id"].ToString();//基本单位ID
                }
                var SF = a["F_TRAX_SFSCWLHS"].ToString();//是否
                if (SF =="0")
                {
                    //构建一个IBillView实例，通过此实例,报错各个单据字段
                    IBillView billView = this.CreateBillView("BD_MATERIALUNITCONVERT");

                    //单据体数据集合
                    List<DynamicObject> saveObj = new List<DynamicObject>();
                    ((IBillViewService)billView).LoadData();

                    //触发插件的OnLoad事件：
                    // 组织控制基类插件，在OnLoad事件中，对主业务组织改变是否提示选项进行初始化。
                    // 如果不触发OnLoad事件，会导致主业务组织赋值不成功
                    DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
                    eventProxy.FireOnLoad();

                    //填充单据数据
                    Fill_YS(billView, WLID, WLBM, JDW, JBSL, JBDW);
                    saveObj.Add(billView.Model.DataObject);

                    //调用保存
                    OperateOption saveOption = OperateOption.Create();
                    this.SaveBill(billView, saveOption, saveObj);
                }
            }
        }
        private void Fill_YS(IBillView billView, string WLID,string WLBM,string JDW,string JBSL,string JBDW)
        {
            try
            {
                //单据头赋值
                billView.Model.SetItemValueByID("FMATERIALID",WLID,0 );//物料
                billView.Model.SetItemValueByID("FCURRENTUNITID", JDW, 0);//件
                billView.Model.SetValue("FCONVERTNUMERATOR", JBSL, 0);//基本数量
                billView.Model.SetItemValueByID("FDESTUNITID", JBDW, 0);//基本单位
            }
            catch (Exception w)
            {
                throw new KDBusinessException("", "错误信息：【" + w.Message.ToString() + "】");
            }
        }
        //传入表单原本单据标识，生成单据视图
        private IBillView CreateBillView(string FKEY)
        {
            // 读取物料的元数据
            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, FKEY) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();
            // 创建用于引入数据的单据view
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);
            // 开始初始化billView：
            // 创建视图加载参数对象，指定各种参数，如FormId, 视图(LayoutId)等
            BillOpenParameter openParam = CreateOpenParameter(meta);
            // 动态领域模型服务提供类，通过此类，构建MVC实例
            var provider = form.GetFormServiceProvider();
            billView.Initialize(openParam, provider);
            return billView as IBillView;
        }

        //元数据，视图加载参数对象
        private BillOpenParameter CreateOpenParameter(FormMetadata meta)
        {
            Form form = meta.BusinessInfo.GetForm();
            // 指定FormId, LayoutId
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            // 数据库上下文
            openParam.Context = this.Context;
            // 本单据模型使用的MVC框架
            openParam.ServiceName = form.FormServiceName;
            // 随机产生一个不重复的PageId，作为视图的标识
            openParam.PageId = Guid.NewGuid().ToString();
            // 元数据
            openParam.FormMetaData = meta;
            // 界面状态：新增 (修改、查看)
            openParam.Status = OperationStatus.ADDNEW;
            // 单据主键：本案例演示新建物料，不需要设置主键
            openParam.PkValue = null;
            // 界面创建目的：普通无特殊目的 （为工作流、为下推、为复制等）
            openParam.CreateFrom = CreateFrom.Default;
            // 基础资料分组维度：基础资料允许添加多个分组字段，每个分组字段会有一个分组维度
            // 具体分组维度Id，请参阅 form.FormGroups 属性
            openParam.GroupId = "";
            // 基础资料分组：如果需要为新建的基础资料指定所在分组，请设置此属性
            openParam.ParentId = 0;
            // 单据类型
            openParam.DefaultBillTypeId = "";
            // 业务流程
            openParam.DefaultBusinessFlowId = "";
            // 主业务组织改变时，不用弹出提示界面
            openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            // 插件
            List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
            openParam.SetCustomParameter(FormConst.PlugIns, plugs);
            PreOpenFormEventArgs args = new PreOpenFormEventArgs(this.Context, openParam);
            foreach (var plug in plugs)
            {// 触发插件PreOpenForm事件，供插件确认是否允许打开界面
                plug.PreOpenForm(args);
            }
            if (args.Cancel == true)
            {// 插件不允许打开界面
                // 本案例不理会插件的诉求，继续....
            }
            // 返回
            return openParam;
        }

        //单据保存方法
        private void SaveBill(IBillView billView, OperateOption saveOption, List<DynamicObject> objects)
        {
            Form form = billView.BillBusinessInfo.GetForm();
            if (form.FormIdDynamicProperty != null)
            {
                form.FormIdDynamicProperty.SetValue(billView.Model.DataObject, form.Id);
            }
            // 调用保存操作
            IOperationResult saveResult = BusinessDataServiceHelper.Save(
                        this.Context,
                        billView.BillBusinessInfo,
                        objects.ToArray());

            // 显示处理结果
            if (saveResult == null)
            {

                return;
            }
            else if (saveResult.IsSuccess == true)
            {
                // 保存成功，直接显示
                //保存1--提交--审核
                List<object> listid = new List<object>();
                foreach (var item in saveResult.SuccessDataEnity)
                {
                    listid.Add(item["Id"].ToString());
                }
                IOperationResult submitResult = BusinessDataServiceHelper.Submit(this.Context, billView.BillBusinessInfo, listid.ToArray(), "Submit", saveOption);
                IOperationResult auditResult = null;
                if (submitResult.IsSuccess == true)
                    auditResult = BusinessDataServiceHelper.Audit(this.Context, billView.BillBusinessInfo, listid.ToArray(), saveOption);
                if (auditResult.IsSuccess == true)
                {
                    int wid = Convert.ToInt32(billView.Model.DataObject["MaterialId_Id"]);
                    string sql = $@"/*dialect*/UPDATE T_BD_MATERIAL SET F_TRAX_SFSCWLHS = '1' WHERE FMATERIALID ='{wid}'";
                    DBUtils.Execute(Context, sql);
                    return;
                }
               
              
            }
            else if (saveResult.InteractionContext != null && saveResult.InteractionContext.Option.GetInteractionFlag().Count > 0)
            {
                // 保存失败，需要用户确认问题
                //InteractionUtil.DoInteraction(this.View, saveResult, saveOption,
                //    new Action<FormResult, IInteractionResult, OperateOption>((
                //        formResult, opResult, option) =>
                //    {
                //        // 用户确认完毕，重新调用保存处理
                //        this.SaveBill(billView, option, objects);
                //    }));
            }
            // 保存失败，显示错误信息
            if (saveResult.IsShowMessage)
            {
                saveResult.MergeValidateErrors();
                OperateResultCollection operates = new OperateResultCollection();
                foreach (var item in saveResult.OperateResult)
                {
                    if (item.SuccessStatus == false)
                    {
                        operates.Add(new OperateResult()
                        {
                            Name = item.Name,
                            Message = item.Message,
                            SuccessStatus = item.SuccessStatus
                        });
                    }
                }

                return;
            }
        }
    }
}
