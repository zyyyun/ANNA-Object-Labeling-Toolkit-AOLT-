using Newtonsoft.Json;

namespace ASLTv1.Models
{
    #region JSON Serialization Classes (COCO Format)

    public class ImageInfo
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("height")] public int Height { get; set; }
        [JsonProperty("width")] public int Width { get; set; }
        [JsonProperty("frame_number")] public int FrameNumber { get; set; }
        [JsonProperty("timestamp")] public string Timestamp { get; set; }
    }

    public class TrackEntry
    {
        [JsonProperty("frame")] public int Frame { get; set; }
        [JsonProperty("timestamp")] public string Timestamp { get; set; }
    }

    public class TrackInfo
    {
        [JsonProperty("entry")] public TrackEntry Entry { get; set; }
        [JsonProperty("exit")] public TrackEntry Exit { get; set; }
        [JsonProperty("current_clip_count")] public int CurrentClipCount { get; set; }
    }

    public class AnnotationData
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("image_id")] public int ImageId { get; set; }
        [JsonProperty("category_id")] public int CategoryId { get; set; }
        [JsonProperty("bbox")] public int[] Bbox { get; set; }
        [JsonProperty("area")] public int Area { get; set; }
        [JsonProperty("iscrowd")] public int Iscrowd { get; set; }
        [JsonProperty("track_id")] public int TrackId { get; set; }
        [JsonProperty("track_info")] public TrackInfo TrackInfo { get; set; }
        [JsonProperty("interacting_object", NullValueHandling = NullValueHandling.Ignore)]
        public string InteractingObject { get; set; }
    }

    public class CategoryData
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("supercategory")] public string Supercategory { get; set; }
    }

    public class VideoInfoExtended
    {
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("date_created")] public string DateCreated { get; set; }
        [JsonProperty("video_file")] public string VideoFile { get; set; }
    }

    public class LabelingDataExtended
    {
        [JsonProperty("info")] public VideoInfoExtended Info { get; set; }
        [JsonProperty("licenses")] public List<object> Licenses { get; set; }
        [JsonProperty("images")] public List<ImageInfo> Images { get; set; }
        [JsonProperty("annotations")] public List<AnnotationData> Annotations { get; set; }
        [JsonProperty("categories")] public List<CategoryData> Categories { get; set; }
    }

    #endregion

    #region Legacy JSON Classes (하위 호환용)

    public class LegacyLabelingData
    {
        [JsonProperty("info")] public LegacyVideoInfo Info { get; set; }
        [JsonProperty("categories")] public List<LegacyCategory> Categories { get; set; }
        [JsonProperty("items")] public List<LegacyItem> Items { get; set; }
        [JsonProperty("annotations")] public List<LegacyAnnotation> Annotations { get; set; }
    }

    public class LegacyVideoInfo
    {
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("video_file")] public string VideoFile { get; set; }
    }

    public class LegacyCategory
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("labels")] public List<LegacyLabelDef> Labels { get; set; }
    }

    public class LegacyLabelDef
    {
        [JsonProperty("name")] public string Name { get; set; }
    }

    public class LegacyItem
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("frame_number")] public int FrameNumber { get; set; }
    }

    public class LegacyAnnotation
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("item_id")] public int ItemId { get; set; }
        [JsonProperty("category_id")] public int CategoryId { get; set; }
        [JsonProperty("bbox")] public int[] Bbox { get; set; }
        [JsonProperty("label")] public string Label { get; set; }
    }

    #endregion

    /// <summary>
    /// 카테고리 ID 매핑 (COCO 형식)
    /// </summary>
    public static class CategoryIdMap
    {
        public static readonly Dictionary<int, CategoryData> Default = new Dictionary<int, CategoryData>
        {
            { 1, new CategoryData { Id = 1, Name = "person", Supercategory = "person" } },
            { 2, new CategoryData { Id = 2, Name = "car", Supercategory = "vehicle" } },
            { 3, new CategoryData { Id = 3, Name = "bus", Supercategory = "vehicle" } },
            { 4, new CategoryData { Id = 4, Name = "truck", Supercategory = "vehicle" } },
            { 5, new CategoryData { Id = 5, Name = "motorcycle", Supercategory = "vehicle" } },
            { 6, new CategoryData { Id = 6, Name = "bicycle", Supercategory = "vehicle" } },
            { 7, new CategoryData { Id = 7, Name = "event", Supercategory = "event" } },
        };
    }
}
