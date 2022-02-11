using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace MyExtend
{
    public class CommonOperationServicePlugIn : AbstractOperationServicePlugIn, ILog
    {
        public void Log(params object[] strArr)
        {
            CommonUtil.Log(strArr);
        }

        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            CommonUtil.LogStep();
        }

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            CommonUtil.LogStep();
        }

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            CommonUtil.LogStep();
        }

        public override void BeforeDoSaveExecute(BeforeDoSaveExecuteEventArgs e)
        {
            base.BeforeDoSaveExecute(e);
            CommonUtil.LogStep();
        }

        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            CommonUtil.LogStep();
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            CommonUtil.LogStep();
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            CommonUtil.LogStep();
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            CommonUtil.LogStep();
        }

        public override void RollbackData(OperationRollbackDataArgs e)
        {
            base.RollbackData(e);
            CommonUtil.LogStep();
        }

        public override void InitializeOperationResult(IOperationResult result)
        {
            base.InitializeOperationResult(result);
            CommonUtil.LogStep();
        }
    }
}
