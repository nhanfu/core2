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

namespace Core.Components
{
    public class ImageUploader : EditableComponent
    {
        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                ParentElement.QuerySelectorAll(".gallery").Cast<HTMLElement>().ForEach(x => x.Remove());
                _path = value;
                if (Entity != null)
                {
                    Entity.SetComplexPropValue(GuiInfo.FieldName, _path);
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
        private static dynamic _webcam;
        public static HTMLElement Camera { get; set; }
        private bool _disabledDelete;
        private HTMLElement _placeHolder;
        private HTMLDivElement _gallerys;

        public event Action FileUploaded;
        public string DataSourceFilter { get; set; }
        public string Label { get; set; }
        private string[] _imageSources => _path?.Split(PathSeparator);

        public ImageUploader(Component ui) : base(ui)
        {
            GuiInfo = ui;
            Label = GuiInfo.PlainText ?? "image/invoice.svg";
            DataSourceFilter = GuiInfo.DataSourceFilter ?? "image/*";
        }

        public override void Render()
        {
            _path = Entity?.GetComplexPropValue(GuiInfo.FieldName)?.ToString();
            var paths = _path?.Split(PathSeparator).ToList();
            RenderUploadForm();
            Path = _path;
            DOMContentLoaded?.Invoke();
        }

        private HTMLElement RenderFileThumb(string path)
        {
            Html.Take(_gallerys).Div.ClassName("gallery");
            var thumbText = RemoveGuid(path);
            var isImage = PathIO.IsImage(path);
            if (isImage)
            {
                Html.Instance.Img.ClassName("image").Style(GuiInfo.ChildStyle).Src(Client.Origin + path.DecodeSpecialChar()).Render();
            }
            else
            {
                Html.Instance.Span.ClassName(thumbText.Contains("pdf") ? "fal fa-file-pdf" : "fal fa-file").Title(thumbText.DecodeSpecialChar())
                    .Style(GuiInfo.ChildStyle).Href(Client.Origin + path.DecodeSpecialChar()).Render();
            }
            Html.Instance.End.Render();
            Html.Instance.Div.ClassName("middle d-flex")
                .Div.ClassName("preview").Event(EventType.Click, Preview, path.DecodeSpecialChar()).I.ClassName("fas fa-eye").EndOf(MVVM.ElementType.div);
            if (!_disabledDelete)
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
            Html.Take(base.EditForm.Element).Div.ClassName("dark-overlay zoom")
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
            var isMultiple = GuiInfo.Precision == 0;
            Html.Take(ParentElement)
                .ClassName("choose-files")
                .Input.Type("file").Attr("name", "files")
                .ClassName("d-none")
                .Attr("accept", DataSourceFilter)
                .AsyncEvent(EventType.Change, UploadSelectedImages).Render();
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
            ConfirmDialog.RenderConfirm($"Bạn chắc chắn muốn xóa {removedPath}", async () =>
            {
                var removed = await new Client(nameof(User)).PostAsync<bool>(removedPath, "DeleteFile");
                var oldVal = _path;
                var newPath = _path.Replace(removedPath, string.Empty)
                    .Replace(PathSeparator + PathSeparator, string.Empty)
                    .Split(PathSeparator).Where(x => x.HasAnyChar()).Distinct().ToList();
                Path = string.Join(PathSeparator, newPath);
                Dirty = true;
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = GuiInfo.FieldName, EvType = EventType.Change });
                }
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
            });
        }

        private async Task UploadSelectedImages(Event e)
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
            await UploadAllFiles(files);
            Dirty = true;
            _input.Value = string.Empty;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _path, OldData = oldVal, FieldName = GuiInfo.FieldName, EvType = EventType.Change });
            }
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
        }

        private async Task<string> UploadBase64Image(string base64Image, string fileName)
        {
            if (base64Image.Contains(PNGUrlPrefix))
            {
                base64Image = base64Image.Substring(PNGUrlPrefix.Length);
            }
            else if (base64Image.Contains(JpegUrlPrefix))
            {
                base64Image = base64Image.Substring(JpegUrlPrefix.Length);
            }
            return await new Client(nameof(User)).PostAsync<string>(base64Image, $"Image?name={fileName.DecodeSpecialChar()}", allowNested: true);
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
            base.UpdateView(force, dirty, componentNames);
        }

        private Task<string> UploadFile(File file)
        {
            var tcs = new TaskCompletionSource<string>();
            var reader = new FileReader();
            if (!GuiInfo.IsRealtime && file.Type.Match("image.*").HasElement())
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
                    var path = await new Client(nameof(User)).PostFilesAsync<string>(file, "file");
                    tcs.SetResult(path);
                });
            }
            return tcs.Task;
        }

        public Task<string> ResizeAndUploadImage(string src, string fileName)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
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
