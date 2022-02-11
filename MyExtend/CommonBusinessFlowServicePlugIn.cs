using Kingdee.BOS.Core.BusinessFlow.PlugIn;
using Kingdee.BOS.Core.BusinessFlow.PlugIn.Args;

namespace MyExtend
{
    public class CommonBusinessFlowServicePlugIn : AbstractBusinessFlowServicePlugIn, ILog
    {
        public void Log(params object[] strArr)
        {
            CommonUtil.Log(strArr);
        }

        public override void BeforeTrackBusinessFlow(BeforeTrackBusinessFlowEventArgs e)
        {
            base.BeforeTrackBusinessFlow(e);
            CommonUtil.LogStep();
        }

        public override void BeforeCreateArticulationRow(BeforeCreateArticulationRowEventArgs e)
        {
            base.BeforeCreateArticulationRow(e);
            CommonUtil.LogStep();
        }

        public override void BeforeWriteBack(BeforeWriteBackEventArgs e)
        {
            base.BeforeWriteBack(e);
            CommonUtil.LogStep();
        }

        public override void AfterCustomReadFields(AfterCustomReadFieldsEventArgs e)
        {
            base.AfterCustomReadFields(e);
            CommonUtil.LogStep();
        }

        public override void RuleFirstRunning(RuleFirstRunningEventArgs e)
        {
            base.RuleFirstRunning(e);
            CommonUtil.LogStep();
        }

        public override void AfterCommitAmount(AfterCommitAmountEventArgs e)
        {
            base.AfterCommitAmount(e);
            CommonUtil.LogStep();
        }

        public override void BeforeCheckHighLimit(BeforeCheckHighLimitEventArgs e)
        {
            base.BeforeCheckHighLimit(e);
            CommonUtil.LogStep();
        }

        public override void AfterCheckHighLimit(AfterCheckHighLimitEventArgs e)
        {
            base.AfterCheckHighLimit(e);
            CommonUtil.LogStep();
        }

        public override void BeforeCloseRow(BeforeCloseRowEventArgs e)
        {
            base.BeforeCloseRow(e);
            CommonUtil.LogStep();
        }

        public override void AfterCloseRow(AfterCloseRowEventArgs e)
        {
            base.AfterCloseRow(e);
            CommonUtil.LogStep();
        }

        public override void BeforeSaveWriteBackData(BeforeSaveWriteBackDataEventArgs e)
        {
            base.BeforeSaveWriteBackData(e);
            CommonUtil.LogStep();
        }

        public override void AfterSaveWriteBackData(AfterSaveWriteBackDataEventArgs e)
        {
            base.AfterSaveWriteBackData(e);
            CommonUtil.LogStep();
        }

        public override void FinishWriteBack(FinishWriteBackEventArgs e)
        {
            base.FinishWriteBack(e);
            CommonUtil.LogStep();
        }
    }
}
