using Kingdee.BOS.Core.BusinessFlow.PlugIn;
using Kingdee.BOS.Core.BusinessFlow.PlugIn.Args;

namespace YYLK
{
    internal class Business
    {
        public class CommonBusinessFlowServicePlugIn : AbstractBusinessFlowServicePlugIn, ILog
        {
            public void Log(params object[] strArr)
            {
                untils.Log(strArr);
            }

            public override void BeforeTrackBusinessFlow(BeforeTrackBusinessFlowEventArgs e)
            {
                base.BeforeTrackBusinessFlow(e);
                untils.LogStep();
            }

            public override void BeforeCreateArticulationRow(BeforeCreateArticulationRowEventArgs e)
            {
                base.BeforeCreateArticulationRow(e);
                untils.LogStep();
            }

            public override void BeforeWriteBack(BeforeWriteBackEventArgs e)
            {
                base.BeforeWriteBack(e);
                untils.LogStep();
            }

            public override void AfterCustomReadFields(AfterCustomReadFieldsEventArgs e)
            {
                base.AfterCustomReadFields(e);
                untils.LogStep();
            }

            public override void RuleFirstRunning(RuleFirstRunningEventArgs e)
            {
                base.RuleFirstRunning(e);
                untils.LogStep();
            }

            public override void AfterCommitAmount(AfterCommitAmountEventArgs e)
            {
                base.AfterCommitAmount(e);
                untils.LogStep();
            }

            public override void BeforeCheckHighLimit(BeforeCheckHighLimitEventArgs e)
            {
                base.BeforeCheckHighLimit(e);
                untils.LogStep();
            }

            public override void AfterCheckHighLimit(AfterCheckHighLimitEventArgs e)
            {
                base.AfterCheckHighLimit(e);
                untils.LogStep();
            }

            public override void BeforeCloseRow(BeforeCloseRowEventArgs e)
            {
                base.BeforeCloseRow(e);
                untils.LogStep();
            }

            public override void AfterCloseRow(AfterCloseRowEventArgs e)
            {
                base.AfterCloseRow(e);
                untils.LogStep();
            }

            public override void BeforeSaveWriteBackData(BeforeSaveWriteBackDataEventArgs e)
            {
                base.BeforeSaveWriteBackData(e);
                untils.LogStep();
            }

            public override void AfterSaveWriteBackData(AfterSaveWriteBackDataEventArgs e)
            {
                base.AfterSaveWriteBackData(e);
                untils.LogStep();
            }

            public override void FinishWriteBack(FinishWriteBackEventArgs e)
            {
                base.FinishWriteBack(e);
                untils.LogStep();
            }
        }
    }
}
