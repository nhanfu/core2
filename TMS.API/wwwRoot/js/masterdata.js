debugger;
Bridge.define("TMS.UI.Business.Setting.MasterDataBL", {
    inherits: [Core.Components.Forms.TabEditor],
    ctors: {
        ctor: function () {
            this.$initialize();
            Core.Components.Forms.TabEditor.ctor.call(this, "MasterData");
            this.Name = "Master Data";
        }
    },
    methods: {
        EditMasterData: function (masterData) {
            var task = Core.Components.Extensions.ComponentExt.OpenPopup(this, "MasterData Detail", function () {
                var type = Bridge.Reflection.getType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                var instance = Bridge.as(Bridge.createInstance(type), Core.Components.Forms.PopupEditor);
                instance.Title = "Th\u00eam tham chi\u1ebfu m\u1edbi";
                instance.Entity = masterData || new TMS.API.Models.MasterData();
                return instance;
            }, false, false);
            Core.Clients.Client.ExecTask(Core.Components.Forms.TabEditor, task);
        },
        CreateMasterData: function () {
            this.EditMasterData(null);
        },
        UpdatePath: function () {
            var task = new Core.Clients.Client.$ctor1("MasterData").PostAsync(System.Boolean, null, System.String.format("UpdatePath", null));
            Core.Clients.Client.ExecTask(System.Boolean, task);
        },
        EditMasterDataParent: function (parent) {
            var masterDataTask = new Core.Clients.Client.$ctor1("MasterData").FirstOrDefaultAsync(TMS.API.Models.MasterData, System.String.format("?$filter=Id eq '{0}'", [parent.ParentId]));
            Core.Clients.Client.ExecTask(TMS.API.Models.MasterData, masterDataTask, Bridge.fn.bind(this, function (masterData) {
                var task = Core.Components.Extensions.ComponentExt.OpenPopup(this, "MasterData Detail", function () {
                    var type = Bridge.Reflection.getType("TMS.UI.Business.Setting.MasterDataDetailsBL");
                    var instance = Bridge.as(Bridge.createInstance(type), Core.Components.Forms.PopupEditor);
                    instance.Title = "Update tham chi\u1ebfu";
                    instance.Entity = masterData;
                    return instance;
                }, false, false);
                Core.Clients.Client.ExecTask(Core.Components.Forms.TabEditor, task);
            }));
        }
    }
});

export default TMS.UI.Business.Setting.MasterDataBL;