using Core.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.Models
{
    public partial class GridPolicy
    {
        public string DataSourceOptimized { get; set; }
        public string Sum { get; set; }
        public bool SimpleText { get; set; }
        public bool StatusBar { get; set; }
        public TextAlign? TextAlignEnum { get; set; }
        public int PostOrder { get; set; }
        public List<object> LocalData { get; set; }
        public List<GridPolicy> LocalHeader { get; set; }
        public bool LocalRender { get; set; }
        public bool IgnoreConfirmHardDelete { get; set; }
    }

    public partial class Component
    {
        public TextAlign? TextAlignEnum { get; set; }
        public bool IsPivot { get; set; }
        public bool AutoFit { get; set; }
        public string MaxWidth { get; set; }
        public int PostOrder { get; set; }
        public List<object> LocalData { get; set; }
        public List<Component> LocalHeader { get; set; }
        public bool StatusBar { get; set; }
        public bool SimpleText { get; set; }
        public string MinWidth { get; set; }
        public string DataSourceOptimized { get; set; }
        public bool LocalRender { get; set; }
        public bool IgnoreConfirmHardDelete { get; set; }
    }

    public partial class ComponentGroup
    {
        public int ItemInRow { get; set; }
    }

    public partial class Entity
    {
        public Type GetEntityType(string ns = null)
        {
            var name = AliasFor ?? Name;
            return Type.GetType((ns ?? Namespace ?? Clients.Client.ModelNamespace) + name);
        }
    }
}
