using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapMarkerRecord {
    [Tooltip("Stable marker id.")]
    public string id;
    [Tooltip("Name shown in minimap/world map UI.")]
    public string displayName;
    [Tooltip("Description shown by future marker detail panels.")]
    [TextArea]
    public string description;
    [Tooltip("Marker category used by filters and icons.")]
    public MapMarkerCategory category;
    [Tooltip("World position of this marker.")]
    public Vector3 worldPosition;
    [Tooltip("Scene name where this marker exists.")]
    public string sceneName;
    [Tooltip("Icon used by UI.")]
    public Sprite icon;
    [Tooltip("Tint color used by UI.")]
    public Color color = Color.white;
    [Tooltip("Higher priority markers can be drawn above lower priority markers.")]
    public int priority;
    [Tooltip("If enabled, this marker can be shown on the minimap.")]
    public bool showOnMinimap;
    [Tooltip("If enabled, this marker can be shown on the full world map.")]
    public bool showOnWorldMap;
    [Tooltip("If enabled, future UI should treat this marker as important/pinned.")]
    public bool important;
    [Tooltip("Whether the player has discovered this marker.")]
    public bool discovered;
    [Tooltip("Whether future UI should hide this marker by player preference.")]
    public bool hidden;
    [Tooltip("Whether future UI should highlight this marker by player preference.")]
    public bool favorite;
    [Tooltip("Optional related region id.")]
    public string regionId;
    [Tooltip("Optional related PokeNav entry id.")]
    public string pokeNavEntryId;
    [Tooltip("Optional related social post id.")]
    public string socialPostId;
    [Tooltip("Optional related Pokemon id.")]
    public string pokemonId;
    [Tooltip("System or component that produced this marker.")]
    public string source;
    [Tooltip("Free-form marker tags copied from the definition.")]
    public List<string> tags = new List<string>();
}
