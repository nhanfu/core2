using TMS.API.Models;
using Core.Components.Forms;

namespace TMS.UI.Business.User
{
    public class ApprovalConfigBL : TabEditor
    {
        private PopupEditor _appForm;
        public ApprovalConfigBL() : base(nameof(ApprovalConfig))
        {
            Entity = new ApprovalConfig();
            Name = "ApprovalConfig";
        }

        public void CreateApprovalConfig()
        {
            _appForm = new PopupEditor(nameof(ApprovalConfig))
            {
                Entity = new ApprovalConfig(),
                Name = "ApprovalConfig Detail",
                Title = "Create phê duyệt",
                ParentElement = TabEditor.Element
            };
            AddChild(_appForm);
        }

        public void EditApprovalConfig(ApprovalConfig app)
        {
            _appForm = new PopupEditor(nameof(ApprovalConfig))
            {
                Entity = app,
                Name = "ApprovalConfig Detail",
                Title = "Chỉnh sửa phê duyệt",
                ParentElement = TabEditor.Element
            };
            AddChild(_appForm);
        }
    }
}
