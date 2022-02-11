using System;
using System.Collections.Generic;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace MyExtend
{

    public class CommonBillPlugIn : AbstractBillPlugIn
    {
        public void LogStep()
        {
            CommonUtil.LogStep();
        }

        public override void PreOpenForm(PreOpenFormEventArgs e)
        {
            base.PreOpenForm(e);
            LogStep();
        }

        public override void OnInitializeService(InitializeServiceEventArgs e)
        {
            base.OnInitializeService(e);
            LogStep();
        }

        public override void OnSetBusinessInfo(SetBusinessInfoArgs e)
        {
            base.OnSetBusinessInfo(e);
            LogStep();
        }

        public override void OnSetLayoutInfo(SetLayoutInfoArgs e)
        {
            base.OnSetLayoutInfo(e);
            LogStep();
        }

        public override void OnCreateDataBinder(CreateDataBinderArgs e)
        {
            base.OnCreateDataBinder(e);
            LogStep();
        }

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            LogStep();
        }

        public override void CreateNewData(BizDataEventArgs e)
        {
            base.CreateNewData(e);
            LogStep();
        }

        #region 加载新数据
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            LogStep();
        }

        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            LogStep();
        }

        public override void OnBillInitialize(BillInitializeEventArgs e)
        {
            base.OnBillInitialize(e);
            LogStep();
        }
        #endregion

        #region 加载已有数据
        public override void LoadData(LoadDataEventArgs e)
        {
            base.LoadData(e);
            LogStep();
        }

        public override void AfterLoadData(EventArgs e)
        {
            base.AfterLoadData(e);
            LogStep();
        }
        #endregion

        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LogStep();
        }

        public override void BeforeBindData(EventArgs e)
        {
            base.BeforeBindData(e);
            LogStep();
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            LogStep();
        }

        #region 单据转换
        public override void OnShowConvertOpForm(ShowConvertOpFormEventArgs e)
        {
            base.OnShowConvertOpForm(e);
            LogStep();
        }

        public override void OnGetConvertRule(GetConvertRuleEventArgs e)
        {
            base.OnGetConvertRule(e);
            LogStep();
        }
        #endregion

        #region 保存和提交
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            LogStep();
        }

        public override void SaveBillFailed(SaveBillFailedEventArgs e)
        {
            base.SaveBillFailed(e);
            LogStep();
        }

        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            LogStep();
        }

        public override void BeforeSubmit(BeforeSubmitEventArgs e)
        {
            base.BeforeSubmit(e);
            LogStep();
        }

        public override void AfterSubmit(AfterSubmitEventArgs e)
        {
            base.AfterSubmit(e);
            LogStep();
        }
        #endregion

        #region 其他
        public override void OnShowTrackResult(ShowTrackResultEventArgs e)
        {
            base.OnShowTrackResult(e);
            LogStep();
        }

        public override void CopyData(CopyDataEventArgs e)
        {
            base.CopyData(e);
            LogStep();
        }

        public override void AfterCopyData(CopyDataEventArgs e)
        {
            base.AfterCopyData(e);
            LogStep();
        }

        public override void BeforeSetStatus(BeforeSetStatusEventArgs e)
        {
            base.BeforeSetStatus(e);
            LogStep();
        }

        public override void AfterSetStatus(AfterSetStatusEventArgs e)
        {
            base.AfterSetStatus(e);
            LogStep();
        }

        public override void VerifyImportData(VerifyImportDataArgs e)
        {
            base.VerifyImportData(e);
            LogStep();
        }

        public override void BeforeImportGetBaseDataUseOrg(BeforeGetBaseDataUseOrgArgs e)
        {
            base.BeforeImportGetBaseDataUseOrg(e);
            LogStep();
        }

        public override void OnTargetBillChanged(TargetBillChangedEventArgs e)
        {
            base.OnTargetBillChanged(e);
            LogStep();
        }
        #endregion
    }
}
