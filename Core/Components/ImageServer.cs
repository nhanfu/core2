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

namespace Core.Components
{
    public class ImageServer : EditableComponent
    {
        protected static HTMLElement _backdrop;
        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                RemoveChild();
                _path = value;
                if (Entity != null)
                {
                    Entity.SetComplexPropValue(GuiInfo.FieldName, _path);
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
        private const string PNGUrlPrefix = "data:image/png;base64,";
        private const string JpegUrlPrefix = "data:image/jpeg;base64,";
        private const int GuidLength = 36;
        private HTMLInputElement _input;
        private static HTMLElement _preview;
        private HTMLElement _plus;
        public static HTMLElement Camera { get; set; }
        private bool _disabledDelete;
        private HTMLElement _placeHolder;

        public string DataSourceFilter { get; set; }
        public string Label { get; set; }
        private string[] _imageSources => _path?.Split(pathSeparator);

        public ImageServer(Component ui) : base(ui)
        {
            GuiInfo = ui;
            DataSourceFilter = GuiInfo.DataSourceFilter ?? "image/*";
        }

        public override void Render()
        {
            _path = Entity?.GetComplexPropValue(GuiInfo.FieldName)?.ToString();
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
                Html.Instance.Img.AsyncEvent(EventType.Click, OpenForm).ClassName("thumb").Style(GuiInfo.ChildStyle).Src(path).Render();
            }
            return Html.Context.ParentElement;
        }

        private static string RemoveGuid(string path)
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
            else
            {
                Element.QuerySelectorAll(".overlay .fa-times").Cast<HTMLElement>().ForEach(x => x.Remove());
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
            Html.Take(base.EditForm.Element).Div.ClassName("dark-overlay")
                .Escape((e) => _preview.Remove())
                .Event(EventType.KeyDown, (Action<Event>)((Event e) =>
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
                }));
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

        private void MoveAround(Event e, string path)
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

        private string MoveLeft(string path, HTMLImageElement img)
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

            img.Src = Client.Origin + _imageSources[index];
            return _imageSources[index];
        }

        private string MoveRight(string path, HTMLImageElement img)
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

            img.Src = Client.Origin + _imageSources[index];
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
            Element = Html.Take(ParentElement).ClassName("uploader").Div.GetContext();
            if (_plus is null)
            {
                if (GuiInfo.Precision > 0)
                {
                    Html.Instance.I.AsyncEvent(EventType.Click, OpenForm).ClassName("fal fa-plus mt-3").Style(GuiInfo.ChildStyle).End.Render();
                }
                else
                {
                    if (Path.IsNullOrWhiteSpace())
                    {
                        Html.Instance.I.AsyncEvent(EventType.Click, OpenForm).ClassName("fal fa-plus mt-3").Style(GuiInfo.ChildStyle).End.Render();
                    }
                }
                _plus = Html.Context;
            }
        }

        private async Task OpenForm()
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
                .AsyncEvent(EventType.Change, (e) => UploadSelectedImages(e));
            _input = Html.Context as HTMLInputElement;
            Html.Instance.End.Div.ClassName("col-md-12").Style("padding: 20px 5%;")
                .Div.ClassName("alert alert-info").Text("Lưu ý: Hệ thống chỉ nhận các file ảnh (bmp, jpg, jpeg, gif, png) và dung lượng dưới 1Mb / file").End
                .Div.ClassName("gallery-buttons bottom-30px")
                    .Button.Event(EventType.Click, (e) => OpenNativeFileDialog(e)).Id("btn-upload").ClassName("btn btn-success btn-md dz-clickable mr-1").I.ClassName("fa fa-upload mr-1").End.Text("Upload ảnh mới").End
                    .Button.Id("btn-upload").ClassName("btn btn-success btn-md mr-1").I.ClassName("fa fa-heart mr-1").End.Text("Chèn ảnh đã chọn").End
                    .Button.Id("btn-upload").ClassName("btn btn-danger btn-md mr-1").I.ClassName("fa fa-trash mr-1").End.Text("Xóa ảnh đã chọn").End.End
                .Div.ClassName(" list-group")
                    .Div.ClassName("row").Id("previewContainer");
            var loadImage = await new Client(nameof(Images)).GetList<Images>($"?$filter=Active eq true");
            if (loadImage.Value != null)
            {
                RenderListImage(loadImage.Value.ToArray());
            }
        }

        private void RenderPlaceHolder()
        {
            if (_path.IsNullOrWhiteSpace())
            {
                _placeHolder = RenderFileThumb(null, true);
            }
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

            Task.Run(async () =>
            {
                var oldVal = _path;
                if (GuiInfo.Precision > 1)
                {
                    var newPath = _path.Replace(removedPath, string.Empty)
                        .Split(pathSeparator).Where(x => x.HasAnyChar()).ToList();
                    Path = string.Join(pathSeparator, newPath);
                }
                else
                {
                    Path = null;
                }
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = GuiInfo.FieldName });
                }
                Dirty = true;
            });
        }

        private async Task UploadSelectedImages(Event e)
        {
            e.PreventDefault();
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }
            var oldVal = _path;
            await UploadAllFiles(files);
            Dirty = true;
            _input.Value = string.Empty;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = GuiInfo.FieldName });
            }
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
        }

        private async Task<Images> UploadBase64Image(string base64Image, string fileName)
        {
            if (base64Image.Contains(PNGUrlPrefix))
            {
                base64Image = base64Image.Substring(PNGUrlPrefix.Length);
            }
            else if (base64Image.Contains(JpegUrlPrefix))
            {
                base64Image = base64Image.Substring(JpegUrlPrefix.Length);
            }
            return await new Client(nameof(Images)).PostAsync<Images>(base64Image, $"UploadImage?filename={fileName}&path={(GuiInfo.DataSourceFilter.IsNullOrWhiteSpace() ? "\\upload\\images" : GuiInfo.DataSourceFilter)}");
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
            Path = Entity.GetComplexPropValue(GuiInfo.FieldName)?.ToString();
            base.UpdateView();
        }

        private Task<Images> UploadFile(File file)
        {
            var tcs = new TaskCompletionSource<Images>();
            var reader = new FileReader();
            var isImage = file.Type.Match("image.*").HasElement();
            if (isImage)
            {
                reader.OnLoad = async (e) =>
                {
                    var path = await ResizeAndUploadImage(e.Target["result"].ToString(), file.Name);
                    tcs.SetResult(path);
                };
                reader.ReadAsDataURL(file);
            }
            else
            {
                Task.Run(async () =>
                {
                    var path = await new Client(nameof(Images)).PostFilesAsync<Images>(file, "file");
                    tcs.SetResult(path);
                });
            }
            return tcs.Task;
        }

        public Task<Images> ResizeAndUploadImage(string src, string fileName)
        {
            var tcs = new TaskCompletionSource<Images>();
            var image = new HTMLImageElement();
            image.OnLoad += async (imageEvent) =>
            {
                var canvas = Document.CreateElement("canvas").As<HTMLCanvasElement>();
                var max_size = 1024;
                var width = image.Width;
                var height = image.Height;
                if (width > height)
                {
                    if (width > max_size)
                    {
                        height = (int)(height * ((float)max_size / width));
                        width = max_size;
                    }
                }
                else
                {
                    if (height > max_size)
                    {
                        width = (int)(width * ((float)max_size / height));
                        height = max_size;
                    }
                }
                canvas.Width = width;
                canvas.Height = height;
                var ctx = canvas.GetContext("2d").As<CanvasRenderingContext2D>();
                ctx.DrawImage(image, 0, 0, width, height);
                var dataUrl = canvas.ToDataURL();
                var path = await UploadBase64Image(dataUrl, fileName);
                tcs.SetResult(path);
            };
            image.Src = src;
            return tcs.Task;
        }

        private async Task UploadAllFiles(FileList filesSelected)
        {
            Spinner.AppendTo(_backdrop);
            var files = filesSelected.Select(UploadFile);
            var allPath = await Task.WhenAll(files);
            if (allPath.Nothing())
            {
                return;
            }
            RenderListImage(allPath);
            Spinner.Hide();
        }

        private void RenderListImage(Images[] allPath)
        {
            allPath.ForEach(img =>
            {
                Html.Take("#previewContainer").Div.ClassName("item col-md-1 col-sm-3")
                 .Div.ClassName("thumbnail")
                     .Div.Img.Event(EventType.Click, async () => await ChooseImage(img)).Src(img.Url).ClassName("list-group-image").End.Input.Type("checkbox").Event(EventType.Change, async () => await DeleteImage(img)).End.End.End.End.Render();
            });
        }

        private async Task ChooseImage(Images img)
        {
            if (GuiInfo.Precision > 1)
            {
                Path += $"{pathSeparator}{img.Url}";
            }
            else
            {
                Path = $"{img.Url}";
                ClosePopup();
            }
            Dirty = true;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _path, FieldName = GuiInfo.FieldName, EvType = EventType.Change });
            }
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
        }

        private async Task DeleteImage(Images img)
        {

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
