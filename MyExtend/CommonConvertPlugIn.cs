using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;

namespace MyExtend
{
    public class CommonConvertPlugIn : AbstractConvertPlugIn, ILog
    {
        public void Log(params object[] strArr)
        {
            CommonUtil.Log(strArr);
        }

        public override void OnInitVariable(InitVariableEventArgs e)
        {
            base.OnInitVariable(e);
            CommonUtil.LogStep();
        }

        public override void OnQueryBuilderParemeter(QueryBuilderParemeterEventArgs e)
        {
            base.OnQueryBuilderParemeter(e);
            CommonUtil.LogStep();
        }

        public override void OnBeforeGetSourceData(BeforeGetSourceDataEventArgs e)
        {
            base.OnBeforeGetSourceData(e);
            CommonUtil.LogStep();
        }

        public override void OnInSelectedRow(InSelectedRowEventArgs e)
        {
            base.OnInSelectedRow(e);
            CommonUtil.LogStep();
        }

        public override void OnBeforeFieldMapping(BeforeFieldMappingEventArgs e)
        {
            base.OnBeforeFieldMapping(e);
            CommonUtil.LogStep();
        }

        public override void OnFieldMapping(FieldMappingEventArgs e)
        {
            base.OnFieldMapping(e);
            CommonUtil.LogStep();
        }

        public override void OnAfterFieldMapping(AfterFieldMappingEventArgs e)
        {
            base.OnAfterFieldMapping(e);
            CommonUtil.LogStep();
        }

        public override void OnCreateLink(CreateLinkEventArgs e)
        {
            base.OnCreateLink(e);
            CommonUtil.LogStep();
        }

        public override void OnSetLinkAmount(SetLinkAmountEventArgs e)
        {
            base.OnSetLinkAmount(e);
            CommonUtil.LogStep();
        }

        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            base.OnAfterCreateLink(e);
            CommonUtil.LogStep();
        }

        public override void OnGetSourceData(GetSourceDataEventArgs e)
        {
            base.OnGetSourceData(e);
            CommonUtil.LogStep();
        }

        public override void OnCreateTarget(CreateTargetEventArgs e)
        {
            base.OnCreateTarget(e);
            CommonUtil.LogStep();
        }

        public override void OnParseFilterOptions(ParseFilterOptionsEventArgs e)
        {
            base.OnParseFilterOptions(e);
            CommonUtil.LogStep();
        }

        public override void OnGetDrawSourceData(GetDrawSourceDataEventArgs e)
        {
            base.OnGetDrawSourceData(e);
            CommonUtil.LogStep();
        }

        public override void OnCreateDrawTarget(CreateDrawTargetEventArgs e)
        {
            base.OnCreateDrawTarget(e);
            CommonUtil.LogStep();
        }

        public override void OnBeforeGroupBy(BeforeGroupByEventArgs e)
        {
            base.OnBeforeGroupBy(e);
            CommonUtil.LogStep();
        }

        public override void OnParseFilter(ParseFilterEventArgs e)
        {
            base.OnParseFilter(e);
            CommonUtil.LogStep();
        }

        public override void OnGetConvertBusinessService(ConvertBusinessServiceEventArgs e)
        {
            base.OnGetConvertBusinessService(e);
            CommonUtil.LogStep();
        }

        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            CommonUtil.LogStep();
        }
    }
}
