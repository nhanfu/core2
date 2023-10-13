using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    internal class AdvancedSearch : PopupEditor
    {
        private GridView _filterGrid;
        private GridView _orderByGrid;
        private SearchEntry _activeState;
        private readonly Type _entityType;
        private List<Component> _headers;

        public ListView ParentListView;
        public string ModelNameSpace { get; }
        private AdvSearchVM AdvSearchEntity => Entity as AdvSearchVM;
        public string _orderById = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(OrderBy)).Id;
        public string _fieldConditionId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(FieldCondition)).Id;
        public string _ComponentId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(Component)).Id;
        public string _entityId = Client.Entities.Values.FirstOrDefault(x => x.Name == nameof(Entity)).Id;

        public AdvancedSearch(ListView parent) : base(nameof(Component))
        {
            Name = "AdvancedSearch";
            Title = "Tìm kiếm nâng cao";
            Icon = "fa fa-search-plus";
            ParentListView = parent;
            _entityType = Type.GetType((ParentListView.GuiInfo.Reference?.Namespace ?? Client.ModelNamespace) + ParentListView.GuiInfo.RefName);
            DOMContentLoaded += LocalRender;
        }

        public void LocalRender()
        {
            _headers = ParentListView.Header
                .Where(x => x.Id != null && !x.ShortDesc.IsNullOrWhiteSpace() && x.Active && !x.Hidden).ToList();
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
                ReferenceId = _entityId.ToString(),
                RefName = nameof(Entity),
                Reference = new Entity { Name = nameof(Entity) },
                Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
            });
            _activeState.GuiInfo.LocalData = IEnumerableExtensions.ToEntity<ActiveStateEnum>();
            _activeState.GuiInfo.LocalHeader = new List<Component>
            {
                new Component
                {
                    FieldName = nameof(Core.Models.Entity.Name),
                    ShortDesc = "Trạng thái",
                    Active = true,
                },
                new Component
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
                ReferenceId = _fieldConditionId.ToString(),
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
            _filterGrid.GuiInfo.LocalHeader = new List<Component>
            {
                new Component
                {
                    Id = 1 .ToString(),
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.FieldId),
                    Events = "{'change': 'FieldId_Changed'}",
                    ShortDesc = "Tên cột",
                    ReferenceId = _ComponentId,
                    RefName = nameof(Component),
                    FormatData = "{ShortDesc}",
                    Active = true,
                    Editable = true,
                    ComponentType = "Dropdown",
                    MinWidth = "100px",
                    MaxWidth = "200px",
                    LocalRender = true,
                    LocalData = _headers.Where(x => x.FieldName != IdField)
                        .Cast<object>().ToList(),
                    LocalHeader = new List<Component>
                    {
                        new Component
                        {
                            EntityId = _ComponentId,
                            FieldName = "ShortDesc",
                            ShortDesc = "Tên cột",
                            Active = true,
                        }
                    },
                    Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
                },
                new Component
                {
                    Id = 2 .ToString(),
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.CompareOperatorId),
                    ShortDesc = "Toán tử",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatData = "{Description}",
                    Active = true,
                    Editable = true,
                    MinWidth = "150px",
                    LocalRender = true,
                    LocalData = IEnumerableExtensions.ToEntity<AdvSearchOperation>(),
                    LocalHeader = new List<Component>
                    {
                        new Component
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Name),
                            ShortDesc = "Toán tử",
                            Active = true,
                        },
                        new Component
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Description),
                            ShortDesc = "Ký hiệu",
                            Active = true,
                        }
                    },
                    Validation = "[{\"Rule\": \"required\", \"Message\": \"{0} is required\"}]",
                },
                new Component
                {
                    Id = 3 .ToString(),
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
                new Component
                {
                    Id = 2 .ToString(),
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.LogicOperatorId),
                    ShortDesc = "Kết hợp",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatData = "{Description}",
                    Active = true,
                    Editable = true,
                    DefaultVal = "0",
                    LocalRender = true,
                    LocalData = IEnumerableExtensions.ToEntity<LogicOperation>(),
                    LocalHeader = new List<Component>
                    {
                        new Component
                        {
                            EntityId = _entityId,
                            FieldName = nameof(Core.Models.Entity.Name),
                            ShortDesc = "Kết hợp",
                            Active = true,
                        },
                        new Component
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

        private EnumerableInstance<Component> HeaderForAdvSearch()
        {
            return ParentListView.Header
                .Where(x => x.Id != null && !x.ShortDesc.IsNullOrWhiteSpace() && x.Active && !x.Hidden);
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
            _orderByGrid.GuiInfo.LocalHeader = new List<Component>
            {
                new Component
                {
                    Id = 1 .ToString(),
                    EntityId = _fieldConditionId,
                    FieldName = nameof(FieldCondition.FieldId),
                    Events = "{'change': 'FieldId_Changed'}",
                    ShortDesc = "Tên cột",
                    ReferenceId = _ComponentId,
                    RefName = nameof(Component),
                    FormatData = "{ShortDesc}",
                    Active = true,
                    Editable = true,
                    ComponentType = "Dropdown",
                    MinWidth = "100px",
                    MaxWidth = "200px",
                    LocalData = _headers
                        .Cast<object>().ToList(),
                    LocalRender = true,
                    LocalHeader = new List<Component>
                    {
                        new Component
                        {
                            EntityId = _ComponentId,
                            FieldName = "ShortDesc",
                            ShortDesc = "Tên cột",
                            Active = true,
                        }
                    },
                },
                new Component
                {
                    Id = 2 .ToString(),
                    EntityId = _orderById,
                    FieldName = nameof(OrderBy.OrderbyOptionId),
                    ShortDesc = "Thứ tự",
                    ReferenceId = _entityId,
                    RefName = nameof(Entity),
                    ComponentType = "Dropdown",
                    FormatData = "{Description}",
                    Active = true,
                    Editable = true,
                    MinWidth = "100px",
                    MaxWidth = "120px",
                    LocalData = IEnumerableExtensions.ToEntity<OrderbyOption>(),
                    LocalHeader = new List<Component>
                    {
                        new Component
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
            if (AdvSearchEntity.ActiveState == ActiveStateEnum.Yes && !query.Contains("Active eq false"))
            {
                query = OdataExt.AppendClause(query, " and Active eq true");
            }
            else if (AdvSearchEntity.ActiveState == ActiveStateEnum.No && !query.Contains("Active eq true"))
            {
                query = OdataExt.AppendClause(query, " and Active eq false");
            }
            var originFilter = OdataExt.GetClausePart(query);
            var conditions = AdvSearchEntity.Conditions.Select((x, index) => new
            {
                Term = GetSearchValue(x),
                Operator = GetLogicOp(x) ?? " and ",
                Group = x.Group
            }).Where(x => x.Term.HasAnyChar()).ToList();

            var filterPart = conditions.Where(x => !x.Group).Select((x, index) => index < conditions.Count - 1 ? x.Term + x.Operator : x.Term).Combine(string.Empty);
            var filterPartGroup = conditions.Where(x => x.Group).Select((x, index) => index < conditions.Count - 1 ? x.Term + x.Operator : x.Term).Combine(string.Empty);
            var qr = $"({originFilter})";
            if (!filterPart.IsNullOrWhiteSpace())
            {
                qr += $" and ({filterPart})".Replace("and )", ")").Replace("or )", ")");
            }
            if (!filterPartGroup.IsNullOrWhiteSpace())
            {
                qr += $" and ({filterPartGroup})".Replace("and )", ")").Replace("or )", ")");
            }
            if (!filterPart.IsNullOrWhiteSpace() || !filterPartGroup.IsNullOrWhiteSpace())
            {
                query = OdataExt.ApplyClause(query, qr);
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
            if (value is null && condition.CompareOperatorId != AdvSearchOperation.EqualNull && condition.CompareOperatorId != AdvSearchOperation.NotEqualNull)
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

        private async Task FieldId_Changed(FieldCondition condition, Component field)
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
            field.FieldName = nameof(FieldCondition.Value);
            EditableComponent component = null;
            var searchByIdList = new string[] { nameof(ComponentTypeTypeEnum.Dropdown), nameof(ComponentTypeTypeEnum.SearchEntry), nameof(ComponentTypeTypeEnum.MultipleSearchEntry) };
            var isSearchId = searchByIdList.Contains(field.ComponentType)
                || fieldType.IsInt32() && (field.FieldName?.EndsWith(IdField) == true || field.FieldName == nameof(Component.InsertedBy) || field.FieldName == nameof(Component.UpdatedBy));
            if (fieldType.IsDate())
            {
                component = SetSearchDateTime(compareCell, field);
                condition.Value = DateTime.Now.ToString();
            }
            else if (isSearchId)
            {
                component = await SetSearchId(field, compareCell, field, component);
                condition.Value = string.Empty;
            }
            else if (fieldType.IsBool())
            {
                component = SetSearchBool(compareCell, field);
                condition.Value = ((int)ActiveStateEnum.All).ToString();
            }
            else if (fieldType.IsNumber())
            {
                component = SetSearchDecimal(compareCell, field);
                condition.Value = 0.ToString();
            }
            else
            {
                component = SetSearchString(compareCell, field);
            }
            condition.LogicOperatorId = condition.LogicOperatorId ?? LogicOperation.And;
            _filterGrid.FirstOrDefault(x => x.GuiInfo != null && x.Entity == condition
                && x.GuiInfo.FieldName == nameof(FieldCondition.LogicOperatorId))?.UpdateView();
            condition.CompareOperatorId = (AdvSearchOperation?)compareCell.GuiInfo.LocalData.Cast<Entity>().FirstOrDefault()?.Id?.TryParseInt();
            compareCell.Value = ((int?)condition.CompareOperatorId)?.ToString();
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
                    return entities.Where(x => (x.Id.TryParseInt() >= (int)AdvSearchOperation.Contains && x.Id.TryParseInt() < (int)AdvSearchOperation.In)
                        || x.Id.TryParseInt() == (int)AdvSearchOperation.Equal || x.Id.TryParseInt() == (int)AdvSearchOperation.NotEqual)
                        .OrderByDescending(x => x.Id.TryParseInt() == (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Datepicker:
                    return entities.Where(x => x.Id.TryParseInt() < (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Number:
                    return entities.Where(x => x.Id.TryParseInt() < (int)AdvSearchOperation.Contains);
                case ComponentTypeTypeEnum.Checkbox:
                    return entities.Where(x => x.Id.TryParseInt() == (int)AdvSearchOperation.Equal);
                case ComponentTypeTypeEnum.SearchEntry:
                    return entities.Where(x => x.Id.TryParseInt() == (int)AdvSearchOperation.In || x.Id.TryParseInt() == (int)AdvSearchOperation.NotIn);
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
            comInfo.LocalHeader = new List<Component>
                {
                    new Component
                    {
                        FieldName = nameof(Core.Models.Entity.Name),
                        ShortDesc = "Trạng thái",
                        Active = true,
                    },
                    new Component
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

        private async Task<EditableComponent> SetSearchId(Component field, SearchEntry compareCell, Component comInfo, EditableComponent component)
        {
            comInfo.ComponentType = nameof(MultipleSearchEntry);
            var refId = field.ReferenceId ?? ParentListView.GuiInfo.ReferenceId;
            comInfo.ReferenceId = refId;
            comInfo.Reference = Utils.GetEntity(refId);
            comInfo.DataSourceFilter = field.DataSourceFilter;
            comInfo.LocalHeader = await new Client(nameof(Component), typeof(User).Namespace).GetRawList<Component>(
                $"?$filter=Active eq true and FeatureId eq null and EntityId eq '{field.ReferenceId ?? field.EntityId}'");
            compareCell.GuiInfo.LocalData = OperatorFactory(ComponentTypeTypeEnum.SearchEntry).Cast<object>().ToList();
            component = new MultipleSearchEntry(comInfo);
            component.EntityType = typeof(FieldCondition);
            compareCell.Value = ((int)AdvSearchOperation.In).ToString();
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
                .Cast<Entity>().Where(x => int.Parse(x.Id) < (int)AdvSearchOperation.Contains)
                .Cast<object>().ToList();
            return component;
        }
    }
}