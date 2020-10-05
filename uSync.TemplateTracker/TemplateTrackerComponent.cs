using Umbraco.Core;
using Umbraco.Core.Composing;

namespace uSync.TemplateTracker
{
    public class TemplateTrackerCompose
        : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.RegisterUnique<TemplateTracker>();
            composition.Components().Append<TemplateTrackerComponent>();
        }
    }

    public class TemplateTrackerComponent : IComponent
    {
        private readonly TemplateTracker tracker;

        public TemplateTrackerComponent(TemplateTracker tracker)
        {
            this.tracker = tracker;
        }

        public void Initialize()
        {
            tracker.TrackChanges();
            // tracker.WatchViewFolder();
        }

        public void Terminate()
        {
            // do nothing.
        }
    }
}
