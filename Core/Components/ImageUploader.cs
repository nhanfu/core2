using System.Net;
using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Linq;
using System.Threading.Tasks;
using PathIO = System.IO.Path;
using Core.ViewModels;
using System.Collections.Generic;

namespace Core.Components
{
    public class ImageUploader : EditableComponent
    {
        protected string _path;
        public virtual string Path
        {
            get => _path;
            set
            {
                ParentElement.QuerySelectorAll(".gallery").Cast<HTMLElement>().ForEach(x => x.Remove());
                _path = value;
                if (Entity != null)
                {
                    Entity.SetComplexPropValue(FieldName, _path);
                }

                if (_path.IsNullOrWhiteSpace())
                {
                    return;
                }

                var updatedImages = _path.Split(PathSeparator).ToList();
                if (updatedImages.Nothing())
                {
                    return;
                }

                updatedImages.ForEach(x =>
                {
                    RenderFileThumb(x);
                });
            }
        }
        public const string PathSeparator = "    ";
        private const string PNGUrlPrefix = "data:image/png;base64,";
        private const string JpegUrlPrefix = "data:image/jpeg;base64,";
        internal const int GuidLength = 36;
        private HTMLInputElement _input;
        private static HTMLElement _preview;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
        private bool _disabledDelete;
        private HTMLDivElement _gallerys;

        public event Action FileUploaded;
        public string DataSourceFilter { get; set; }
        private string[] _imageSources => _path?.Split(PathSeparator);

        public ImageUploader(Component ui) : base(ui)
        {
            GuiInfo = ui;
            DataSourceFilter = GuiInfo.DataSourceFilter ?? "image/*";
        }

        public override void Render()
        {
            _path = Entity?.GetPropValue(FieldName)?.ToString();
            var paths = _path?.Split(PathSeparator).ToList();
            RenderUploadForm();
            Path = _path;
            DOMContentLoaded?.Invoke();
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
        }

        private HTMLElement RenderFileThumb(string path)
        {
            Html.Take(_gallerys).Div.ClassName("gallery");
            var thumbText = RemoveGuid(path);
            var isImage = PathIO.IsImage(path);
            if (isImage)
            {
                Html.Instance.Div.ClassName("file-upload").Img.ClassName("image").Style(GuiInfo.ChildStyle).Src((path.Contains("http") ? "" : Client.Origin) + path.DecodeSpecialChar()).Render();
            }
            else
            {
                Html.Instance.Span.ClassName(thumbText.Contains("pdf") ? "fal fa-file-pdf" : "fal fa-file").Title(thumbText.DecodeSpecialChar())
                    .Style(GuiInfo.ChildStyle).Href((path.Contains("http") ? "" : Client.Origin) + path.DecodeSpecialChar()).Render();
            }
            Html.Instance.End.Render();
            Html.Instance.Div.ClassName("middle d-flex")
                .Div.ClassName("preview").Event(EventType.Click, Preview, path.DecodeSpecialChar()).I.ClassName("fas fa-eye").EndOf(MVVM.ElementType.div);
            if (!Disabled)
            {
                Html.Instance.Div.ClassName("delete")
                    .Event(EventType.Click, RemoveFile, path).I.ClassName("fas fa-trash-alt").EndOf(MVVM.ElementType.div);
            }
            Html.Instance.End.Div.ClassName("middle-secondery").Style("font-size:8px").Text(thumbText.DecodeSpecialChar()).Render();
            return _gallerys;
        }

        internal static string RemoveGuid(string path)
        {
            string thumbText = path;
            if (path.Length > GuidLength)
            {
                var fileName = PathIO.GetFileNameWithoutExtension(path);
                thumbText = fileName.SubStrIndex(0, fileName.Length - GuidLength) + PathIO.GetExtension(path);
            }

            return thumbText;
        }

        public void SetCanDeleteImage(bool canDelete)
        {
            _disabledDelete = !canDelete;
            if (canDelete)
            {
                UpdateView();
            }
        }

        private void Preview(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return;
            }

            if (!PathIO.IsImage(path))
            {
                Client.Download(path);
                return;
            }

            HTMLImageElement img = null;
            var rotate = 0;
            Html.Take(base.EditForm.Element).Div.ClassName("dark-overlay zoom")
                .Escape((e) => _preview.Remove())
                .Event(EventType.KeyDown, (Event e) =>
                {
                    var keyCode = EventExt.KeyCodeEnum(e);
                    if (keyCode != Enums.KeyCodeEnum.LeftArrow && keyCode != Enums.KeyCodeEnum.RightArrow)
                    {
                        return;
                    }

                    if (keyCode == Enums.KeyCodeEnum.LeftArrow)
                    {
                        path = MoveLeft(path, img);
                    }
                    else
                    {
                        path = MoveRight(path, img);
                    }
                });
            _preview = Html.Context;
            Html.Instance
                    .Img.Src((path.Contains("http") ? "" : Client.Origin) + path);
            img = Html.Context as HTMLImageElement;
            Html.Instance.End
                .Span.ClassName("close").Event(EventType.Click, () => _preview.Remove()).End
                .Div.ClassName("toolbar")
                    .Icon("fa fa-undo ro-left").Event(EventType.Click, () =>
                    {
                        rotate -= 90;
                        img.Style.Transform = $"rotate({rotate}deg)";
                    }).End
                    .Icon("fa fa-cloud-download-alt").Title("Tải xuống").Event(EventType.Click, () =>
                    {
                        var link = Document.CreateElement("a") as HTMLAnchorElement;
                        link.Href = img.Src;
                        link.Download = img.Src.Substring(img.Src.LastIndexOf("/"));
                        Document.Body.AppendChild(link);
                        link.Click();
                        link.Remove();
                    }).End
                    .Icon("fa fa-redo ro-right").Event(EventType.Click, () =>
                    {
                        rotate += 90;
                        img.Style.Transform = $"rotate({rotate}deg)";
                    }).End
                .End
                .Icon("fa fa-chevron-left").Event(EventType.Click, () =>
                {
                    path = MoveLeft(path, img);
                }).End
                .Icon("fa fa-chevron-right").Event(EventType.Click, () =>
                {
                    path = MoveRight(path, img);
                }).End.Render();
            /*@
             zoom();
             */
        }

        private int zoomLevel;
        private int flagZoomIn = 1;
        private int zoomMaxLevel = 3;
        private void ZoomImage(HTMLImageElement img)
        {
            if (flagZoomIn < zoomMaxLevel)
            {
                if (zoomLevel == 0 || zoomLevel < zoomMaxLevel)
                {
                    img.Style.Cursor = "zoom-in";
                    img.Height += 450;
                    img.Width += 400;
                    zoomLevel++;
                    flagZoomIn++;
                }
            }
            else if (flagZoomIn == zoomMaxLevel)
            {
                if (zoomLevel == 1)
                {
                    flagZoomIn = 1;
                }
                img.Style.Cursor = "zoom-out";
                img.Height -= 450;
                img.Width -= 400;
                zoomLevel--;
            }
            //img.Style.Transform = $"scale({zoomLevel})";
        }

        protected void MoveAround(Event e, string path)
        {
            var keyCode = e.KeyCodeEnum();
            if (keyCode != Enums.KeyCodeEnum.LeftArrow && keyCode != Enums.KeyCodeEnum.RightArrow)
            {
                return;
            }

            if (!(e.Target.As<HTMLElement>().FirstElementChild is HTMLImageElement img))
            {
                return;
            }

            if (keyCode == Enums.KeyCodeEnum.LeftArrow)
            {
                MoveLeft(path, img);
            }
            else
            {
                MoveRight(path, img);
            }
        }

        protected string MoveLeft(string path, HTMLImageElement img)
        {
            var index = Array.IndexOf(_imageSources, path);
            if (index == 0)
            {
                index = _imageSources.Length - 1;
            }
            else
            {
                index--;
            }

            img.Src = (path.Contains("http") ? "" : Client.Origin) + _imageSources[index];
            return _imageSources[index];
        }

        protected string MoveRight(string path, HTMLImageElement img)
        {
            var index = Array.IndexOf(_imageSources, path);
            if (index == 0)
            {
                index = _imageSources.Length - 1;
            }
            else
            {
                index--;
            }

            img.Src = (path.Contains("http") ? "" : Client.Origin) + _imageSources[index];
            return _imageSources[index];
        }

        public void OpenFileDialog(Event e)
        {
            if (Disabled)
            {
                return;
            }

            OpenNativeFileDialog(e);
            return;
            /*@
            if (typeof(navigator.camera) === 'undefined')
            {
                this._input.click();
            }
            else
            {
                this.RenderImageSourceChooser();
            }
            */
        }

        private void RenderUploadForm()
        {
            var isMultiple = GuiInfo.Precision == 0;
            Html.Take(ParentElement)
                .ClassName("choose-files")
                .Input.Type("file").Attr("name", "files")
                .ClassName("d-none")
                .Attr("accept", DataSourceFilter)
                .Event(EventType.Change, UploadSelectedImages).Render();
            Element = _input = Html.Context as HTMLInputElement;
            if (isMultiple)
            {
                Html.Instance.Attr("multiple", "multiple");
            }
            Html.Take(ParentElement).Div.ClassName("file-upload")
                .I.ClassName("fal fa-file-alt")
                .Event(EventType.Click, OpenFileDialog).End.Div.ClassName("gallerys").Render();
            _gallerys = Html.Context as HTMLDivElement;
        }

        private void RemoveFile(Event e, string removedPath)
        {
            if (Disabled)
            {
                return;
            }

            e.StopPropagation();
            if (removedPath.IsNullOrEmpty())
            {
                return;
            }
            ConfirmDialog.RenderConfirm($"Bạn chắc chắn muốn xóa {PathIO.GetFileNameWithoutExtension(RemoveGuid(removedPath.DecodeSpecialChar())) + PathIO.GetExtension(RemoveGuid(removedPath.DecodeSpecialChar()))}", async () =>
            {
                var removed = await new Client(nameof(User)) { CustomPrefix = Client.FileFTP }.PostAsync<bool>(removedPath, "DeleteFile");
                var oldVal = _path;
                var newPath = _path.Replace(removedPath, string.Empty)
                    .Replace(PathSeparator + PathSeparator, string.Empty)
                    .Split(PathSeparator).Where(x => x.HasAnyChar()).Distinct().ToList();
                Path = string.Join(PathSeparator, newPath);
                Dirty = true;
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = FieldName, EvType = EventType.Change });
                }
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
            });
        }

        private void UploadSelectedImages(Event e)
        {
            e.PreventDefault();
            if (EditForm.IsLock)
            {
                return;
            }
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            var oldVal = _path;
            var task = UploadAllFiles(files);
            Client.ExecTaskNoResult(task, () =>
            {
                Dirty = true;
                _input.Value = string.Empty;
                UserInput?.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = FieldName, EvType = EventType.Change });
                var dispatch = this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
                Client.ExecTaskNoResult(dispatch);
            });
        }

        public Task<string> UploadBase64Image(string base64Image, string fileName)
        {
            return Client.Instance.SubmitAsync<string>(new XHRWrapper
            {
                Value = base64Image,
                Url = $"/user/image/?name={fileName}",
                IsRawString = true,
                Method = Enums.HttpMethod.POST
            });
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                if (_input != null)
                {
                    _input.Disabled = value;
                }

                base.Disabled = value;
                if (value)
                {
                    ParentElement.SetAttribute("disabled", "");
                }
                else
                {
                    ParentElement.RemoveAttribute("disabled");
                }
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Path = Entity.GetPropValue(FieldName)?.ToString();
            base.UpdateView(force, dirty, componentNames);
        }

        protected Task<string> UploadFile(File file)
        {
            var tcs = new TaskCompletionSource<string>();
            if (GuiInfo.IsRealtime || !file.Type.Match("image.*").HasElement())
            {
                Client.Instance.PostFilesAsync<string>(file, Utils.FileSvc).Done(path =>
                {
                    tcs.SetResult(path);
                });
                return tcs.Task;
            }
            var reader = new FileReader();
            reader.OnLoad = (e) =>
            {
                UploadBase64Image(e.Target["result"].ToString(), file.Name).Done(path =>
                {
                    tcs.SetResult(path);
                    Client.Instance.PatchAsync(new PatchVM
                    {
                        Table = nameof(FileUpload),
                        Changes = new List<PatchDetail> {
                                new PatchDetail{ Field = Id, Value = System.Id.NewGuid() },
                                new PatchDetail{ Field = nameof(FileUpload.EntityName), Value = GuiInfo.EntityName },
                                new PatchDetail{ Field = nameof(FileUpload.RecordId), Value = EntityId },
                                new PatchDetail{ Field = nameof(FileUpload.SectionId), Value = GuiInfo.ComponentGroupId },
                                new PatchDetail{ Field = nameof(FileUpload.FieldName), Value = FieldName },
                                new PatchDetail{ Field = nameof(FileUpload.FileName), Value = file.Name },
                                new PatchDetail{ Field = nameof(FileUpload.FilePath), Value = path },
                        }
                    }).Done();
                });
            };
            reader.ReadAsDataURL(file);
            return tcs.Task;
        }

        private async Task UploadAllFiles(FileList filesSelected)
        {
            Spinner.AppendTo(EditForm.Element);
            var files = filesSelected.Select(UploadFile);
            var allPath = await Task.WhenAll(files);
            if (allPath.Nothing())
            {
                return;
            }
            if (GuiInfo.Precision == 0)
            {
                var paths = Path + PathSeparator + string.Join(PathSeparator, allPath);
                allPath = paths.Trim().Split(PathSeparator).Distinct().ToArray();
            }
            var oldVal = _path;
            Path = string.Join(PathSeparator, allPath);
            Spinner.Hide();
            FileUploaded?.Invoke();
        }

        private void OpenNativeFileDialog(Event e)
        {
            e?.PreventDefault();
            _input.Click();
        }

        public override string GetValueText()
        {
            if (_imageSources.Nothing())
            {
                return null;
            }
            return _imageSources.Select(path =>
            {
                var label = RemoveGuid(path);
                return $"<a target=\"_blank\" href=\"{path}\">{label}</a>";
            }).Combine(",");
        }
    }
}
