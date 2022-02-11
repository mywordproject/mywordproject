using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace YYLK
{
    public class operation: AbstractOperationServicePlugIn, ILog
    {
        public void Log(params object[] strArr)
        {
            untils.Log(strArr);
        }

        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            untils.LogStep();
        }

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            untils.LogStep();
        }

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            untils.LogStep();
        }

        public override void BeforeDoSaveExecute(BeforeDoSaveExecuteEventArgs e)
        {
            base.BeforeDoSaveExecute(e);
            untils.LogStep();
        }

        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            untils.LogStep();
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            untils.LogStep();
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            untils.LogStep();
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            untils.LogStep();
        }

        public override void RollbackData(OperationRollbackDataArgs e)
        {
            base.RollbackData(e);
            untils.LogStep();
        }

        public override void InitializeOperationResult(IOperationResult result)
        {
            base.InitializeOperationResult(result);
            untils.LogStep();
        }
    }
}
