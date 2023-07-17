using Core.Models;
using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components.Framework
{
    public class FeatureDetailBL : PopupEditor
    {
        private Feature FeatureEntity => Entity.CastProp<Feature>();

        public FeatureDetailBL() : base(nameof(Core.Models.Feature))
        {
            Name = "FeatureEditor";
            Title = "Feature";
            Config = true;
            PopulateDirty = false;
            Entity = new Feature();
            DOMContentLoaded += AlterPosition;
        }

        private void AlterPosition()
        {
            Element.ParentElement.AddClass("properties");
        }

        public async Task<bool> SaveFeature(Feature feature)
        {
            var extendedProp = feature.Properties;
            if (!extendedProp.HasAnyChar())
            {
                return await SaveFeatureInternal(feature);
            }
            ConfirmDialog.RenderConfirm("Feature properties has data.<br />Do you want to merge to the database?",
                async () =>
                {
                    await MergeFeatureProp(feature, feature.Properties);
                    feature.Properties = null;
                    await SaveFeatureInternal(feature);
                }, async () => await SaveFeatureInternal(feature));
            return true;
        }

        private async Task MergeFeatureProp(Feature feature, string str_prop)
        {
            if (str_prop.IsNullOrWhiteSpace())
            {
                return;
            }

            var properties = JsonConvert.DeserializeObject<Feature>(str_prop);
            MergeFeature(feature, properties);
            await CreateSection(feature, properties);
        }

        private async Task CreateSection(Feature feature, Feature properties)
        {
            if (properties.ComponentGroup.Nothing())
            {
                return;
            }
            var newSections = new List<object>();
            var extSection = properties.ComponentGroup.ToDictionaryDistinct(x => x.Id);
            var originSection = feature.ComponentGroup.ToDictionary(x => x.Id);
            foreach (var section in extSection)
            {
                if (originSection.ContainsKey(section.Key))
                {
                    originSection[section.Key].CopyPropFrom(section.Value);
                }
                else
                {
                    var newSection = new ComponentGroup();
                    newSection.CopyPropFrom(section.Value);
                    newSection.ClearReferences();
                    ReflectionExt.ProcessObjectRecursive(newSection, x =>
                    {
                        if (x.HasOwnProperty(IdField))
                        {
                            x[IdField] = 0;
                        }
                        if (x.HasOwnProperty(nameof(ComponentGroup.FeatureId)))
                        {
                            x[nameof(ComponentGroup.FeatureId)] = FeatureEntity.Id;
                        }
                        if (x.HasOwnProperty(nameof(ComponentGroup.ParentId)))
                        {
                            x[nameof(ComponentGroup.ParentId)] = null;
                        }
                        if (x.HasOwnProperty(nameof(GridPolicy.ComponentId)))
                        {
                            x[nameof(GridPolicy.ComponentId)] = null;
                        }
                    });
                    newSections.Add(newSection);
                }
            }
            try
            {
                var udpated = await new Client(nameof(ComponentGroup), typeof(User).Namespace).BulkUpdateAsync(newSections);
            }
            catch
            {
            }
        }

        private void MergeFeature(Feature feature, Feature properties)
        {
            if (feature.Id == properties.Id)
            {
                return;
            }
            feature.CopyPropFrom(properties, maxLevel: 1, new string[] { IdField, nameof(Feature.Name) });
        }

        protected override async Task<object> LoadEntity()
        {
            var featureTask = new Client(nameof(Models.Feature), typeof(User).Namespace).GetAsync<Feature>(FeatureEntity.Id);
            var sectionTask = new Client(nameof(ComponentGroup), typeof(User).Namespace).GetRawList<ComponentGroup>($"?$expad=Component&$filter=Active eq true and FeatureId eq {FeatureEntity.Id}");
            var headerTask = new Client(nameof(GridPolicy), typeof(User).Namespace).GetRawList<GridPolicy>($"?$filter=Active eq true and FeatureId eq {FeatureEntity.Id}");
            await Task.WhenAll(featureTask, sectionTask, headerTask);
            var feature = featureTask.Result;
            BuildTree(sectionTask.Result);
            feature.ComponentGroup = sectionTask.Result;
            feature.GridPolicy = headerTask.Result;
            feature.ClearReferences();
            var res = await base.LoadEntity();
            return res;
        }

        private async Task<bool> SaveFeatureInternal(Feature feature)
        {
            feature.ClearReferences();
            var comGroup = feature.ComponentGroup.ForEach(cg =>
            {
                cg.Component = null;
                cg.InverseParent = null;
                cg.Parent = null;
                cg.Feature = null;
                cg.FeatureId = feature.Id;
            });
            var components = feature.Component.ForEach(cg =>
            {
                cg.ComponentGroup = null;
            });
            var comGroupTask = comGroup.HasElement() ? new Client(nameof(ComponentGroup), typeof(User).Namespace).BulkUpdateAsync(comGroup.ToList()) : Task.FromResult(new List<ComponentGroup>());
            var comTask = components.HasElement() ? new Client(nameof(Component), typeof(User).Namespace).BulkUpdateAsync(components.ToList()) : Task.FromResult(new List<Component>());
            await Task.WhenAll(comGroupTask, comTask);
            feature.Active = true;
            feature.ComponentGroup.CopyPropFrom(comGroupTask.Result);
            feature.Component.CopyPropFrom(comTask.Result);
            var saved = await base.Save(true);
            return saved;
        }

        public void EditGridColumn(object arg)
        {
            var header = arg as GridPolicy;
            var editor = new HeaderEditor() { Entity = header, ParentElement = TabEditor.Element };
            var tab = Tabs.FirstOrDefault(x => x.Show);
            tab?.AddChild(editor);
        }
    }
}
