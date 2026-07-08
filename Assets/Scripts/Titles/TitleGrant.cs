using System;
using UnityEngine;

[Serializable]
public class TitleGrant {
    [Tooltip("Title, badge, permit or rank granted by this entry.")]
    public TitleDefinition title;
    [Tooltip("If enabled, this grant is permanent. If disabled, it uses duration/default hours when the title allows temporary grants.")]
    public bool grantPermanently = true;
    [Tooltip("Temporary grant duration in in-game hours. 0 uses the title default duration.")]
    [Min(0)]
    public int durationHours;
    [Tooltip("Short reason stored in save/debug data, such as quest, professor, police or event.")]
    public string source;
    [Tooltip("If enabled, an existing temporary copy is refreshed/replaced with the new duration.")]
    public bool refreshExisting = true;

    public int ResolveDurationHours() {
        if(title == null) {
            return 0;
        }

        if(grantPermanently || !title.CanBeTemporary) {
            return -1;
        }

        if(durationHours > 0) {
            return durationHours;
        }

        if(title.DefaultDurationHours > 0) {
            return title.DefaultDurationHours;
        }

        return title.PermanentByDefault ? -1 : 1;
    }
}
