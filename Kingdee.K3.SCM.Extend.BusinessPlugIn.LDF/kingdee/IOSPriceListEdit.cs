using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.BusinessEntity.Organizations;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.BD.ServiceHelper;
using Kingdee.K3.Core.BD;
using Kingdee.K3.Core.BD.ServiceArgs;
using Kingdee.K3.Core.MFG.EnumConst;
using Kingdee.K3.SCM.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
namespace Kingdee.K3.SCM.Extend.BusinessPlugIn.kingdee
{
	[Description("重构")]
	//热启动,不用重启IIS
	[HotUpdate]
	public class IOSPriceListEdit : AbstractBillPlugIn
	{
		private const long SETTLEORGFUNC = 107L;
		private const string TBImportSalPrice = "TBIMPORTSALPRICE";
		private const string TBImportPurPrice = "TBIMPORTPURPRICE";
		private const string TBImportSettlePrice = "TBIMPORTSETTLEPRICE";
		private string RatioKey = string.Empty;
		private string FormIDKey = string.Empty;
		private bool existsCurrency = true;
		private Dictionary<string, DynamicObject> materialDic = new Dictionary<string, DynamicObject>();
		private string message = string.Empty;
		private List<FieldAppearance> listFieldApp;
		private long lastAuxpropId;
		public override void OnInitialize(InitializeEventArgs e)
		{
			base.OnInitialize(e);
		}
		public override void AfterCreateNewData(EventArgs e)
		{
			if (base.View.OpenParameter.CreateFrom.Equals(CreateFrom.Copy))
			{
				return;
			}
			List<SelectorItemInfo> list = new List<SelectorItemInfo>();
			list.Add(new SelectorItemInfo("FIsAccountOrg"));
			QueryBuilderParemeter para = new QueryBuilderParemeter
			{
				FormId = "ORG_Organizations",
				SelectItems = list,
				FilterClauseWihtKey = string.Format("FOrgID={0}", base.Context.CurrentOrganizationInfo.ID)
			};
			DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
			if (dynamicObjectCollection != null && dynamicObjectCollection.Count > 0 && Convert.ToBoolean(dynamicObjectCollection[0]["FIsAccountOrg"]))
			{
				base.View.Model.SetValue("FCreateOrgId", base.Context.CurrentOrganizationInfo.ID);
				JSONObject defCurrencyAndExchangeTypeByBizOrgID = CommonServiceHelper.GetDefCurrencyAndExchangeTypeByBizOrgID(base.Context, Convert.ToInt64(base.Context.CurrentOrganizationInfo.ID));
				if (defCurrencyAndExchangeTypeByBizOrgID != null)
				{
					base.View.Model.SetValue("FCURRENCYID", Convert.ToInt64(defCurrencyAndExchangeTypeByBizOrgID["FCyForID"]));
				}
			}
			else
			{
				base.View.Model.SetValue("FCreateOrgId", null);
				base.View.Model.SetValue("FCURRENCYID", null);
			}
			this.LoadAcctSys();
		}
		public override void DataChanged(DataChangedEventArgs e)
		{
			base.DataChanged(e);
			string key;
			switch (key = e.Field.Key.ToUpperInvariant())
			{
				case "FPRICETYPE":
					this.ClearEntity();
					return;
				case "FCREATEORGID":
					if (e.NewValue != null && Convert.ToInt64(e.NewValue) > 0L)
					{
						long defaultCurrencyByBizOrgID = CommonServiceHelper.GetDefaultCurrencyByBizOrgID(base.Context, Convert.ToInt64(e.NewValue));
						base.View.Model.SetValue("FCURRENCYID", defaultCurrencyByBizOrgID);
						return;
					}
					base.View.Model.SetValue("FCURRENCYID", null);
					return;
				case "FAUXPROPID":
					{
						DynamicObject newAuxpropData = e.OldValue as DynamicObject;
						this.AuxpropDataChanged(newAuxpropData, e.Row);
						return;
					}
				case "FPRICEUNITID":
					{
						string str = Convert.ToString(base.View.Model.GetValue("FPRICETYPE"));
						if (str.EqualsIgnoreCase("MaterialGroup"))
						{
							DynamicObject dynamicObject = base.View.Model.GetValue("FPRICEUNITID", e.Row) as DynamicObject;
							if (!dynamicObject.IsNullOrEmpty())
							{
								long num2 = Convert.ToInt64(dynamicObject["Id"]);
								string strSql = string.Format("SELECT T2.FUNITID FROM T_BD_UNIT T1\r\nJOIN T_BD_UNIT T2 ON T1.FUNITGROUPID = T2.FUNITGROUPID\r\nWHERE T1.FUNITID = {0} AND T2.FISBASEUNIT = '1'", num2);
								long num3 = DBServiceHelper.ExecuteScalar<long>(base.Context, strSql, 0L, null);
								if (num3 == 0L)
								{
									num3 = num2;
								}
								base.View.Model.SetValue("FBaseUnitID", num3, e.Row);
								return;
							}
						}
						break;
					}
				case "FPRICE":
					{
						decimal price = Convert.ToDecimal(base.View.Model.GetValue("FPrice", e.Row));
						DynamicObject dynamicObject2 = base.View.Model.GetValue("FPRICEUNITID", e.Row) as DynamicObject;
						DynamicObject dynamicObject3 = base.View.Model.GetValue("FBaseUnitID", e.Row) as DynamicObject;
						DynamicObject dynamicObject4 = base.View.Model.GetValue("FMATERIALID", e.Row) as DynamicObject;
						if (dynamicObject2 != null && dynamicObject3 != null)
						{
							long unitId = Convert.ToInt64(dynamicObject2["Id"]);
							long baseunitId = Convert.ToInt64(dynamicObject3["Id"]);
							long materialmasterId = 0L;
							if (dynamicObject4 != null)
							{
								materialmasterId = Convert.ToInt64(dynamicObject4[FormConst.MASTER_ID]);
							}
							decimal baseUnitPrice = this.GetBaseUnitPrice(materialmasterId, unitId, baseunitId, price);
							base.View.Model.SetValue("FBaseUnitPrice", baseUnitPrice, e.Row);
							return;
						}
						break;
					}
				case "FTAXPRICE":
					{
						decimal d = Convert.ToDecimal(base.View.Model.GetValue("FTaxPrice", e.Row));
						decimal d2 = Convert.ToDecimal(base.View.Model.GetValue("FTaxRate", e.Row));
						var aa = d2 / 100m;
						decimal price2 = d/ ++(aa);
						DynamicObject dynamicObject5 = base.View.Model.GetValue("FPRICEUNITID", e.Row) as DynamicObject;
						DynamicObject dynamicObject6 = base.View.Model.GetValue("FBaseUnitID", e.Row) as DynamicObject;
						DynamicObject dynamicObject7 = base.View.Model.GetValue("FMATERIALID", e.Row) as DynamicObject;
						if (dynamicObject5 != null && dynamicObject6 != null)
						{
							long unitId2 = Convert.ToInt64(dynamicObject5["Id"]);
							long baseunitId2 = Convert.ToInt64(dynamicObject6["Id"]);
							long materialmasterId2 = 0L;
							if (dynamicObject7 != null)
							{
								materialmasterId2 = Convert.ToInt64(dynamicObject7[FormConst.MASTER_ID]);
							}
							decimal baseUnitPrice2 = this.GetBaseUnitPrice(materialmasterId2, unitId2, baseunitId2, price2);
							base.View.Model.SetValue("FBaseUnitPrice", baseUnitPrice2, e.Row);
						}
						break;
					}

					return;
			}
			var HSZZID = (this.View.Model.GetValue("FCREATEORGID") as DynamicObject)["Id"].ToString();
			var DJT = (DynamicObjectCollection)this.Model.DataObject["PRICELISTENTRY"];
			var WL = "";
			foreach (var item in DJT)
			{
				WL = item["MaterialId"] == null ? "" : (item["MaterialId"] as DynamicObject)["Id"].ToString();
			}
			if (e.Field.Key == "FMATERIALID")
			{
				string cgjmbdj= $@"/*dialect*/select b.F_TRAX_DDZKDJP,b.F_TRAX_XJZKDJP,b.F_TRAX_GDZKDJP
from t_PUR_PriceList a
inner join t_PUR_PriceListEntry b on a.fid=b.fid
WHERE b.FMATERIALID='{WL}'
AND a.FUSEORGID='{HSZZID}'";
				var date= DBUtils.ExecuteDynamicObject(Context, cgjmbdj);
                if (date.Count>0)
                {
                    foreach (var a in date)
                    {
						this.View.Model.SetValue("F_TRAX_DDZKDJP", a["F_TRAX_DDZKDJP"].ToString(),e.Row);
						this.View.Model.SetValue("F_TRAX_XJZKDJP", a["F_TRAX_XJZKDJP"].ToString(), e.Row);
						this.View.Model.SetValue("F_TRAX_GDZKDJP", a["F_TRAX_GDZKDJP"].ToString(), e.Row);
					}
					this.View.UpdateView("T_IOS_PRICELISTENTRY");
				}
			}
		}
		private void ClearEntity()
		{
			DynamicObjectCollection dynamicObjectCollection = base.View.Model.DataObject["PRICELISTENTRY"] as DynamicObjectCollection;
			dynamicObjectCollection.Clear();
			base.View.Model.CreateNewEntryRow("FEntity");
			base.View.UpdateView("FEntity");
		}
		private decimal GetBaseUnitPrice(long materialmasterId, long unitId, long baseunitId, decimal price)
		{
			if (baseunitId == unitId)
			{
				return price;
			}
			UnitConvert unitConvertRate = UnitConvertServiceHelper.GetUnitConvertRate(base.View.Context, new GetUnitConvertRateArgs
			{
				MasterId = materialmasterId,
				SourceUnitId = baseunitId,
				DestUnitId = unitId
			});
			return unitConvertRate.ConvertQty(price, "");
		}
		public override void AfterDoOperation(AfterDoOperationEventArgs e)
		{
			string key;
			switch (key = e.Operation.Operation.ToUpperInvariant())
			{
				case "NEWENTRY":
				case "COPYENTRYROW":
				case "INSERTENTRY":
				case "DELETEENTRY":
					this.ControlDocStatus();
					return;
				case "TBIMPORTSALPRICE":
					if (e.OperationResult.IsSuccess)
					{
						this.ShowPriceList("BD_SAL_PriceList");
						return;
					}
					break;
				case "TBIMPORTPURPRICE":
					if (e.OperationResult.IsSuccess)
					{
						this.ShowPriceList("PUR_PriceCategory");
						return;
					}
					break;
				case "TBIMPORTSETTLEPRICE":
					if (e.OperationResult.IsSuccess)
					{
						this.ImportNewSettlePrice();
					}
					break;

					return;
			}
		}
		private void ControlDocStatus()
		{
			if (!this.Model.DataObject["DocumentStatus"].ToString().ToUpperInvariant().Equals("C"))
			{
				return;
			}
			DynamicObjectCollection dynamicObjectCollection = base.View.Model.DataObject["PRICELISTENTRY"] as DynamicObjectCollection;
			if (dynamicObjectCollection != null)
			{
				if ((
					from p in dynamicObjectCollection
					where p != null && Convert.ToString(p["RowAuditStatus"]) != "A"
					select p).ToList<DynamicObject>().Count == 0)
				{
					base.View.Model.SetValue("FAuditStatus", "A");
					return;
				}
				base.View.Model.SetValue("FAuditStatus", "P");
			}
		}
		private void ShowPriceList(string fromID)
		{
			this.FormIDKey = fromID;
			if (!(this.Model.GetValue("FCREATEORGID") is DynamicObject))
			{
				base.View.ShowErrMessage(ResManager.LoadKDString("请选选择核算组织", "004011030002815", SubSystemType.SCM, new object[0]), "", MessageBoxType.Notice);
				return;
			}
			DynamicObject dynamicObject = this.Model.GetValue("FCURRENCYID") as DynamicObject;
			ListShowParameter listShowParameter = new ListShowParameter();
			listShowParameter.FormId = fromID;
			listShowParameter.IsLookUp = true;
			listShowParameter.IsShowApproved = true;
			listShowParameter.IsShowUsed = false;
			listShowParameter.IsIsolationOrg = false;
			listShowParameter.MultiSelect = false;
			listShowParameter.PageId = SequentialGuid.NewGuid().ToString();
			listShowParameter.ListFilterParameter.Filter = " FFORBIDSTATUS='A' \r\n                                                  AND FDOCUMENTSTATUS='C' \r\n                                                ";
			if (dynamicObject != null)
			{
				this.existsCurrency = true;
				IRegularFilterParameter expr_C7 = listShowParameter.ListFilterParameter;
				expr_C7.Filter += string.Format(" AND FCURRENCYID={0}", dynamicObject["Id"]);
			}
			else
			{
				this.existsCurrency = false;
			}
			base.View.ShowForm(listShowParameter, new Action<FormResult>(this.PriceCloseFun));
		}
		protected void PriceCloseFun(FormResult e)
		{
			if (e == null || e.ReturnData == null)
			{
				return;
			}
			ListSelectedRow listSelectedRow = (e.ReturnData as ListSelectedRowCollection).FirstOrDefault<ListSelectedRow>();
			if (listSelectedRow == null)
			{
				return;
			}
			DynamicObjectType objType;
			DynamicObjectCollection entityData = this.GetEntityDataObject(out objType);
			List<DynamicObject> resultList = this.GetRetrunPrinceList(listSelectedRow);
			if ((
				from p in resultList
				where Convert.ToInt32(p["FIsIncludedTax"]) == 1
				select p).Count<DynamicObject>() > 0)
			{
				base.View.ShowMessage(ResManager.LoadKDString("所选择的源价目表为含税价格，选“是”将直接引入作为结算价格。是否继续引入？", "004011030002818", SubSystemType.SCM, new object[0]), MessageBoxOptions.YesNo, delegate (MessageBoxResult result)
				{
					if (result == MessageBoxResult.Yes)
					{
						this.HandleResult(objType, entityData, resultList);
					}
				}, "", MessageBoxType.Notice);
				return;
			}
			this.HandleResult(objType, entityData, resultList);
		}
		private DynamicObjectCollection GetEntityDataObject(out DynamicObjectType objType)
		{
			Entity entity = base.View.BusinessInfo.GetEntity("FEntity");
			objType = entity.DynamicObjectType;
			return this.Model.GetEntityDataObject(entity);
		}
		private void HandleResult(DynamicObjectType objType, DynamicObjectCollection entityData, List<DynamicObject> resultList)
		{
			List<DynamicObject> resultList2 = this.HandleDestId(resultList);
			IOSPriceListEdit.ClearNullRows(entityData, objType);
			this.AddIsIncludedTax(resultList);
			this.AddToThisEntity(resultList2, objType, entityData);
			if (this.materialDic.Count > 0)
			{
				this.ShowConflictModel(entityData);
			}
		}
		private void ShowConflictModel(DynamicObjectCollection entityData)
		{
			DynamicObject dynamicObject = (DynamicObject)base.View.Model.GetValue("FCURRENCYID");
			K3DisplayerModel k3DisplayerModel = K3DisplayerModel.Create(base.Context, this.K3DisplayFields().ToArray(), null);
			using (Dictionary<string, DynamicObject>.KeyCollection.Enumerator enumerator = this.materialDic.Keys.GetEnumerator())
			{
				/**
				while (enumerator.MoveNext())
				{
					IOSPriceListEdit.<> c__DisplayClassb <> c__DisplayClassb = new IOSPriceListEdit.<> c__DisplayClassb();
					<> c__DisplayClassb.key = enumerator.Current;
					int seq = Convert.ToInt32(<> c__DisplayClassb.key.Split(new char[]
					{
						'$'
					})[0]);
					long num = Convert.ToInt64(this.materialDic[<> c__DisplayClassb.key]["FAuxPropId"]);
					IMetaDataService service = ServiceFactory.GetService<IMetaDataService>(base.Context);
					FormMetadata formMetadata = (FormMetadata)service.Load(base.Context, "BD_FLEXSITEMDETAILV", true);
					IViewService service2 = ServiceFactory.GetService<IViewService>(base.Context);
					DynamicObject auxObj = (num == 0L) ? null : service2.LoadSingle(base.Context, num, formMetadata.BusinessInfo.GetDynamicObjectType());
					string resultPrice = string.Format("{0}_{1}", Convert.ToDecimal(this.materialDic[<> c__DisplayClassb.key][this.RatioKey]).ToString(), Convert.ToDecimal(this.materialDic[<> c__DisplayClassb.key]["FPrice"]).ToString());
					DynamicObject dynamicObject2 = (
						from p in entityData
						where Convert.ToInt32(p["Seq"]) == seq && Convert.ToInt64(p["PRICEUNITID_Id"]) == Convert.ToInt64(this.materialDic[<> c__DisplayClassb.key]["FUnitID"]) && BDFlexServiceHelper.AuxPropEquals(this.Context, (DynamicObject)p["AuxPropId"], auxObj) && !string.Format("{0}_{1}", Convert.ToDecimal(p["PRICEBASE"]).ToString(), Convert.ToBoolean(this.materialDic[<> c__DisplayClassb.key]["FIsIncludedTax"]) ? Convert.ToDecimal(p["TaxPrice"]).ToString() : Convert.ToDecimal(p["FPrice"]).ToString()).Equals(resultPrice)
						select p).FirstOrDefault<DynamicObject>();
					if (dynamicObject2 != null)
					{
						new K3DisplayerMessage();
						k3DisplayerModel.AddMessage(string.Format("{0}~|~{1}~|~{2}~|~{3}~|~{4}~|~{5}~|~{6}~|~{7}~|~{8}", new object[]
						{
							(dynamicObject2["MaterialId"] as DynamicObject)["Number"],
							this.materialDic[<>c__DisplayClassb.key]["FMaterialName"],
							(dynamicObject2["PRICEUNITID"] as DynamicObject)["Name"],
							this.materialDic[<>c__DisplayClassb.key]["FIsIncludedTax"],
							string.Format("{0:F2}", dynamicObject2["PRICEBASE"]),
							string.Format("{0:F" + dynamicObject["PRICEDIGITS"].ToString() + "}", Convert.ToBoolean(this.materialDic[<>c__DisplayClassb.key]["FIsIncludedTax"]) ? dynamicObject2["TaxPrice"] : dynamicObject2["FPrice"]),
							string.Format("{0:F2}", this.materialDic[<>c__DisplayClassb.key][this.RatioKey]),
							string.Format("{0:F" + dynamicObject["PRICEDIGITS"].ToString() + "}", this.FormIDKey.Equals("BD_SAL_PriceList") ? this.materialDic[<>c__DisplayClassb.key]["FPrice"] : (Convert.ToBoolean(this.materialDic[<>c__DisplayClassb.key]["FIsIncludedTax"]) ? this.materialDic[<>c__DisplayClassb.key]["FTaxPrice"] : this.materialDic[<>c__DisplayClassb.key]["FPrice"])),
							seq
						}));
					}
				}**/
			}
			k3DisplayerModel.SummaryMessage = ResManager.LoadKDString("选择需要引入的数据！", "004011030006338", SubSystemType.SCM, new object[0]);
			k3DisplayerModel.MultiSelect = true;
			base.View.ShowK3Displayer(k3DisplayerModel, this.UpdateConfictModel(), "BOS_K3Displayer");
		}
		private List<FieldAppearance> K3DisplayFields()
		{
			if (this.listFieldApp != null && this.listFieldApp.Count > 0)
			{
				return this.listFieldApp;
			}
			this.listFieldApp = new List<FieldAppearance>();
			FieldAppearance fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FMaterialNo", ResManager.LoadKDString("物料编码", "004011030002824", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FMaterialName", ResManager.LoadKDString("物料名称", "004011030002827", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FBaseUnit", ResManager.LoadKDString("计价单位", "004011030006339", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<CheckBoxFieldAppearance, CheckBoxField>(base.Context, "FIsIncludedTax", ResManager.LoadKDString("含税", "004011000021378", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FCurPriceBase", ResManager.LoadKDString("当前价格系数", "004011030006113", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			fieldAppearance.TextAlign = 2;
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FCurPrice", ResManager.LoadKDString("当前价格", "004011030002830", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			fieldAppearance.TextAlign = 2;
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FNewPriceBase", ResManager.LoadKDString("引入价格系数", "004011030006114", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			fieldAppearance.TextAlign = 2;
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<TextFieldAppearance, TextField>(base.Context, "FNewPrice", ResManager.LoadKDString("引入价格", "004011030002833", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("100", base.Context.UserLocale.LCID);
			fieldAppearance.TextAlign = 2;
			this.listFieldApp.Add(fieldAppearance);
			fieldAppearance = K3DisplayerUtil.CreateDisplayerField<IntegerFieldAppearance, IntegerField>(base.Context, "FEntiySeq", ResManager.LoadKDString("对应行号", "004011030002836", SubSystemType.SCM, new object[0]), "", null);
			fieldAppearance.Width = new LocaleValue("1", base.Context.UserLocale.LCID);
			fieldAppearance.Visible = 0;
			this.listFieldApp.Add(fieldAppearance);
			return this.listFieldApp;
		}
		private Action<FormResult> UpdateConfictModel()
		{
			return delegate (FormResult result)
			{
				K3DisplayerModel k3DisplayerModel = result.ReturnData as K3DisplayerModel;
				if (k3DisplayerModel != null && k3DisplayerModel.BarItemKey.Equals("tbOK"))
				{
					K3DisplayerMessage[] messages = k3DisplayerModel.Messages;
					for (int i = 0; i < messages.Length; i++)
					{
						K3DisplayerMessage k3DisplayerMessage = messages[i];
						if (k3DisplayerMessage != null)
						{
							int row = Convert.ToInt32(k3DisplayerMessage.DataEntity["FEntiySeq"]) - 1;
							int entryRowCount = base.View.Model.GetEntryRowCount("FEntity");
							base.View.Model.CopyEntryRow("FEntity", row, entryRowCount, false);
							base.View.Model.SetValue("FPRICEBASE", k3DisplayerMessage.DataEntity["FNewPriceBase"], entryRowCount);
							base.View.Model.SetValue(Convert.ToBoolean(k3DisplayerMessage.DataEntity["FIsIncludedTax"]) ? "FTaxPrice" : "FPrice", k3DisplayerMessage.DataEntity["FNewPrice"], entryRowCount);
							base.View.InvokeFieldUpdateService(Convert.ToBoolean(k3DisplayerMessage.DataEntity["FIsIncludedTax"]) ? "FTaxPrice" : "FPrice", entryRowCount);
						}
					}
					base.View.UpdateView("FEntity");
					this.ControlDocStatus();
					base.View.UpdateViewState();
					this.Model.DataChanged = true;
				}
			};
		}
		private List<DynamicObject> GetRetrunPrinceList(ListSelectedRow row)
		{
			List<SelectorItemInfo> list = new List<SelectorItemInfo>();
			list.Add(new SelectorItemInfo("FCreateOrgId"));
			list.Add(new SelectorItemInfo("FCURRENCYID"));
			list.Add(new SelectorItemInfo("FMaterialId"));
			list.Add(new SelectorRefItemInfo("FMaterialId.FMasterId"));
			list.Add(new SelectorItemInfo("FMaterialName"));
			list.Add(new SelectorItemInfo("FAuxPropId"));
			list.Add(new SelectorItemInfo("FUnitID"));
			list.Add(new SelectorRefItemInfo("FUnitID.FName"));
			list.Add(new SelectorItemInfo("FPrice"));
			list.Add(new SelectorItemInfo("FAuxPropId"));
			list.Add(new SelectorItemInfo("FIsIncludedTax"));
			if (this.FormIDKey.Equals("PUR_PriceCategory"))
			{
				list.Add(new SelectorItemInfo("FDisableStatus"));
				list.Add(new SelectorItemInfo("FTaxPrice"));
				list.Add(new SelectorItemInfo("FTaxRate"));

				list.Add(new SelectorItemInfo("F_TRAX_DDZKJE"));
				list.Add(new SelectorItemInfo("F_TRAX_DDZKL"));
				list.Add(new SelectorItemInfo("F_TRAX_XJZKL"));
				list.Add(new SelectorItemInfo("F_TRAX_XJZKJE"));
				list.Add(new SelectorItemInfo("F_TRAX_GDZKL"));
				list.Add(new SelectorItemInfo("F_TRAX_GDZKJE"));
				list.Add(new SelectorItemInfo("F_TRAX_TSZKL"));
				list.Add(new SelectorItemInfo("F_TRAX_TSZKJE"));
				list.Add(new SelectorItemInfo("F_TRAX_HHZKBL"));
				list.Add(new SelectorItemInfo("F_TRAX_Decimal1"));
				list.Add(new SelectorItemInfo("F_TRAX_YBZKBL")); 
			    list.Add(new SelectorItemInfo("F_TRAX_TSZKDJP"));
				this.RatioKey = "FPriceCoefficient";
			}
			else
			{
				if (this.FormIDKey.Equals("BD_SAL_PriceList"))
				{
					list.Add(new SelectorItemInfo("FEntryForbidStatus"));
					list.Add(new SelectorItemInfo("FRowAuditStatus"));
					this.RatioKey = "FPriceBase";
				}
			}
			list.Add(new SelectorItemInfo(this.RatioKey));
			QueryBuilderParemeter para = new QueryBuilderParemeter
			{
				FormId = this.FormIDKey,
				SelectItems = list,
				FilterClauseWihtKey = string.Format(" FID = {0} ", row.PrimaryKeyValue)
			};
			DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
			if (this.FormIDKey.Equals("PUR_PriceCategory"))
			{
				return (
					from p in dynamicObjectCollection
					where p["FDisableStatus"].ToString().Equals("B")
					select p).ToList<DynamicObject>();
			}
			if (this.FormIDKey.Equals("BD_SAL_PriceList"))
			{
				return (
					from p in dynamicObjectCollection
					where p["FEntryForbidStatus"].ToString().Equals("A") && p["FRowAuditStatus"].ToString().Equals("A")
					select p).ToList<DynamicObject>();
			}
			return dynamicObjectCollection.ToList<DynamicObject>();
		}
		private List<DynamicObject> HandleDestId(List<DynamicObject> resultList)
		{
			List<DynamicObject> list = new List<DynamicObject>();
			DynamicObject dynamicObject = this.Model.GetValue("FCREATEORGID") as DynamicObject;
			long currentOrgID = Convert.ToInt64(dynamicObject["Id"]);
			List<DynamicObject> list2 = new List<DynamicObject>();
			List<DynamicObject> list3 = new List<DynamicObject>();
			foreach (DynamicObject current in resultList)
			{
				if (Convert.ToInt64(current["FCreateOrgId"]) == currentOrgID)
				{
					return resultList;
				}
				list2 = OrganizationServiceHelper.GetAllocatedDestInfo(base.Context, new List<AllocateSourceInfo>
				{
					new AllocateSourceInfo
					{
						FormId = "BD_MATERIAL",
						SourceId = current["FMaterialId"].ToString(),
						DestOrgId = currentOrgID.ToString()
					}
				}, false).ToList<DynamicObject>();
				if (list2.Count > 0)
				{
					list3.AddRange(list2);
				}
				else
				{
					this.message += string.Format(ResManager.LoadKDString("引入的价目表中【{0}】物料未分配至当前核算组织，不支持引入\r\n", "004011000021717", SubSystemType.SCM, new object[0]), Convert.ToString(current["FMaterialName"]));
				}
			}
			using (List<DynamicObject>.Enumerator enumerator2 = resultList.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DynamicObject obj = enumerator2.Current;
					IEnumerable<DynamicObject> source =
						from p in list3
						where Convert.ToInt64(p["FSOURCEID"]) == Convert.ToInt64(obj["FMaterialId_FMasterId"]) && Convert.ToInt64(p["FDESTORGID"]) == currentOrgID
						select p;
					if (source.Count<DynamicObject>() == 0)
					{
						obj["FMaterialId"] = obj["FMaterialId_FMasterId"];
						list.Add(obj);
					}
					else
					{
						DynamicObject dynamicObject2 = (
							from p in list3
							where Convert.ToInt64(p["FSOURCEID"]) == Convert.ToInt64(obj["FMaterialId"])
							select p).FirstOrDefault<DynamicObject>();
						if (dynamicObject2 != null)
						{
							obj["FMaterialId"] = dynamicObject2["FDESTID"];
							list.Add(obj);
						}
						else
						{
							dynamicObject2 = (
								from p in list3
								where Convert.ToInt64(p["FSDESTID"]) == Convert.ToInt64(obj["FMaterialId"])
								select p).FirstOrDefault<DynamicObject>();
							if (dynamicObject2 != null)
							{
								obj["FMaterialId"] = dynamicObject2["FDESTID"];
								list.Add(obj);
							}
						}
					}
				}
			}
			return list;
		}
		private void AddToThisEntity(List<DynamicObject> resultList, DynamicObjectType objType, DynamicObjectCollection entityData)
		{
			this.materialDic.Clear();
			int count = entityData.Count;
			object value = this.Model.GetValue("FEffectiveDATE");
			object value2 = this.Model.GetValue("FEXPIRYDATE");
			int num = 0;
			for (int i = 0; i < resultList.Count; i++)
			{
				if (i == 0 && !this.existsCurrency)
				{
					this.Model.SetValue("FCURRENCYID", resultList[i]["FCURRENCYID"]);
				}
				int num2 = num + count;
				long materialId = Convert.ToInt64(resultList[i]["FMaterialId"]);
				long unitID = Convert.ToInt64(resultList[i]["FUnitID"]);
				long num3 = Convert.ToInt64(resultList[i]["FAuxPropId"]);
				IMetaDataService service = ServiceFactory.GetService<IMetaDataService>(base.Context);
				FormMetadata formMetadata = (FormMetadata)service.Load(base.Context, "BD_FLEXSITEMDETAILV", true);
				IViewService service2 = ServiceFactory.GetService<IViewService>(base.Context);
				DynamicObject auxObj = (num3 == 0L) ? null : service2.LoadSingle(base.Context, num3, formMetadata.BusinessInfo.GetDynamicObjectType());
				DynamicObject dynamicObject = (
					from p in entityData
					where Convert.ToInt64(p["MaterialId_Id"]) == materialId && Convert.ToInt64(p["PRICEUNITID_Id"]) == unitID && BDFlexServiceHelper.AuxPropEquals(this.Context, (DynamicObject)p["AuxPropId"], auxObj)
					select p).FirstOrDefault<DynamicObject>();
				if (dynamicObject != null)
				{
					string text = string.Format("{0}_{1}", Convert.ToDecimal(dynamicObject["PRICEBASE"]).ToString(), Convert.ToBoolean(resultList[i]["FIsIncludedTax"]) ? Convert.ToDecimal(dynamicObject["TaxPrice"]).ToString() : Convert.ToDecimal(dynamicObject["FPrice"]).ToString());
					string text2 = string.Format("{0}_{1}", Convert.ToDecimal(resultList[i][this.RatioKey]).ToString(), this.FormIDKey.Equals("BD_SAL_PriceList") ? Convert.ToDecimal(resultList[i]["FPrice"]).ToString() : (Convert.ToBoolean(resultList[i]["FIsIncludedTax"]) ? Convert.ToDecimal(resultList[i]["FTaxPrice"]).ToString() : Convert.ToDecimal(resultList[i]["FPrice"]).ToString()));
					if (!text.Equals(text2))
					{
						string key = dynamicObject["Seq"].ToString() + "$" + text2;
						if (!this.materialDic.ContainsKey(key))
						{
							this.materialDic.Add(key, resultList[i]);
						}
					}
				}
				else
				{
					DynamicObject dynamicObject2 = new DynamicObject(objType);
					entityData.Add(dynamicObject2);
					this.Model.SetValue("FMaterialId", materialId, num2);
					base.View.InvokeFieldUpdateService("FMaterialId", num2);
					if (this.Model.GetValue("FMaterialId", num2) == null)
					{
						List<SelectorItemInfo> list = new List<SelectorItemInfo>();
						list.Add(new SelectorItemInfo("FForbidStatus"));
						list.Add(new SelectorItemInfo("FName"));
						QueryBuilderParemeter para = new QueryBuilderParemeter
						{
							FormId = "BD_MATERIAL",
							SelectItems = list,
							FilterClauseWihtKey = string.Format(" FMATERIALID = {0} ", materialId)
						};
						DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
						if (!dynamicObjectCollection.IsNullOrEmpty() && dynamicObjectCollection.Count > 0)
						{
							string a = Convert.ToString(dynamicObjectCollection[0]["FForbidStatus"]);
							if (a == "B")
							{
								this.message += string.Format(ResManager.LoadKDString("引入的价目表中【{0}】物料已禁用，不支持引入\r\n", "004011000021524", SubSystemType.SCM, new object[0]), Convert.ToString(dynamicObjectCollection[0]["FName"]));
							}
						}
						IOSPriceListEdit.ClearNullRows(entityData, objType);
					}
					else
					{
						dynamicObject2["seq"] = num2 + 1;
						this.Model.SetValue("FPRICEUNITID", unitID, num2);
						this.Model.SetValue("FAuxPropId", num3, num2);
						dynamicObject2["EntryEffectiveDate"] = value;
						dynamicObject2["EntryExpriyDate"] = value2;
						decimal num4 = Convert.ToDecimal(base.View.Model.GetValue("FTaxRate", num2));
						if (this.FormIDKey.Equals("PUR_PriceCategory"))
						{
							num4 = Convert.ToDecimal(resultList[i]["FTaxRate"]);
							dynamicObject2["TaxRate"] = num4;
							dynamicObject2["TaxPrice"] = resultList[i]["FTaxPrice"];
							dynamicObject2["FPrice"] = resultList[i]["FPrice"];

							dynamicObject2["F_TRAX_DDZKL"] = resultList[i]["F_TRAX_DDZKL"];
							dynamicObject2["F_TRAX_DDZKJE"] = resultList[i]["F_TRAX_DDZKJE"];
							dynamicObject2["F_TRAX_XJZKL"] = resultList[i]["F_TRAX_XJZKL"];
							dynamicObject2["F_TRAX_XJZKJE"] = resultList[i]["F_TRAX_XJZKJE"];
							dynamicObject2["F_TRAX_GDZKL"] = resultList[i]["F_TRAX_GDZKL"];
							dynamicObject2["F_TRAX_GDZKJE"] = resultList[i]["F_TRAX_GDZKJE"];
							dynamicObject2["F_TRAX_TSZKL"] = resultList[i]["F_TRAX_TSZKL"];
							dynamicObject2["F_TRAX_TSZKJE"] = resultList[i]["F_TRAX_TSZKJE"];
							dynamicObject2["F_TRAX_HHZKBL"] = resultList[i]["F_TRAX_HHZKBL"];
							dynamicObject2["F_TRAX_XSCBL"] = resultList[i]["F_TRAX_Decimal1"];
							dynamicObject2["F_TRAX_YBZKBL"] = resultList[i]["F_TRAX_YBZKBL"]; 
							dynamicObject2["F_TRAX_TSZKDJP"] = resultList[i]["F_TRAX_TSZKDJP"];
						}
						else
						{
							if (this.FormIDKey.Equals("BD_SAL_PriceList"))
							{
								if (Convert.ToBoolean(resultList[i]["FIsIncludedTax"]))
								{
									dynamicObject2["TaxPrice"] = resultList[i]["FPrice"];
									var a = num4 / 100m;
									dynamicObject2["FPrice"] = Convert.ToDecimal(resultList[i]["FPrice"]) /( ++a);
								}
								else
								{
									dynamicObject2["FPrice"] = resultList[i]["FPrice"];
									var b = num4 / 100m;
									dynamicObject2["TaxPrice"] = Convert.ToDecimal(resultList[i]["FPrice"]) * ++(b);
								}
							}
						}
						dynamicObject2["PRICEBASE"] = resultList[i][this.RatioKey];
						dynamicObject2["ForbidStatusEn"] = "0";
						num++;
					}
				}
			}
			if (!string.IsNullOrEmpty(this.message))
			{
				base.View.ShowWarnningMessage(this.message, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				this.message = string.Empty;
			}
			base.View.UpdateView("FEntity");
			this.ControlDocStatus();
			base.View.UpdateViewState();
			this.Model.DataChanged = true;
		}
		private void AddIsIncludedTax(List<DynamicObject> resultList)
		{
			using (List<DynamicObject>.Enumerator enumerator = resultList.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					DynamicObject current = enumerator.Current;
					base.View.Model.SetValue("FIsIncludedTax", Convert.ToString(current["FIsIncludedTax"]));
				}
			}
		}
		private static void ClearNullRows(DynamicObjectCollection entityData, DynamicObjectType objType)
		{
			DynamicObjectCollection dynamicObjectCollection = new DynamicObjectCollection(objType, null);
			foreach (DynamicObject current in entityData)
			{
				if (current["MaterialId"] == null)
				{
					dynamicObjectCollection.Add(current);
				}
			}
			foreach (DynamicObject current2 in dynamicObjectCollection)
			{
				entityData.Remove(current2);
			}
		}
		private void ImportNewSettlePrice()
		{
			DynamicObject dynamicObject = this.Model.GetValue("FCREATEORGID") as DynamicObject;
			if (dynamicObject == null)
			{
				base.View.ShowErrMessage(ResManager.LoadKDString("请选选择核算组织", "004011030002815", SubSystemType.SCM, new object[0]), "", MessageBoxType.Notice);
				return;
			}
			DynamicObject dynamicObject2 = this.Model.GetValue("FCURRENCYID") as DynamicObject;
			if (dynamicObject2 == null)
			{
				base.View.ShowErrMessage(ResManager.LoadKDString("请选选择币别", "004011030002839", SubSystemType.SCM, new object[0]), "", MessageBoxType.Notice);
				return;
			}
			DynamicObjectType dynamicObjectType;
			DynamicObjectCollection entityDataObject = this.GetEntityDataObject(out dynamicObjectType);
			List<long> list = new List<long>();
			foreach (DynamicObject current in entityDataObject)
			{
				if (current != null && current["MaterialId"] != null)
				{
					list.Add(Convert.ToInt64((current["MaterialId"] as DynamicObject)["Id"]));
				}
			}
			if (list.Count == 0)
			{
				base.View.ShowErrMessage(ResManager.LoadKDString("没有录入物料信息，请先录入物料信息。", "004011030002842", SubSystemType.SCM, new object[0]), "", MessageBoxType.Notice);
				return;
			}
			List<SelectorItemInfo> list2 = new List<SelectorItemInfo>();
			list2.Add(new SelectorItemInfo("FMaterialId"));
			list2.Add(new SelectorItemInfo("FUnitID"));
			list2.Add(new SelectorItemInfo("FPriceBase"));
			list2.Add(new SelectorItemInfo("FPrice"));
			list2.Add(new SelectorItemInfo("FAuxpropertyID"));
			QueryBuilderParemeter para = new QueryBuilderParemeter
			{
				FormId = "IOS_SettleTranLst",
				SelectItems = list2,
				FilterClauseWihtKey = string.Format(" FPrice>0 AND FMaterialId IN ({0}) AND FAcctOrgId={1} AND FCurrencyId={2}", string.Join<long>(",", list), dynamicObject["Id"], dynamicObject2["Id"]),
				OrderByClauseWihtKey = " FModifyDate DESC "
			};
			DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
			using (IEnumerator<DynamicObject> enumerator2 = dynamicObjectCollection.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DynamicObject obj = enumerator2.Current;
					DynamicObject dynamicObject3 = (
						from p in entityDataObject
						where Convert.ToInt64((p["MaterialId"] as DynamicObject)["Id"]) == Convert.ToInt64(obj["FMaterialId"])
						select p).FirstOrDefault<DynamicObject>();
					if (dynamicObject3 != null)
					{
						dynamicObject3["PRICEBASE"] = obj["FPriceBase"];
						dynamicObject3["FPrice"] = obj["FPrice"];
						this.Model.SetValue("FPRICEUNITID", obj["FUnitID"], Convert.ToInt32(dynamicObject3["seq"]));
						this.Model.SetValue("FAuxPropId", obj["FAuxpropertyID"], Convert.ToInt32(dynamicObject3["seq"]));
					}
				}
			}
		}
		public override void BeforeFlexSelect(BeforeFlexSelectEventArgs e)
		{
			base.BeforeFlexSelect(e);
			if (e.FieldKey.EqualsIgnoreCase("FAuxpropId"))
			{
				DynamicObjectCollection dynamicObjectCollection = base.View.Model.DataObject["PRICELISTENTRY"] as DynamicObjectCollection;
				this.lastAuxpropId = Convert.ToInt64(dynamicObjectCollection[e.Row]["AuxpropId_Id"]);
			}
		}
		public override void AfterShowFlexForm(AfterShowFlexFormEventArgs e)
		{
			base.AfterShowFlexForm(e);
			if (e.Result == AfterShowFlexFormEventArgs.FormResult.OK && e.FlexField.Key.EqualsIgnoreCase("FAuxpropId"))
			{
				this.AuxpropDataChanged(e.Row);
			}
		}
		private void AuxpropDataChanged(int row)
		{
			DynamicObjectCollection dynamicObjectCollection = base.View.Model.DataObject["PRICELISTENTRY"] as DynamicObjectCollection;
			long num = Convert.ToInt64(dynamicObjectCollection[row]["AuxpropId_Id"]);
			if (num == this.lastAuxpropId)
			{
				return;
			}
			DynamicObject dynamicObject = base.View.Model.GetValue("FBomId", row) as DynamicObject;
			long num2 = 0L;
			if (!dynamicObject.IsNullOrEmpty())
			{
				num2 = Convert.ToInt64(dynamicObject["Id"]);
			}
			DynamicObject dynamicObject2 = base.View.Model.GetValue("FMATERIALID", row) as DynamicObject;
			Convert.ToInt64(dynamicObject2["Id"]);
			base.View.Model.GetValue("FCreateOrgId", row);
			long lOrgId = Convert.ToInt64(dynamicObject2["Id"]);
			long masterId = Convert.ToInt64(dynamicObject2[FormConst.MASTER_ID]);
			long defaultBomKey = MFGServiceHelperForSCM.GetDefaultBomKey(base.Context, masterId, lOrgId, num, Kingdee.K3.Core.MFG.EnumConst.Enums.Enu_BOMUse.ZZBOM);
			if (defaultBomKey != num2)
			{
				base.View.Model.SetValue("FBomId", defaultBomKey, row);
			}
			this.lastAuxpropId = num;
		}
		private void AuxpropDataChanged(DynamicObject newAuxpropData, int row)
		{
			DynamicObject dynamicObject = base.View.Model.GetValue("FBomId", row) as DynamicObject;
			long num = 0L;
			if (!dynamicObject.IsNullOrEmpty())
			{
				num = Convert.ToInt64(dynamicObject["Id"]);
			}
			DynamicObject dynamicObject2 = base.View.Model.GetValue("FMATERIALID", row) as DynamicObject;
			Convert.ToInt64(dynamicObject2["Id"]);
			base.View.Model.GetValue("FCreateOrgId", row);
			long lOrgId = Convert.ToInt64(dynamicObject2["Id"]);
			long masterId = Convert.ToInt64(dynamicObject2[FormConst.MASTER_ID]);
			long defaultBomKey = MFGServiceHelperForSCM.GetDefaultBomKey(base.Context, masterId, lOrgId, newAuxpropData, Kingdee.K3.Core.MFG.EnumConst.Enums.Enu_BOMUse.ZZBOM);
			if (defaultBomKey != num)
			{
				base.View.Model.SetValue("FBomId", defaultBomKey, row);
			}
		}
		private void LoadAcctSys()
		{
			long num = Convert.ToInt64(base.View.Model.DataObject["CreateOrgId_Id"]);
			if (num <= 0L)
			{
				return;
			}
			FormMetadata formMetaData = MetaDataServiceHelper.GetFormMetaData(base.Context, "Org_AccountSystem");
			BusinessInfo subBusinessInfo = formMetaData.BusinessInfo.GetSubBusinessInfo(new List<string>
			{
				"FID",
				"FNumber",
				"FName",
				"FMAINORGID"
			});
			QueryBuilderParemeter queryParemeter = new QueryBuilderParemeter
			{
				BusinessInfo = subBusinessInfo,
				FilterClauseWihtKey = string.Format(" FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A' ", new object[0])
			};
			DynamicObject[] array = BusinessDataServiceHelper.LoadFromCache(base.Context, subBusinessInfo.GetDynamicObjectType(), queryParemeter);
			DynamicObjectCollection dynamicObjectCollection = (DynamicObjectCollection)base.View.Model.DataObject["PRICELISTACCTSYS"];
			DynamicObject[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				DynamicObject acctSysObject = array2[i];
				DynamicObjectCollection dynamicObjectCollection2 = acctSysObject["AcctSysEntry"] as DynamicObjectCollection;
				if (dynamicObjectCollection2 != null && dynamicObjectCollection2.Count != 0)
				{
					if (!dynamicObjectCollection.Any((DynamicObject p) => Convert.ToInt64(p["ACCTSysId_Id"]) == Convert.ToInt64(acctSysObject["Id"])))
					{
						DynamicObject dynamicObject = new DynamicObject(dynamicObjectCollection.DynamicCollectionItemPropertyType);
						dynamicObject["ACCTSysId_Id"] = Convert.ToInt64(acctSysObject["Id"]);
						dynamicObject["ACCTSysId"] = acctSysObject;
						dynamicObject["IsEnable"] = true;
						dynamicObjectCollection.Add(dynamicObject);
					}
				}
			}
			this.OrderAcctSysCollection(dynamicObjectCollection);
		}
		private void OrderAcctSysCollection(DynamicObjectCollection acctSysCollection)
		{
			IOrderedEnumerable<DynamicObject> orderedEnumerable =
				from n in acctSysCollection
				where n["ACCTSysId"] != null
				orderby ((DynamicObject)n["ACCTSysId"])["Number"]
				select n;
			DynamicObjectCollection dynamicObjectCollection = new DynamicObjectCollection(acctSysCollection.DynamicCollectionItemPropertyType, null);
			foreach (DynamicObject current in orderedEnumerable)
			{
				dynamicObjectCollection.Add(current);
			}
			acctSysCollection.Clear();
			foreach (DynamicObject current2 in dynamicObjectCollection)
			{
				acctSysCollection.Add(current2);
			}
		}
		public override void BeforeF7Select(BeforeF7SelectEventArgs e)
		{
			string a;
			if ((a = e.FieldKey.ToUpperInvariant()) != null)
			{
				if (!(a == "FACCTSYSID"))
				{
					if (!(a == "FLOT"))
					{
						if (a == "FBOMID")
						{
							string text = this.GetBomFilter(e.Row);
							if (string.IsNullOrEmpty(e.ListFilterParameter.Filter))
							{
								e.ListFilterParameter.Filter = text;
							}
							else
							{
								IRegularFilterParameter expr_1CB = e.ListFilterParameter;
								expr_1CB.Filter = expr_1CB.Filter + " AND " + text;
							}
							(e.DynamicFormShowParameter as ListShowParameter).IsIsolationOrg = false;
							e.IsShowApproved = false;
						}
					}
					else
					{
						string text = string.Empty;
						DynamicObject dynamicObject = base.View.Model.GetValue("FMATERIALID", e.Row) as DynamicObject;
						if (dynamicObject != null)
						{
							text = string.Format(" EXISTS (SELECT 1 FROM T_BD_MATERIAL TBM WHERE TBM.FMASTERID = {0} AND FMATERIALID = TBM.FMATERIALID)", Convert.ToInt64(dynamicObject["MsterId"]));
						}
						if (string.IsNullOrEmpty(e.ListFilterParameter.Filter))
						{
							e.ListFilterParameter.Filter = text;
						}
						else
						{
							IRegularFilterParameter expr_180 = e.ListFilterParameter;
							expr_180.Filter = expr_180.Filter + " AND " + text;
						}
					}
				}
				else
				{
					DynamicObjectCollection dynamicObjectCollection = base.View.Model.DataObject["PRICELISTACCTSYS"] as DynamicObjectCollection;
					List<long> list = new List<long>();
					if (dynamicObjectCollection != null && dynamicObjectCollection.Count > 0)
					{
						list = (
							from p in dynamicObjectCollection
							select Convert.ToInt64(p["ACCTSysId_Id"])).ToList<long>();
					}
					if (list.Count == 0)
					{
						list.Add(0L);
					}
					string text2 = string.Format(" FACCTSYSTEMID Not In ({0})", string.Join<long>(",", list));
					if (string.IsNullOrEmpty(e.ListFilterParameter.Filter))
					{
						e.ListFilterParameter.Filter = text2;
					}
					else
					{
						IRegularFilterParameter expr_F2 = e.ListFilterParameter;
						expr_F2.Filter = expr_F2.Filter + " AND " + text2;
					}
				}
			}
			base.BeforeF7Select(e);
		}
		public override void BeforeSetItemValueByNumber(BeforeSetItemValueByNumberArgs e)
		{
			string a;
			if ((a = e.BaseDataFieldKey.ToUpper()) != null)
			{
				if (!(a == "FBOMID"))
				{
					if (!(a == "FLOT"))
					{
						return;
					}
					string text = this.GetLotFilter(e.Row);
					if (string.IsNullOrEmpty(e.Filter))
					{
						e.Filter = text;
						return;
					}
					e.Filter = e.Filter + " AND " + text;
				}
				else
				{
					string text = this.GetBomFilter(e.Row);
					if (string.IsNullOrEmpty(e.Filter))
					{
						e.Filter = text;
						return;
					}
					e.Filter = e.Filter + " AND " + text;
					return;
				}
			}
		}
		private string GetBomFilter(int row)
		{
			DynamicObject dynamicObject = base.View.Model.GetValue("FMATERIALID", row) as DynamicObject;
			if (dynamicObject == null)
			{
				return string.Format(" FID={0}", 0);
			}
			long lMaterialMasterId = Convert.ToInt64(dynamicObject["msterID"]);
			DynamicObject dynamicObject2 = base.View.Model.GetValue("FCreateOrgId", row) as DynamicObject;
			long lOrgId = (dynamicObject2 == null) ? 0L : Convert.ToInt64(dynamicObject2["Id"]);
			DynamicObject auxpropData = base.View.Model.GetValue("FAuxpropId", row) as DynamicObject;
			List<long> approvedBomIdByOrgId = MFGServiceHelperForSCM.GetApprovedBomIdByOrgId(base.View.Context, lMaterialMasterId, lOrgId, auxpropData);
			string result;
			if (!approvedBomIdByOrgId.IsEmpty<long>())
			{
				result = string.Format(" FID IN ({0}) ", string.Join<long>(",", approvedBomIdByOrgId));
			}
			else
			{
				result = string.Format(" FID={0}", 0);
			}
			return result;
		}
		private string GetLotFilter(int row)
		{
			DynamicObject dynamicObject = base.View.Model.GetValue("FMATERIALID", row) as DynamicObject;
			if (dynamicObject == null)
			{
				return string.Format(" FID = {0} ", 0);
			}
			return string.Format(" EXISTS (SELECT 1 FROM T_BD_MATERIAL TBM WHERE TBM.FMASTERID = {0} AND FMATERIALID = TBM.FMATERIALID)", Convert.ToInt64(dynamicObject["MsterId"]));
		}
	}
}

