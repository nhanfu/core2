using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class FileUploadGrid : GridView
    {
        public FileUploadGrid(Component ui) : base(ui)
        {
            GuiInfo = ui;
            ui.LocalHeader = new List<Component>
            {
                new Component
                {
                    FieldName = nameof(FileUpload.FileName),
                    Editable = true,
                    ShortDesc = "Name",
                    ComponentType = "Input",
                    Order = 2
                },
                new Component
                {
                    FieldName = nameof(FileUpload.FilePath),
                    Editable = true,
                    Active = true,
                    ComponentType = nameof(ImageUploader),
                    DataSourceFilter= ui.DataSourceFilter.IsNullOrEmpty() ?  "*.*" : ui.DataSourceFilter,
                    ShortDesc = "File",
                    IsRealtime = GuiInfo.IsRealtime,
                    Precision = 1,
                    Order = 1
                },
                new Component
                {
                    FieldName = nameof(FileUpload.InsertedBy),
                    Active = true,
                    RefName = nameof(User),
                    ReferenceId = Utils.GetEntity(nameof(User))?.Id,
                    FormatData = "{" + nameof(User.FullName) + "}",
                    ComponentType = "Label",
                    ShortDesc = "Created by",
                    Order = 3
                },
                new Component
                {
                    FieldName = nameof(FileUpload.InsertedDate),
                    Active = true,
                    ComponentType = "Label",
                    ShortDesc = "Created date",
                    Order = 4
                },
                new Component
                {
                    FieldName = nameof(FileUpload.UpdatedBy),
                    Active = true,
                    RefName = nameof(User),
                    ReferenceId = Utils.GetEntity(nameof(User))?.Id,
                    FormatData = "{" + nameof(User.FullName) + "}",
                    ComponentType = "Label",
                    ShortDesc = "Updated by",
                    Order = 5
                },
                new Component
                {
                    FieldName = nameof(FileUpload.UpdatedDate),
                    Active = true,
                    ComponentType = "Label",
                    ShortDesc = "Updated date",
                    Order = 6
                },
            };
            ui.Row = int.MaxValue;
            DOMContentLoaded += SetEntityPath;
        }

        public override void Render()
        {
            RowData._data = new List<object>();
            GuiInfo.DataSourceFilter = $"?$filter=EntityName eq '{GuiInfo.IdField}' and RecordId eq {Entity[IdField]} and FieldName eq '{GuiInfo.FieldName}' and SectionId eq {GuiInfo.ComponentGroupId} and RecordId ne 0";
            DataSourceFilter = GuiInfo.DataSourceFilter;
            base.Render();
            Paginator.Show = false;
        }

        internal override void AddSections()
        {
            if (HeaderSection?.Element != null)
            {
                return;
            }
            base.AddSections();
            Html.Take(Element).Div.ClassName("toolbar")
                .Button.ClassName("btn fa fa-download").Title("Download all").Event(EventType.Click, DownloadAll).End.Render();
            Element.Prepend(Html.Context);
        }

        private void DownloadAll()
        {
            RowData.Data.Select(x => x.CastProp<FileUpload>()).ForEach(x => Client.Download(x.FilePath));
        }


        internal override async Task RowChangeHandler(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            if (rowData[nameof(FileUpload.Id)].As<int?>() <= 0)
            {
                rowData[nameof(FileUpload.InsertedBy)] = Client.Token.UserId;
                rowData[nameof(FileUpload.InsertedDate)] = DateTime.Now;
            }
            else
            {
                rowData[nameof(FileUpload.UpdatedBy)] = Client.Token.UserId;
                rowData[nameof(FileUpload.UpdatedDate)] = DateTime.Now;
            }
            rowData[nameof(FileUpload.EntityName)] = GuiInfo.IdField;
            rowData[nameof(FileUpload.RecordId)] = EntityId;
            rowData[nameof(FileUpload.SectionId)] = GuiInfo.ComponentGroupId;
            rowData[nameof(FileUpload.FieldName)] = GuiInfo.FieldName;
            rowData[nameof(FileUpload.FileName)] = ImageUploader.RemoveGuid(rowData[nameof(FileUpload.FilePath)] as string);
            await RowChangeHandlerGrid(rowData, rowSection, observableArgs);
            rowSection.UpdateView(true);
            SetEntityPath();
            if (GuiInfo.IsRealtime && EntityId != null)
            {
                await RealtimeUpdateAsync(rowSection, observableArgs);
            }
            Dirty = true;
        }

        public override Task<List<object>> BatchUpdate(bool updateView = false)
        {
            UpdatedRows.ForEach(row => row[nameof(FileUpload.RecordId)] = EntityId);
            return base.BatchUpdate(updateView);
        }

        private void SetEntityPath()
        {
            Entity[GuiInfo.FieldName] = AllListViewItem.Combine(x => x.Entity[nameof(FileUpload.FilePath)] as string, ImageUploader.PathSeparator);
        }

        protected override void SetRowData(List<object> listData)
        {
            if (listData == null)
            {
                listData = new List<object>();
            }
            listData = MergePaths(listData);
            RowData._data = new List<object>();
            if (listData.HasElement())
            {
                listData.ForEach(RowData._data.Add);
            }
            RenderContent();
            SetEntityPath();
        }

        private List<object> MergePaths(List<object> listData)
        {
            var pathCombined = Entity[GuiInfo.FieldName] as string;
            if (pathCombined.IsNullOrWhiteSpace())
            {
                return listData;
            }
            var separatedFiles = listData.ToDictionaryDistinct(x => x[nameof(FileUpload.FilePath)].As<string>());
            var existPaths = pathCombined.Split(ImageUploader.PathSeparator);
            var existFiles = existPaths.Select(x =>
            {
                var metaData = separatedFiles.GetValueOrDefault(x) as dynamic;
                return new FileUpload
                {
                    EntityName = GuiInfo.IdField,
                    RecordId = EntityId,
                    SectionId = GuiInfo.ComponentGroupId,
                    FieldName = GuiInfo.FieldName,
                    FileName = ImageUploader.RemoveGuid(x),
                    FilePath = x,
                    Id = metaData?.Id ?? 0,
                    InsertedBy = metaData?.InsertedBy ?? 1,
                    InsertedDate = metaData?.InsertedDate ?? DateTime.Now,
                    UpdatedBy = metaData?.UpdatedBy,
                    UpdatedDate = metaData?.UpdatedDate,
                };
            }).ToArray();
            listData = existFiles.Union(listData.Select(x => x.CastProp<FileUpload>())).DistinctBy(x => x.FilePath).Cast<object>().ToList();
            return listData;
        }

        public override async Task<IEnumerable<object>> HardDeleteConfirmed(List<object> deleted)
        {
            var deleted1 = await base.HardDeleteConfirmed(deleted);
            Dirty = true;
            SetEntityPath();
            return deleted1;
        }
    }
}
