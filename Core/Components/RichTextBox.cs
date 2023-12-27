using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Threading.Tasks;
using Core.Structs;

namespace Core.Components
{
    public class RichTextBox : EditableComponent
    {
        private HTMLTextAreaElement _textArea;
        private object editor;

        public RichTextBox(Component ui, HTMLElement ele = null) : base(ui)
        {
            _textArea = ele as HTMLTextAreaElement;
        }

        public override void Render()
        {
            if (GuiInfo.Row <= 0)
            {
                GuiInfo.Row = 1;
            }
            if (_textArea is null)
            {
                _textArea = Html.Take(ParentElement).TextArea.Id("RE_" + Uuid7.Id25()).GetContext() as HTMLTextAreaElement;
            }
            Task.Run(async () =>
            {
                await Client.LoadScript("https://support.pavietnam.vn/js/tinymce/tinymce.min.js?v=1.9.84");
                /*@
                var self = this;
                tinymce.init({
                    selector: '#' + this._textArea.id,
                    menubar: 'file edit view insert format tools table tc help',
                    plugins: 'print preview code powerpaste casechange importcss tinydrive searchreplace autolink autosave save directionality advcode visualblocks visualchars fullscreen image link media mediaembed template codesample table charmap hr pagebreak nonbreaking anchor toc insertdatetime advlist lists checklist wordcount tinymcespellchecker a11ychecker imagetools textpattern noneditable help formatpainter permanentpen pageembed charmap tinycomments mentions quickbars linkchecker emoticons advtable export',
                    toolbar: 'undo redo | bold italic underline strikethrough | fontselect fontsizeselect formatselect | alignleft aligncenter alignright alignjustify | outdent indent |  numlist bullist checklist | forecolor backcolor anchor link media image table | hr code removeformat | fullscreen | casechange permanentpen formatpainter | pagebreak | charmap emoticons | preview save print | insertfile pageembed template | a11ycheck ltr rtl | showcomments addcomment',
                    fontsize_formats: '8pt 9pt 10pt 11pt 12pt 13pt 14pt 15pt 16pt 17pt 18pt 24pt 36pt 48pt',
                    images_upload_handler: self.ImageUploadHandler,
                    setup: function(editor) {
                        self.editor = editor;
                        editor.on('init', function() {
                            editor.setContent(self.Entity[self.FieldName] || '');
                        });
                        editor.on('input', function(e) {
                            self.Entity[self.FieldName] = editor.getContent();
                            self.Dirty = true;
                        });
                    }
                });
                */
            });
        }

        public void ImageUploadHandler(object fileWrapper, Action<string> success)
        {
            var file = fileWrapper["blob"].As<Func<File>>().Invoke();
            var reader = new FileReader();
            reader.OnLoad += async (Event e) =>
            {
                var uploader = new ImageUploader(new Component());
                var path = await uploader.UploadBase64Image(e.Target["result"].ToString(), file.Name);
                if (success != null)
                {
                    success.Invoke(path);
                    string content = null;
                    /*@
                    content = this.editor.setContent(text);
                    */
                    Entity.SetComplexPropValue(FieldName, content);
                }
            };
            reader.ReadAsDataURL(file);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var text = Utils.GetPropValue(Entity, FieldName);
            /*@
            this.editor.setContent(text || '');
            */
        }
    }
}
