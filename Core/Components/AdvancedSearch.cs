using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;
using System.Globalization;

namespace Core.Components
{
    internal class AdvancedSearch : PopupEditor
    {
        private GridView _filterGrid;
        private GridView _orderByGrid;
        private SearchEntry _activeState;
        private readonly Type _entityType;
        private List<GridPolicy> _headers;

        public ListView ParentListView;
        public string ModelNameSpace { get; }
        private AdvSearchVM AdvSearchEntity => Entity as AdvSearchVM;
        public int _orderById = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(OrderBy)).Id;
        public int _fieldConditionId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(FieldCondition)).Id;
        public int _gridPolicyId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(GridPolicy)).Id;
        public int _entityId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(Entity)).Id;

        public AdvancedSearch(ListView parent) : base(nameof(GridPolicy))
        {
            Name = "AdvancedSearch";
            Title = "Tìm kiếm nâng cao";
            Icon = "fa fa-search-plus";
            ParentListView = parent;
            _entityType = Type.GetType((ParentListView.GuiInfo.Reference.Namespace ?? Client.ModelNamespace) + ParentListView.GuiInfo.RefName);
            DOMContentLoaded += LocalRender;
        }

        public void LocalRender()
        {
            _headers = ParentListView.Header
                .Where(x => x.Id > 0 && !x.ShortDesc.IsNullOrWhiteSpace() && x.Active && !x.Hidden).ToList();
            Feature.FeaturePolicy.Add(new FeaturePolicy
            {
                CanRead = true,
                CanWrite = true,
                CanDelete = true
            });
            Entity = ParentListView.AdvSearchVM;
            var fieldMap = HeaderForAdvSearch();
            var orderby = OdataExt.GetClausePart(ParentListView.FormattedDataSource, OdataExt.OrderByKeyword);
            ParentListView.AdvSearchVM.OrderBy = orderby.Split(",").Select(x =>
            {
                var orderField = x.Trim().Replace(new RegExp(@"\s+"), " ").Split(" ");
                if (orderField.Length < 1)
                {
                    return null;
                }

                var field = _headers.FirstOrDefault(header => header.FieldName == orderField[0]);
                if (field is null)
                {
                    return null;
                }

                var result = new OrderBy
                {
                    FieldId = field.Id,
                    Field = field
                };
                if (orderField.Length == 1)
                {
                    result.OrderbyOptionId = OrderbyOption.ASC;
                }
                else
                {
                    var parsed = Enum.TryParse(orderField[1].ToString().ToUpper(), out OrderbyOption orderbyOption);
                    result.OrderbyOptionId = parsed ? orderbyOption : OrderbyOption.ASC;
                }
                return result;
            }).Where(x => x != null).ToList();
            var section = AddSection();
            AddFilters(section);
            AddOrderByGrid(section);
        }

        private Section AddSection()
        {
            var section = new Section(ElementType.div)
            {
                ComponentGroup = new ComponentGroup
                {
                    Column = 4,
                    Label = "Bộ lọc",
                    Active = true,
                    ClassName = "scroll-content",
                }
            };
            AddChild(section);
            var label = new HTMLLabelElement();
            section.Element.AppendChild(label);
            label.TextContent = "Trạng thái";
            section.ClassName = "filter-warpper panel group wrapper";
            return section;
        }

        private void AddFilters(Section section)
        {
            _activeState = new SearchEntry(new Component
            {
                FieldName = nameof(AdvSearchVM.ActiveState),
                Column = 4,
                Label = "Trạng thái",
                FormatData = "{Description}",
                ShowLabel = true,
                LocalRender = true,
                ReferenceId = _entityId,
                RefName = nameof(Entity),
                Reference = new Entity { Name = nameof(Entity) },
                Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
            });
            _activeState.GuiInfo.LocalData = IEnumerableExtensions.ToEntity<ActiveStateEnum>();
            _activeState.GuiInfo.LocalHeader = new List<GridPolicy>
            {
                new GridPolicy
                {
                    FieldName = nameof(Core.Models.Entity.Name),
                    ShortDesc = "Trạng thái",
                    Active = true,
                },
                new GridPolicy
                {
                    FieldName = nameof(Core.Models.Entity.Description),
                    ShortDesc = "Miêu tả",
                    Active = true,
                }
            };
            _activeState.ParentElement = section.Element;
            section.AddChild(_activeState);

            _filterGrid = new GridView(new Component
            {
                FieldName = nameof(AdvSearchVM.Conditions),
                Column = 4,
                ReferenceId = _fieldConditionId,
                RefName = nameof(FieldCondition),
                Reference = new Entity { Name = nameof(FieldCondition), Namespace = typeof(Component).Namespace + "." },
                LocalRender = true,
                IgnoreConfirmHardDelete = true,
                CanAdd = true,
                Events = "{'DOMContentLoaded': 'FilterDomLoaded'}",
            });
            _filterGrid.OnDeleteConfirmed += () =>
            {
                _filterGrid.GetSelectedRows().ForEach(_filterGrid.RowData.Remove);
            };
            _filterGrid.GuiInfo.LocalHeader = new List<GridPolicy>
            {
                new GridPolicy
                {
                    Id = 1,
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.FieldId),
                    Events = "{'change': 'FieldId_Changed'}",
                    ShortDesc = "Tên cột",
                    ReferenceId = _gridPolicyId,
                    RefName = nameof(GridPolicy),
                    FormatCell = "{ShortDesc}",
                    Active = true,
                    Editable = true,
                    ComponentType = "Dropdown",
                    MinWidth = "100px",
                    MaxWidth = "200px",
                    LocalRender = true,
                    LocalData = _headers.Where(x => x.FieldName != IdField)
                        .Cast<object>().ToList(),
                    LocalHeader = new List<GridPolicy>
                    {
                        new GridPolicy
                        {
                            EntityId = _gridPolicyId,
                            FieldName = "ShortDesc",
                            ShortDesc = "Tên cột",
                            Active = true,
                        }
                    },
                    Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
                },
                new GridPolicy
                {
                    Id = 2,
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.CompareOperatorId),
                    ShortDesc = "Toán tử",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatCell = "{Description}",
                    Active = true,
                    Editable = true,
                    MinWidth = "150px",
                    LocalRender = true,
                    LocalData = IEnumerableExtensions.ToEntity<AdvSearchOperation>(),
                    LocalHeader = new List<GridPolicy>
                    {
                        new GridPolicy
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Name),
                            ShortDesc = "Toán tử",
                            Active = true,
                        },
                        new GridPolicy
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Description),
                            ShortDesc = "Ký hiệu",
                            Active = true,
                        }
                    },
                    Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
                },
                new GridPolicy
                {
                    Id = 3,
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.Value),
                    ShortDesc = "Giá trị",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Input",
                    Active = true,
                    Editable = true,
                    MinWidth = "450px",
                    Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
                },
                new GridPolicy
                {
                    Id = 2,
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.LogicOperatorId),
                    ShortDesc = "Kết hợp",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatCell = "{Description}",
                    Active = true,
                    Editable = true,
                    DefaultVal = "0",
                    LocalRender = true,
                    LocalData = IEnumerableExtensions.ToEntity<LogicOperation>(),
                    LocalHeader = new List<GridPolicy>
                    {
                        new GridPolicy
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Name),
                            ShortDesc = "Kết hợp",
                            Active = true,
                        },
                        new GridPolicy
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Description),
                            ShortDesc = "Miêu tả",
                            Active = true,
                        },
                    }
                },
            };
            _filterGrid.GuiInfo.LocalData = AdvSearchEntity.Conditions.Cast<object>().ToList();
            _filterGrid.ParentElement = section.Element;
            section.AddChild(_filterGrid);
            _filterGrid.Element.AddEventListener(EventType.KeyDown, ToggleIndent);
        }

        public void FilterDomLoaded()
        {
            _filterGrid.MainSection.Children.ForEach(x =>
            {
                var condition = x.Entity.As<FieldCondition>();
                _ = FieldId_Changed(condition, condition.Field);
            });
        }

        private EnumerableInstance<GridPolicy> HeaderForAdvSearch()
        {
            return ParentListView.Header
                .Where(x => x.Id > 0 && !x.ShortDesc.IsNullOrWhiteSpace() && x.Active && !x.Hidden);
        }

        private void AddOrderByGrid(Section section)
        {
            _orderByGrid = new GridView(new Component
            {
                FieldName = nameof(AdvSearchVM.OrderBy),
                Column = 4,
                ReferenceId = _orderById,
                RefName = nameof(Entity),
                Reference = new Entity { Name = nameof(OrderBy), Namespace = typeof(OrderBy).Namespace + "." },
                CanAdd = true,
                IgnoreConfirmHardDelete = true,
                LocalRender = true,
            });
            _orderByGrid.OnDeleteConfirmed += () =>
            {
                _orderByGrid.GetSelectedRows().ForEach(_orderByGrid.RowData.Remove);
            };
            _orderByGrid.GuiInfo.LocalHeader = new List<GridPolicy>
            {
                new GridPolicy
                {
                    Id = 1,
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.FieldId),
                    Events = "{'change': 'FieldId_Changed'}",
                    ShortDesc = "Tên cột",
                    ReferenceId = _gridPolicyId,
                    RefName = nameof(GridPolicy),
                    FormatCell = "{ShortDesc}",
                    Active = true,
                    Editable = true,
                    ComponentType = "Dropdown",
                    MinWidth = "100px",
                    MaxWidth = "200px",
                    LocalData = _headers
                        .Cast<object>().ToList(),
                    LocalRender = true,
                    LocalHeader = new List<GridPolicy>
                    {
                        new GridPolicy
                        {
                            EntityId = _gridPolicyId,
                            FieldName = "ShortDesc",
                            ShortDesc = "Tên cột",
                            Active = true,
                        }
                    },
                },
                new GridPolicy
                {
                    Id = 2,
                    EntityId = _orderById,
                    FieldName = nameof(OrderBy.OrderbyOptionId),
                    ShortDesc = "Thứ tự",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatCell = "{Description}",
                    Active = true,
                    Editable = true,
                    MinWidth = "100px",
                    MaxWidth = "120px",
                    LocalData = IEnumerableExtensions.ToEntity<OrderbyOption>(),
                    LocalHeader = new List<GridPolicy>
                    {
                        new GridPolicy
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Name),
                            ShortDesc = "Thứ tự",
                            Active = true,
                        },
                    },
                    LocalRender = true,
                },
            };
            _orderByGrid.GuiInfo.LocalData = AdvSearchEntity.OrderBy.Cast<object>().ToList();
            _orderByGrid.ParentElement = section.Element;
            section.AddChild(_orderByGrid);
        }

        private void ToggleIndent(Event e)
        {
            var keyCode = e.KeyCodeEnum();
            if (keyCode != KeyCodeEnum.Tab)
            {
                return;
            }

            e.PreventDefault();
            var reducing = e.ShiftKey();
            var selectedRows = _filterGrid.GetSelectedRows();
            var idMap = selectedRows.ToDictionary(x => x[IdField].As<int>());
            _filterGrid.RowAction(x => idMap.ContainsKey(x.Entity[IdField].As<int>()), x =>
            {
                var fieldCondition = x.Entity as FieldCondition;
                fieldCondition.Level += (reducing ? -1 : 1);
                x.Element.QuerySelectorAll("td").Cast<HTMLElement>().ForEach(td => td.Style.PaddingLeft = fieldCondition.Level + "rem");
            });
        }

        public override void DirtyCheckAndCancel()
        {
            base.Dispose();
        }

        public async Task ApplyFilter()
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return;
            }
            var query = CalcAdvSearchQuery();
            await ParentListView.ReloadData(query);
        }

        public string CalcAdvSearchQuery()
        {
            var query = ParentListView.FormattedDataSource;
            query = query.Replace(new RegExp(@"(Active eq true (and|or)?)|( (and|or)?Active eq true^)"), "");
            if (AdvSearchEntity.ActiveState == ActiveStateEnum.Yes)
            {
                query = OdataExt.AppendClause(query, " and Active eq true");
            }
            else if (AdvSearchEntity.ActiveState == ActiveStateEnum.No)
            {
                query = OdataExt.AppendClause(query, " and Active eq false");
            }

            var originFilter = OdataExt.GetClausePart(query);
            var conditions = AdvSearchEntity.Conditions.Select((x, index) => new
            {
                Term = GetSearchValue(x),
                Operator = GetLogicOp(x) ?? " and "
            }).Where(x => x.Term.HasAnyChar()).ToList();
            var filterPart = conditions.Select((x, index) => index < conditions.Count - 1 ? x.Term + x.Operator : x.Term).Combine(string.Empty);
            if (!filterPart.IsNullOrWhiteSpace())
            {
                query = OdataExt.ApplyClause(query, $"({originFilter}) and ({filterPart})");
            }
            var orderbyList = AdvSearchEntity.OrderBy.Select(orderby => $"{orderby.Field.FieldName} {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
            var orderByPart = string.Join(",", orderbyList);
            if (!orderByPart.IsNullOrWhiteSpace())
            {
                query = OdataExt.ApplyClause(query, string.Join(",", orderbyList), OdataExt.OrderByKeyword);
            }

            return query;
        }

        private string GetSearchValue(FieldCondition condition)
        {
            bool ignoreSearch = false;
            var value = condition.Value?.ToString();
            if (value.IsNullOrWhiteSpace() && condition.CompareOperatorId != AdvSearchOperation.EqualNull && condition.CompareOperatorId != AdvSearchOperation.NotEqualNull)
            {
                return null;
            }
            var fieldType = _entityType.GetProperty(condition.Field.FieldName).PropertyType;
            if (fieldType.IsDate())
            {
                if (!value.IsNullOrWhiteSpace())
                {
                    try
                    {
                        var dateTime = DateTime.ParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToISOFormat();
                        value = $"cast({dateTime},Edm.DateTimeOffset)";
                    }
                    catch
                    {
                        var dateTime = DateTime.ParseExact(value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToISOFormat();
                        value = $"cast({dateTime},Edm.DateTimeOffset)";
                    }
                }
            }
            else if (fieldType.IsInt32() && condition.Field.FieldName.EndsWith(IdField))
            {
                if (value != null)
                {
                    var list = value.Split(",").Select(x => x.TryParseInt()).Where(x => x != null).Cast<int>();
                    value = string.Join(",", list);
                }
            }
            else if (fieldType.IsNumber())
            {
                value = $"{value}";
            }
            else if (fieldType.IsBool())
            {
                var tryParsed = Enum.TryParse<ActiveStateEnum>(value, out var state);
                if (tryParsed && state == ActiveStateEnum.Yes)
                {
                    value = "true";
                }
                else if (state == ActiveStateEnum.No)
                {
                    value = "false";
                }
            }
            else
            {
                value = $"'{value.EncodeSpecialChar()}'";
            }

            var func = AdvOptionExt.OperationToOdata[condition.CompareOperatorId.Value];
            var formattedFunc = ignoreSearch ? string.Empty : string.Format(func, condition.Field?.FieldName, value);

            return formattedFunc;
        }

        private static string GetLogicOp(FieldCondition condition)
        {
            if (condition.LogicOperatorId is null)
            {
                return null;
            }

            return condition.LogicOperatorId == LogicOperation.And ? " and " : " or ";
        }

        private async Task FieldId_Changed(FieldCondition condition, GridPolicy field)
        {
            if (condition is null || field is null)
            {
                return;
            }
            condition.Field = field;
            var fieldType = _entityType.GetProperty(field.FieldName).PropertyType;
            var cell = _filterGrid.FirstOrDefault(x => x.GuiInfo != null && x.Entity == condition && x.GuiInfo.FieldName == nameof(FieldCondition.Value));
            var compareCell = _filterGrid.FirstOrDefault(x => x.GuiInfo != null && x.Entity == condition
                && x.GuiInfo.FieldName == nameof(FieldCondition.CompareOperatorId)) as SearchEntry;
            if (cell is null)
            {
                return;
            }

            var parentCellElement = cell.ParentElement;
            var parentCell = cell.Parent;
            cell.Dispose();
            var comInfo = field.MapToComponent();
            comInfo.FieldName = nameof(FieldCondition.Value);
            EditableComponent component = null;
            var searchByIdList = new string[] { nameof(ComponentTypeTypeEnum.Dropdown), nameof(ComponentTypeTypeEnum.SearchEntry), nameof(ComponentTypeTypeEnum.MultipleSearchEntry) };
            var isSearchId = searchByIdList.Contains(field.ComponentType)
                || fieldType.IsInt32() && (field.FieldName?.EndsWith(IdField) == true || field.FieldName == nameof(Component.InsertedBy) || field.FieldName == nameof(Component.UpdatedBy));
            if (fieldType.IsDate())
            {
                component = SetSearchDateTime(compareCell, comInfo);
                condition.Value = DateTime.Now.ToString();
            }
            else if (isSearchId)
            {
                component = await SetSearchId(field, compareCell, comInfo, component);
                condition.Value = string.Empty;
            }
            else if (fieldType.IsBool())
            {
                component = SetSearchBool(compareCell, comInfo);
                condition.Value = ((int)ActiveStateEnum.All).ToString();
            }
            else if (fieldType.IsNumber())
            {
                component = SetSearchDecimal(compareCell, comInfo);
                condition.Value = 0.ToString();
            }
            else
            {
                component = SetSearchString(compareCell, comInfo);
            }
            condition.LogicOperatorId = condition.LogicOperatorId ?? LogicOperation.And;
            _filterGrid.FirstOrDefault(x => x.GuiInfo != null && x.Entity == condition
                && x.GuiInfo.FieldName == nameof(FieldCondition.LogicOperatorId))?.UpdateView();
            condition.CompareOperatorId = (AdvSearchOperation?)compareCell.GuiInfo.LocalData.Cast<Entity>().FirstOrDefault()?.Id;
            compareCell.Value = (int?)condition.CompareOperatorId;
            compareCell.UpdateView();
            component.Entity = condition;
            component.SetValue(nameof(condition.Value), condition.Value);
            component.Parent = parentCell;
            parentCell.Children.Insert(2, component);
            component.ParentElement = parentCellElement;
            component.Render();
        }

        private static EditableComponent SetSearchString(SearchEntry compareCell, Component comInfo)
        {
            EditableComponent component;
            comInfo.ComponentType = nameof(Textbox);
            component = new Textbox(comInfo);
            compareCell.GuiInfo.LocalData = OperatorFactory(ComponentTypeTypeEnum.Textbox).Cast<object>().ToList();
            return component;
        }

        public static IEnumerable<Entity> OperatorFactory(ComponentTypeTypeEnum componentType)
        {
            var entities = IEnumerableExtensions.ToEntity<AdvSearchOperation>().Cast<Entity>();
            switch (componentType)
            {
                case ComponentTypeTypeEnum.Textbox:
                    return entities.Where(x => (x.Id >= (int)AdvSearchOperation.Contains && x.Id < (int)AdvSearchOperation.In)
                        || x.Id == (int)AdvSearchOperation.Equal || x.Id == (int)AdvSearchOperation.NotEqual)
                        .OrderByDescending(x => x.Id == (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Datepicker:
                    return entities.Where(x => x.Id < (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Number:
                    return entities.Where(x => x.Id < (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Checkbox:
                    return entities.Where(x => x.Id == (int)AdvSearchOperation.Equal);
                case ComponentTypeTypeEnum.SearchEntry:
                    return entities.Where(x => x.Id == (int)AdvSearchOperation.In || x.Id == (int)AdvSearchOperation.NotIn);
            }
            return null;
        }

        private static EditableComponent SetSearchDecimal(SearchEntry compareCell, Component comInfo)
        {
            EditableComponent component;
            comInfo.ComponentType = nameof(Number);
            component = new Number(comInfo);
            compareCell.GuiInfo.LocalData = OperatorFactory(ComponentTypeTypeEnum.Number).Cast<object>().ToList();
            return component;
        }

        private static EditableComponent SetSearchBool(SearchEntry compareCell, Component comInfo)
        {
            EditableComponent component;
            comInfo.FormatData = "{" + nameof(Core.Models.Entity.Description) + "}";
            comInfo.ComponentType = nameof(SearchEntry);
            comInfo.LocalRender = true;
            comInfo.LocalData = IEnumerableExtensions.ToEntity<ActiveStateEnum>();
            comInfo.LocalHeader = new List<GridPolicy>
                {
                    new GridPolicy
                    {
                        FieldName = nameof(Core.Models.Entity.Name),
                        ShortDesc = "Trạng thái",
                        Active = true,
                    },
                    new GridPolicy
                    {
                        FieldName = nameof(Core.Models.Entity.Description),
                        ShortDesc = "Miêu tả",
                        Active = true,
                    }
                };
            component = new SearchEntry(comInfo);
            compareCell.GuiInfo.LocalData = OperatorFactory(ComponentTypeTypeEnum.Checkbox).Cast<object>().ToList();
            return component;
        }

        private async Task<EditableComponent> SetSearchId(GridPolicy field, SearchEntry compareCell, Component comInfo, EditableComponent component)
        {
            comInfo.ComponentType = nameof(MultipleSearchEntry);
            var refId = field.ReferenceId ?? ParentListView.GuiInfo.ReferenceId ?? 0;
            comInfo.ReferenceId = refId;
            comInfo.Reference = Utils.GetEntity(refId);
            comInfo.DataSourceFilter = field.DataSource;
            comInfo.LocalHeader = await new Client(nameof(GridPolicy), typeof(User).Namespace).GetRawList<GridPolicy>(
                $"?$filter=Active eq true and FeatureId eq null and EntityId eq {field.ReferenceId ?? field.EntityId}");
            compareCell.GuiInfo.LocalData = OperatorFactory(ComponentTypeTypeEnum.SearchEntry).Cast<object>().ToList();
            component = new MultipleSearchEntry(comInfo);
            component.EntityType = typeof(FieldCondition);
            compareCell.Value = (int)AdvSearchOperation.In;
            return component;
        }

        private static EditableComponent SetSearchDateTime(SearchEntry compareCell, Component comInfo)
        {
            EditableComponent component;
            comInfo.ComponentType = nameof(Datepicker);
            comInfo.Precision = 7; // add time picker
            component = new Datepicker(comInfo);
            compareCell.GuiInfo.LocalData =
                IEnumerableExtensions.ToEntity<AdvSearchOperation>()
                .Cast<Entity>().Where(x => x.Id < (int)AdvSearchOperation.Contains)
                .Cast<object>().ToList();
            return component;
        }
    }
}