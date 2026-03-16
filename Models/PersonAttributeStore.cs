namespace AOLTv1.Models
{
    public class PersonAttributeEntry
    {
        public string AttributeName { get; set; }
        public object Value { get; set; }
        public int WaypointEntryFrame { get; set; }
        public int ApplyFromFrame { get; set; }
        public int PersonId { get; set; }
    }

    public class PersonAttributeStore
    {
        private Dictionary<(string videoFile, int personId), Dictionary<string, object>> globalAttributes = new();
        private Dictionary<int, Dictionary<string, int>> globalAttributePriority = new();
        private Dictionary<int, List<PersonAttributeEntry>> waypointScopedAttributes = new();

        public static readonly HashSet<string> singleSelectAttributeNames = new HashSet<string>
        {
            "Occlusion", "BodyView", "ActionType",
            "Age", "Gender", "Height", "Weight/BodyShape", "Face",
            "HairLength", "HairStyle", "HairColor",
            "UpperClothType", "UpperClothSleeve", "UpperClothPattern",
            "LowerClothType", "LowerClothLegwear", "LowerClothLength", "LowerClothPattern", "LowerClothMaterial",
            "FootwearType", "FootwearColor"
        };

        private static readonly HashSet<string> waypointScopedAttributeNames = new HashSet<string>
        {
            "Occlusion", "BodyView", "ActionType"
        };

        public static bool IsWaypointScoped(string attributeName)
        {
            return waypointScopedAttributeNames.Contains(attributeName);
        }

        private int CalculateGlobalPriority(int personId, int waypointEntryFrame, int applyFromFrame, List<WaypointMarker> waypointMarkers)
        {
            var allWaypoints = waypointMarkers
                .Where(w => w.Label == "person" && w.ObjectId == personId)
                .OrderBy(w => w.EntryFrame)
                .ToList();

            if (!allWaypoints.Any())
                return 10;

            bool isFirstWaypoint = allWaypoints[0].EntryFrame == waypointEntryFrame;

            if (isFirstWaypoint && applyFromFrame == waypointEntryFrame)
                return 100;
            else if (applyFromFrame == waypointEntryFrame)
                return 50;
            else
                return 10;
        }

        public object GetAttribute(int personId, int frameIndex, string attributeName, List<WaypointMarker> waypointMarkers, string videoFile = null)
        {
            string searchAttributeName = attributeName;
            if (attributeName == "Weight" || attributeName == "BodyPosture")
                searchAttributeName = "Weight/BodyShape";

            bool isSingleSelect = singleSelectAttributeNames.Contains(attributeName);
            object rawValue = null;

            var currentWaypoint = waypointMarkers
                .Where(w => w.Label == "person" &&
                           w.ObjectId == personId &&
                           frameIndex >= w.EntryFrame &&
                           frameIndex <= w.ExitFrame)
                .OrderByDescending(w => w.EntryFrame)
                .FirstOrDefault();

            if (waypointScopedAttributes.ContainsKey(personId) && currentWaypoint != null)
            {
                var entry = waypointScopedAttributes[personId]
                    .Where(e => e.AttributeName == searchAttributeName &&
                               e.WaypointEntryFrame == currentWaypoint.EntryFrame &&
                               e.ApplyFromFrame <= frameIndex)
                    .OrderByDescending(e => e.ApplyFromFrame)
                    .FirstOrDefault();

                if (entry != null)
                    rawValue = entry.Value;
            }

            if (rawValue == null && !string.IsNullOrEmpty(videoFile))
            {
                bool hasWaypointScopedValue = currentWaypoint != null &&
                    waypointScopedAttributes.ContainsKey(personId) &&
                    waypointScopedAttributes[personId].Any(e =>
                        e.WaypointEntryFrame == currentWaypoint.EntryFrame &&
                        (e.AttributeName == searchAttributeName ||
                         (searchAttributeName == "Weight/BodyShape" && (e.AttributeName == "Weight" || e.AttributeName == "BodyPosture"))));

                if (!hasWaypointScopedValue)
                {
                    var currentKey = (videoFile, personId);
                    if (globalAttributes.ContainsKey(currentKey))
                    {
                        if (globalAttributes[currentKey].ContainsKey(searchAttributeName))
                            rawValue = globalAttributes[currentKey][searchAttributeName];
                        else if (globalAttributes[currentKey].ContainsKey(attributeName))
                            rawValue = globalAttributes[currentKey][attributeName];
                    }

                    if (rawValue == null)
                    {
                        foreach (var kvp in globalAttributes)
                        {
                            if (kvp.Key.personId == personId && kvp.Key.videoFile != videoFile)
                            {
                                if (kvp.Value.ContainsKey(searchAttributeName))
                                {
                                    rawValue = kvp.Value[searchAttributeName];
                                    break;
                                }
                                else if (kvp.Value.ContainsKey(attributeName))
                                {
                                    rawValue = kvp.Value[attributeName];
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (rawValue == null) return null;

            if (isSingleSelect)
            {
                if (rawValue is List<string> listValue && listValue.Count > 0)
                    return listValue[0];
                else if (rawValue is string[] arrayValue && arrayValue.Length > 0)
                    return arrayValue[0];
                else if (rawValue is string stringValue)
                    return stringValue;
                else
                    return rawValue.ToString();
            }

            if (rawValue is List<string> listValue2)
                return listValue2;
            else if (rawValue is string[] arrayValue2)
                return arrayValue2.ToList();
            else if (rawValue is string stringValue2)
                return new List<string> { stringValue2 };
            else
                return new List<string> { rawValue.ToString() };
        }

        public void SetAttribute(int personId, int waypointEntryFrame, int applyFromFrame, string attributeName, object value, List<WaypointMarker> waypointMarkers, string videoFile = null)
        {
            string saveAttributeName = attributeName;
            if (attributeName == "Weight" || attributeName == "BodyPosture")
                saveAttributeName = "Weight/BodyShape";

            if (value == null)
            {
                if (waypointScopedAttributes.ContainsKey(personId))
                {
                    waypointScopedAttributes[personId].RemoveAll(e =>
                        e.AttributeName == saveAttributeName ||
                        (saveAttributeName == "Weight/BodyShape" && (e.AttributeName == "Weight" || e.AttributeName == "BodyPosture")));
                }
                if (!string.IsNullOrEmpty(videoFile))
                {
                    var key = (videoFile, personId);
                    if (globalAttributes.ContainsKey(key))
                    {
                        globalAttributes[key][saveAttributeName] = null;
                        if (saveAttributeName == "Weight/BodyShape")
                        {
                            globalAttributes[key]["Weight"] = null;
                            globalAttributes[key]["BodyPosture"] = null;
                        }
                    }
                }
                if (globalAttributePriority.ContainsKey(personId))
                {
                    globalAttributePriority[personId].Remove(saveAttributeName);
                    if (saveAttributeName == "Weight/BodyShape")
                    {
                        globalAttributePriority[personId].Remove("Weight");
                        globalAttributePriority[personId].Remove("BodyPosture");
                    }
                }
            }
            else
            {
                if (!waypointScopedAttributes.ContainsKey(personId))
                    waypointScopedAttributes[personId] = new List<PersonAttributeEntry>();

                waypointScopedAttributes[personId].RemoveAll(e =>
                    (e.AttributeName == saveAttributeName ||
                     (saveAttributeName == "Weight/BodyShape" && (e.AttributeName == "Weight" || e.AttributeName == "BodyPosture"))) &&
                    e.WaypointEntryFrame == waypointEntryFrame &&
                    e.ApplyFromFrame >= applyFromFrame);

                waypointScopedAttributes[personId].Add(new PersonAttributeEntry
                {
                    AttributeName = saveAttributeName,
                    Value = value,
                    WaypointEntryFrame = waypointEntryFrame,
                    ApplyFromFrame = applyFromFrame,
                    PersonId = personId
                });

                waypointScopedAttributes[personId].Sort((a, b) =>
                {
                    int entryCompare = a.WaypointEntryFrame.CompareTo(b.WaypointEntryFrame);
                    if (entryCompare != 0) return entryCompare;
                    return a.ApplyFromFrame.CompareTo(b.ApplyFromFrame);
                });

                if (!string.IsNullOrEmpty(videoFile))
                {
                    var key = (videoFile, personId);
                    if (!globalAttributes.ContainsKey(key))
                        globalAttributes[key] = new Dictionary<string, object>();
                    globalAttributes[key][saveAttributeName] = value;
                }
            }
        }

        public void SetAttributesBatch(int personId, List<(int waypointEntryFrame, int applyFromFrame, string attributeName, object value)> attributes, List<WaypointMarker> waypointMarkers, string videoFile = null)
        {
            if (attributes == null || attributes.Count == 0)
                return;

            bool needsSort = false;

            foreach (var (waypointEntryFrame, applyFromFrame, attributeName, value) in attributes)
            {
                if (value == null)
                {
                    if (waypointScopedAttributes.ContainsKey(personId))
                        waypointScopedAttributes[personId].RemoveAll(e => e.AttributeName == attributeName);
                    if (!string.IsNullOrEmpty(videoFile))
                    {
                        var key = (videoFile, personId);
                        if (globalAttributes.ContainsKey(key))
                            globalAttributes[key][attributeName] = null;
                    }
                }
                else
                {
                    if (!waypointScopedAttributes.ContainsKey(personId))
                        waypointScopedAttributes[personId] = new List<PersonAttributeEntry>();

                    waypointScopedAttributes[personId].RemoveAll(e =>
                        e.AttributeName == attributeName &&
                        e.WaypointEntryFrame == waypointEntryFrame &&
                        e.ApplyFromFrame >= applyFromFrame);

                    waypointScopedAttributes[personId].Add(new PersonAttributeEntry
                    {
                        AttributeName = attributeName,
                        Value = value,
                        WaypointEntryFrame = waypointEntryFrame,
                        ApplyFromFrame = applyFromFrame,
                        PersonId = personId
                    });

                    needsSort = true;

                    if (!string.IsNullOrEmpty(videoFile))
                    {
                        var key = (videoFile, personId);
                        if (!globalAttributes.ContainsKey(key))
                            globalAttributes[key] = new Dictionary<string, object>();
                        globalAttributes[key][attributeName] = value;
                    }
                }
            }

            if (needsSort && waypointScopedAttributes.ContainsKey(personId))
            {
                waypointScopedAttributes[personId].Sort((a, b) =>
                {
                    int entryCompare = a.WaypointEntryFrame.CompareTo(b.WaypointEntryFrame);
                    if (entryCompare != 0) return entryCompare;
                    return a.ApplyFromFrame.CompareTo(b.ApplyFromFrame);
                });
            }
        }

        public Dictionary<string, object> GetAllAttributes(int personId, int frameIndex, List<WaypointMarker> waypointMarkers, string videoFile = null)
        {
            var result = new Dictionary<string, object>();

            var currentWaypoint = waypointMarkers
                .Where(w => w.Label == "person" &&
                           w.ObjectId == personId &&
                           frameIndex >= w.EntryFrame &&
                           frameIndex <= w.ExitFrame)
                .OrderByDescending(w => w.EntryFrame)
                .FirstOrDefault();

            if (currentWaypoint != null && waypointScopedAttributes.ContainsKey(personId))
            {
                var entries = waypointScopedAttributes[personId]
                    .Where(e => e.WaypointEntryFrame == currentWaypoint.EntryFrame &&
                               e.ApplyFromFrame <= frameIndex);

                foreach (var entry in entries.GroupBy(e => e.AttributeName))
                {
                    var latestEntry = entry.OrderByDescending(e => e.ApplyFromFrame).First();
                    if (latestEntry.Value != null)
                    {
                        string attrName = latestEntry.AttributeName;
                        if (attrName == "Weight" || attrName == "BodyPosture")
                            attrName = "Weight/BodyShape";
                        result[attrName] = latestEntry.Value;
                    }
                }
            }

            if (!string.IsNullOrEmpty(videoFile))
            {
                var currentKey = (videoFile, personId);
                var fallbackAttributes = new Dictionary<string, object>();

                var wpScopedAttrNames = new HashSet<string>();
                if (currentWaypoint != null && waypointScopedAttributes.ContainsKey(personId))
                {
                    foreach (var entry in waypointScopedAttributes[personId]
                        .Where(e => e.WaypointEntryFrame == currentWaypoint.EntryFrame))
                    {
                        string attrName = entry.AttributeName;
                        if (attrName == "Weight" || attrName == "BodyPosture")
                            attrName = "Weight/BodyShape";
                        wpScopedAttrNames.Add(attrName);
                    }
                }

                if (globalAttributes.ContainsKey(currentKey))
                {
                    foreach (var kvp in globalAttributes[currentKey])
                    {
                        if (kvp.Value != null)
                        {
                            string attrName = kvp.Key;
                            if (attrName == "Weight" || attrName == "BodyPosture")
                                attrName = "Weight/BodyShape";
                            if (!result.ContainsKey(attrName) && !wpScopedAttrNames.Contains(attrName))
                                result[attrName] = kvp.Value;
                        }
                    }
                }

                foreach (var kvp in globalAttributes)
                {
                    if (kvp.Key.personId == personId && kvp.Key.videoFile != videoFile)
                    {
                        foreach (var attrKvp in kvp.Value)
                        {
                            if (attrKvp.Value != null)
                            {
                                string attrName = attrKvp.Key;
                                if (attrName == "Weight" || attrName == "BodyPosture")
                                    attrName = "Weight/BodyShape";
                                if (!result.ContainsKey(attrName) &&
                                    !wpScopedAttrNames.Contains(attrName) &&
                                    !fallbackAttributes.ContainsKey(attrName))
                                {
                                    fallbackAttributes[attrName] = attrKvp.Value;
                                }
                            }
                        }
                    }
                }

                foreach (var kvp in fallbackAttributes)
                    result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        public void ClearPersonAttributes(int personId, string videoFile = null)
        {
            if (!string.IsNullOrEmpty(videoFile))
            {
                var key = (videoFile, personId);
                if (globalAttributes.ContainsKey(key))
                    globalAttributes[key].Clear();
            }
            if (waypointScopedAttributes.ContainsKey(personId))
                waypointScopedAttributes[personId].Clear();
        }
    }
}
