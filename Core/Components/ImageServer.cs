using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Linq;
using System.Threading.Tasks;
using PathIO = System.IO.Path;
using Core.ViewModels;

namespace Core.Components
{
    public class ImageServer : Image
    {
        protected static HTMLElement _backdrop;
        public override string Path
        {
            get => _path;
            set
            {
                RemoveChild();
                _path = value;
                if (Entity != null)
                {
                    Entity.SetComplexPropValue(FieldName, _path);
                }

                if (_path is null)
                {
                    RenderFileThumb(_path);
                    return;
                }
                var updatedImages = _path.Split(pathSeparator).ToList();
                updatedImages.ForEach(x =>
                {
                    RenderFileThumb(x);
                });
            }
        }

        private void RemoveChild()
        {
            ParentElement.QuerySelectorAll(".thumb-wrapper").Cast<HTMLElement>().ForEach(x => x.Remove());
        }

        private const string pathSeparator = "    ";
        private HTMLInputElement _input;
        private static HTMLElement _preview;
        private HTMLElement _plus;
        public static HTMLElement Camera { get; set; }
        private bool _disabledDelete;
        private HTMLElement _placeHolder;

        private string[] _imageSources => _path?.Split(pathSeparator);

        public ImageServer(Component ui) : base(ui)
        {
            GuiInfo = ui;
            DataSourceFilter = GuiInfo.DataSourceFilter ?? "image/*";
        }

        public override void Render()
        {
            _path = Entity?.GetPropValue(FieldName)?.ToString();
            var paths = _path?.Split(pathSeparator).ToList();
            RenderUploadForm();
            Path = _path;
            DOMContentLoaded?.Invoke();
        }

        private HTMLElement RenderFileThumb(string path, bool first = false)
        {
            Html.Take(Element).Div.ClassName("thumb-wrapper")
                .Div.ClassName("overlay");
            if (!_disabledDelete && path != null && path != "")
            {
                Html.Instance.I.ClassName("fal fa-eye").Event(EventType.Click, Preview, path).End.I.ClassName("fa fa-times").Event(EventType.Click, RemoveFile, path).End.Render();
            }
            Html.Instance.End.Render();
            if (!path.IsNullOrWhiteSpace())
            {
                Html.Instance.Img.Event(EventType.Click, OpenForm).ClassName("thumb").Style(GuiInfo.ChildStyle).Src(path).Render();
            }
            return Html.Context.ParentElement;
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
            Html.Take(base.EditForm.Element).Div.ClassName("dark-overlay")
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
                .Img.Src(Client.Origin + path);
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
        }

        private void RenderUploadForm()
        {
            Element = Html.Take(ParentElement).ClassName("uploader").Div.GetContext();
            if (_plus is null)
            {
                if (GuiInfo.Precision > 0)
                {
                    Html.Instance.I.Event(EventType.Click, OpenForm).ClassName("fal fa-plus mt-3").Style(GuiInfo.ChildStyle).End.Render();
                }
                else
                {
                    if (Path.IsNullOrWhiteSpace())
                    {
                        Html.Instance.I.Event(EventType.Click, OpenForm).ClassName("fal fa-plus mt-3").Style(GuiInfo.ChildStyle).End.Render();
                    }
                }
                _plus = Html.Context;
            }
        }

        private void OpenForm()
        {
            if (_backdrop is null)
            {
                Html.Take(Document.Body)
                    .Div.ClassName("backdrop").Style("z-index:2000").TabIndex(-1);
                _backdrop = Html.Context;
            }
            _backdrop.Show();
            Html.Take(_backdrop).Clear()
                .Div.ClassName("popup-content").Style("width:100%").Div.ClassName("popup-title").Span.IconForSpan("");
            Html.Instance.End.Span.Text("Danh sách hình ảnh");
            Html.Instance.End.Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                .Event(EventType.Click, ClosePopup)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body");
            Html.Instance
                .Input.Type("file").Attr("multiple", "multiple").Attr("name", "imagefiles").ClassName("d-none")
                .Attr("accept", DataSourceFilter)
                .Event(EventType.Change, (e) => UploadSelectedImages(e));
            _input = Html.Context as HTMLInputElement;
            Html.Instance.End.Div.ClassName("col-md-12").Style("padding: 20px 5%;")
                .Div.ClassName("alert alert-info").Text("Lưu ý: Hệ thống chỉ nhận các file ảnh (bmp, jpg, jpeg, gif, png) và dung lượng dưới 1Mb / file").End
                .Div.ClassName("gallery-buttons bottom-30px")
                    .Button.Event(EventType.Click, (e) => OpenNativeFileDialog(e)).Id("btn-upload").ClassName("btn btn-success btn-md dz-clickable mr-1").I.ClassName("fa fa-upload mr-1").End.Text("Upload ảnh mới").End
                    .Button.Id("btn-upload").ClassName("btn btn-success btn-md mr-1").I.ClassName("fa fa-heart mr-1").End.Text("Chèn ảnh đã chọn").End
                    .Button.Id("btn-upload").ClassName("btn btn-danger btn-md mr-1").I.ClassName("fa fa-trash mr-1").End.Text("Xóa ảnh đã chọn").End.End
                .Div.ClassName(" list-group")
                    .Div.ClassName("row").Id("previewContainer");

            var isFn = Utils.IsFunction(GuiInfo.PreQuery, out var fn);
            var loadImageTask = Client.Instance.ComQuery(new SqlViewModel
            {
                ComId = GuiInfo.Id,
                Params = isFn ? JSON.Stringify(fn.Call(null, this)) : null
            })
            .Done(ds =>
            {
                var images = ds.Length > 0 ? ds[0].As<Images[]>() : null;
                if (images.HasElement())
                {
                    RenderListImage(images.Select(x => x.Url).ToArray());
                }
            });
        }

        private void RemoveFile(Event e, string removedPath)
        {
            if (Disabled)
            {
                return;
            }
            _plus = null;
            e.StopPropagation();
            if (removedPath.IsNullOrEmpty())
            {
                return;
            }

            var oldVal = _path;
            if (GuiInfo.Precision > 1)
            {
                var newPath = _path.Replace(removedPath, string.Empty).Split(pathSeparator).Where(x => x.HasAnyChar()).ToList();
                Path = string.Join(pathSeparator, newPath);
            }
            else
            {
                Path = null;
            }
            var dispatchTask = this.DispatchEvent(GuiInfo.Events, EventType.Change, Entity);
            Client.ExecTaskNoResult(dispatchTask, () =>
            {
                UserInput?.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = FieldName });
                Dirty = true;
            });
        }

        private void UploadSelectedImages(Event e)
        {
            e.PreventDefault();
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            var oldVal = _path;
            UploadAllFiles(files).Done(() =>
            {
                Dirty = true;
                _input.Value = string.Empty;
                UserInput?.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = FieldName });
                this.DispatchEvent(GuiInfo.Events, EventType.Change, Entity).Done();
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

        private Task UploadAllFiles(FileList filesSelected)
        {
            var tcs = new TaskCompletionSource<bool>();
            Spinner.AppendTo(_backdrop);
            var files = filesSelected.Select(UploadFile);
            Task.WhenAll(files).Done(allPath =>
            {
                if (allPath.Nothing())
                {
                    return;
                }
                RenderListImage(allPath);
                Spinner.Hide();
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }

        private void RenderListImage(string[] allPath)
        {
            allPath.ForEach(img =>
            {
                Html.Take("#previewContainer").Div.ClassName("item col-md-1 col-sm-3")
                 .Div.ClassName("thumbnail")
                     .Div.Img.Event(EventType.Click, () => ChooseImage(img)).Src(img).ClassName("list-group-image").End.Input.Type("checkbox").End.End.End.End.Render();
            });
        }

        private void ChooseImage(string img)
        {
            if (GuiInfo.Precision > 1)
            {
                Path += $"{pathSeparator}{img}";
            }
            else
            {
                Path = $"{img}";
                ClosePopup();
            }
            Dirty = true;
            UserInput?.Invoke(new ObservableArgs { NewData = _path, FieldName = FieldName, EvType = EventType.Change });
            this.DispatchEvent(GuiInfo.Events, EventType.Change, Entity).Done();
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

        public void ClosePopup()
        {
            _backdrop?.Hide();
        }
    }
}
