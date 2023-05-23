using Bridge.Html5;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;

namespace Core.Components.Forms
{
    public class ConfirmDialog : EditForm
    {
        private bool _cancel;
        private HTMLElement _yesBtn;
        public Textbox Textbox { get; private set; }
        public Number Number { get; private set; }
        public Datepicker Datepicker { get; private set; }
        public int? Precision;
        public SearchEntry SearchEntry { get; private set; }
        public bool IgnoreNoButton { get; set; }
        public bool MultipleLine { get; set; } = true;
        public string YesText { get; set; } = "Đồng ý";
        public string NoText { get; set; } = "Không";
        public string CancelText { get; set; } = "Đóng";
        public string Content { get; set; } = "Bạn có chắc chắn muốn xóa dữ liệu?";
        public bool NeedAnswer { get; set; }
        public string ComType { get; set; } = nameof(Textbox);
        public bool IgnoreCancelButton { get; set; } = true;

        public ConfirmDialog() : base(null)
        {
            PopulateDirty = false;
            Title = "Xác nhận";
        }

        public override void Render()
        {
            Html.Take(Document.Body).Div.ClassName("backdrop")
                .Style("align-items: center;").Escape((e) => Dispose());
            Element = Html.Context;
            ParentElement = Element.ParentElement;
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: auto;")
                .Div.ClassName("popup-title").IHtml(Title)
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, CloseDispose)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body");

            Html.Instance.P.IHtml(Content).End.Div.Event(EventType.KeyDown, HotKeyHandler).MarginRem(Direction.top, 1);
            if (NeedAnswer)
            {
                if (ComType == nameof(Textbox))
                {
                    Textbox = new Textbox(new Models.Component
                    {
                        PlainText = "Nhập câu trả lời",
                        ShowLabel = false,
                        FieldName = CompareGridView.ReasonOfChange,
                        Row = 2,
                    })
                    {
                        MultipleLine = MultipleLine
                    };
                    AddChild(Textbox);
                    Html.Instance.End.Render();
                }
                if (ComType == nameof(Number))
                {
                    Number = new Number(new Models.Component
                    {
                        PlainText = "Nhập câu trả lời",
                        FieldName = CompareGridView.ReasonOfChange,
                        Visibility = true,
                        ShowLabel = false,
                    });
                    AddChild(Number);
                    Html.Instance.End.Render();
                }
                if (ComType == nameof(Datepicker))
                {
                    Datepicker = new Datepicker(new Models.Component
                    {
                        PlainText = "Chọn ngày",
                        ShowLabel = false,
                        FieldName = CompareGridView.ReasonOfChange,
                        Row = 2,
                        FocusSearch = true,
                        Visibility = true,
                        Precision = Precision,
                    });
                    AddChild(Datepicker);
                    Html.Instance.EndOf(".datetime-picker");
                }
            }
            Html.Instance.Button(YesText, "button info small", "fa fa-check")
                    .Event(EventType.Click, async () =>
                    {
                        var isValid = await IsFormValid();
                        if (!isValid)
                        {
                            return;
                        }

                        try
                        {
                            YesConfirmed?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                        if (DisposeAfterYes)
                        {
                            Dispose();
                        }
                    }).Render();
            _yesBtn = Html.Context;
            Html.Instance.End.Render();
            if (!IgnoreNoButton)
            {
                Html.Instance.Button(NoText, "button alert small", "mif-exit")
                    .MarginRem(Direction.left, 1)
                    .Event(EventType.Click, () =>
                    {
                        try
                        {
                            NoConfirmed?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                        CloseDispose();
                    }).End.Render();
            }

            if (!IgnoreCancelButton)
            {
                Html.Instance.Button(CancelText, "button info small", "fa fa-times")
                    .MarginRem(Direction.left, 1)
                    .Event(EventType.Click, () =>
                    {
                        _cancel = true;
                        Dispose();
                    });
            }

            if (NeedAnswer)
            {
                if (Textbox != null)
                {
                    Textbox.Element.Focus();
                }
                if (Datepicker != null)
                {
                    Datepicker.Element.Focus();
                }
                if (Number != null)
                {
                    Number.Element.Focus();
                }
            }
            else
            {
                _yesBtn.Focus();
            }
        }

        public Action YesConfirmed;
        public Action NoConfirmed;
        public Action Canceled;
        public bool DisposeAfterYes { get; set; } = true;

        public override void Dispose()
        {
            base.Dispose();
        }

        public void CloseDispose()
        {
            Canceled?.Invoke();
            base.Dispose();
        }

        public static ConfirmDialog RenderConfirm(string content, Action yesConfirm, Action noConfirm = null)
        {
            var confirm = new ConfirmDialog
            {
                Content = content,
            };
            confirm.Render();
            confirm.YesConfirmed += yesConfirm;
            confirm.NoConfirmed += noConfirm;
            return confirm;
        }

        public static ConfirmDialog RenderConfirm(string content, string textButton = "Đóng")
        {
            var dialog = new ConfirmDialog
            {
                Content = content,
            };
            dialog.YesText = textButton;
            dialog.IgnoreNoButton = true;
            dialog.Render();
            return dialog;
        }

        protected void HotKeyHandler(Event e)
        {
            var keyCode = e.KeyCodeEnum();
            if (keyCode == KeyCodeEnum.Enter)
            {
                _yesBtn.Click();
            }
        }
    }
}
