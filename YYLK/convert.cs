using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;

namespace YYLK
{
    internal class convert: AbstractConvertPlugIn, ILog
    {
        public void Log(params object[] strArr)
        {
            untils.Log(strArr);
        }

        public override void OnInitVariable(InitVariableEventArgs e)
        {
            base.OnInitVariable(e);
            untils.LogStep();
        }

        public override void OnQueryBuilderParemeter(QueryBuilderParemeterEventArgs e)
        {
            base.OnQueryBuilderParemeter(e);
            untils.LogStep();
        }

        public override void OnBeforeGetSourceData(BeforeGetSourceDataEventArgs e)
        {
            base.OnBeforeGetSourceData(e);
            untils.LogStep();
        }

        public override void OnInSelectedRow(InSelectedRowEventArgs e)
        {
            base.OnInSelectedRow(e);
            untils.LogStep();
        }

        public override void OnBeforeFieldMapping(BeforeFieldMappingEventArgs e)
        {
            base.OnBeforeFieldMapping(e);
            untils.LogStep();
        }

        public override void OnFieldMapping(FieldMappingEventArgs e)
        {
            base.OnFieldMapping(e);
            untils.LogStep();
        }

        public override void OnAfterFieldMapping(AfterFieldMappingEventArgs e)
        {
            base.OnAfterFieldMapping(e);
            untils.LogStep();
        }

        public override void OnCreateLink(CreateLinkEventArgs e)
        {
            base.OnCreateLink(e);
            untils.LogStep();
        }

        public override void OnSetLinkAmount(SetLinkAmountEventArgs e)
        {
            base.OnSetLinkAmount(e);
            untils.LogStep();
        }

        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            base.OnAfterCreateLink(e);
            untils.LogStep();
        }

        public override void OnGetSourceData(GetSourceDataEventArgs e)
        {
            base.OnGetSourceData(e);
            untils.LogStep();
        }

        public override void OnCreateTarget(CreateTargetEventArgs e)
        {
            base.OnCreateTarget(e);
            untils.LogStep();
        }

        public override void OnParseFilterOptions(ParseFilterOptionsEventArgs e)
        {
            base.OnParseFilterOptions(e);
            untils.LogStep();
        }

        public override void OnGetDrawSourceData(GetDrawSourceDataEventArgs e)
        {
            base.OnGetDrawSourceData(e);
            untils.LogStep();
        }

        public override void OnCreateDrawTarget(CreateDrawTargetEventArgs e)
        {
            base.OnCreateDrawTarget(e);
            untils.LogStep();
        }

        public override void OnBeforeGroupBy(BeforeGroupByEventArgs e)
        {
            base.OnBeforeGroupBy(e);
            untils.LogStep();
        }

        public override void OnParseFilter(ParseFilterEventArgs e)
        {
            base.OnParseFilter(e);
            untils.LogStep();
        }

        public override void OnGetConvertBusinessService(ConvertBusinessServiceEventArgs e)
        {
            base.OnGetConvertBusinessService(e);
            untils.LogStep();
        }

        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            untils.LogStep();
        }
    }
}
