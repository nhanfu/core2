using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Models;

namespace Core.Components.Framework
{
    public class FeatureBL : TabEditor
    {
        public FeatureBL() : base(nameof(Feature))
        {
            Name = "Feature-management";
            Title = "Feature";
            Icon = "icons/config.png";
        }

        public void EditFeature(Component feature)
        {
            var id = "Feature_" + feature.Id;
            this.OpenTab(id, () => new FeatureDetailBL
            {
                Id = id,
                Entity = feature,
                Title = $"Feature {feature.Name ?? feature.Label ?? feature.Description}"
            });
        }
    }
}
