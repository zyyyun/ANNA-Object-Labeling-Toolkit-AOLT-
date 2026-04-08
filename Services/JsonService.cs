using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AOLTv1.Models;

namespace AOLTv1.Services
{
    /// <summary>
    /// Handles loading and exporting labeling data in COCO-like JSON format.
    /// JSON 라벨링 데이터 로드/내보내기 서비스.
    /// </summary>
    public class JsonService
    {
        #region Category ID Mapping

        private static readonly Dictionary<string, int> CategoryNameToIdMap = new()
        {
            // Person categories (1~20)
            {"person_01", 1}, {"person_02", 2}, {"person_03", 3}, {"person_04", 4},
            {"person_05", 5}, {"person_06", 6}, {"person_07", 7}, {"person_08", 8},
            {"person_09", 9}, {"person_10", 10}, {"person_11", 11}, {"person_12", 12},
            {"person_13", 13}, {"person_14", 14}, {"person_15", 15}, {"person_16", 16},
            {"person_17", 17}, {"person_18", 18}, {"person_19", 19}, {"person_20", 20},

            // Vehicle categories (21~24)
            {"car", 21}, {"motorcycle", 22}, {"e_scooter", 23}, {"bicycle", 24},

            // Event categories (25~34)
            {"event_hazard", 25}, {"event_accident", 26}, {"event_damage", 27}, {"event_fire", 28},
            {"event_intrusion", 29}, {"event_leak", 30}, {"event_failure", 31}, {"event_lost_object", 32},
            {"event_fall", 33}, {"event_abnormal_behavior", 34}
        };

        private static readonly Color[] MarkerColors = new Color[]
        {
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(16, 185, 129),
            Color.FromArgb(139, 92, 246),
            Color.FromArgb(245, 158, 11),
            Color.FromArgb(236, 72, 153),
        };

        #endregion

        #region Helper Methods

        public static int GetBoxId(BoundingBox box)
        {
            if (box.Label == "person") return box.PersonId;
            if (box.Label == "vehicle") return box.VehicleId;
            if (box.Label == "event") return box.EventId;
            return 0;
        }

        private static int GetCategoryId(string label, int boxId)
        {
            string categoryName = GetCategoryName(label, boxId);

            if (CategoryNameToIdMap.ContainsKey(categoryName))
                return CategoryNameToIdMap[categoryName];

            if (label == "person") return Math.Min(boxId, 20);
            if (label == "vehicle") return Math.Min(21 + (boxId - 1), 24);
            if (label == "event") return Math.Min(25 + (boxId - 1), 34);

            return boxId;
        }

        private static string GetCategoryName(string label, int boxId)
        {
            if (label == "person")
            {
                return $"person_{boxId:D2}";
            }
            else if (label == "vehicle")
            {
                return boxId switch
                {
                    1 => "car",
                    2 => "motorcycle",
                    3 => "e_scooter",
                    4 => "bicycle",
                    _ => "car"
                };
            }
            else if (label == "event")
            {
                return boxId switch
                {
                    1 => "event_hazard",
                    2 => "event_accident",
                    3 => "event_damage",
                    4 => "event_fire",
                    5 => "event_intrusion",
                    6 => "event_leak",
                    7 => "event_failure",
                    8 => "event_lost_object",
                    9 => "event_fall",
                    10 => "event_abnormal_behavior",
                    _ => "event_hazard"
                };
            }

            return $"{label}_{boxId:D2}";
        }

        #endregion

        #region Resolve JSON Path

        /// <summary>
        /// Resolves the _labels.json file path for a given video file.
        /// Returns null if no JSON file exists.
        /// </summary>
        public string? ResolveJsonPath(string videoFilePath)
        {
            string? videoDir = Path.GetDirectoryName(videoFilePath);
            if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                return null;

            string saveDir = Path.Combine(videoDir, "labels");
            if (!Directory.Exists(saveDir))
                return null;

            string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
            string normalPath = Path.Combine(saveDir, baseFileName + "_labels.json");

            if (File.Exists(normalPath))
                return normalPath;

            return null;
        }

        #endregion

        #region Load JSON

        /// <summary>
        /// Result of loading labeling data from a JSON file.
        /// </summary>
        public class LoadResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public string? LoadedFilePath { get; set; }
            public List<BoundingBox> BoundingBoxes { get; set; } = new();
            public List<WaypointMarker> WaypointMarkers { get; set; } = new();
            public Dictionary<int, CategoryData> CategoryMap { get; set; } = new();
            public Dictionary<int, string> FrameTimestampMap { get; set; } = new();
            public int NextAnnotationId { get; set; } = 1;
        }

        /// <summary>
        /// Loads labeling data from the _labels.json file associated with a video.
        /// </summary>
        /// <param name="videoFilePath">Path to the video file.</param>
        /// <param name="fps">Video FPS (for formatting waypoint times).</param>
        /// <param name="progressCallback">Optional callback for progress updates (message).</param>
        public async Task<LoadResult> LoadLabelingDataAsync(
            string videoFilePath,
            double fps,
            Action<string>? progressCallback = null)
        {
            var result = new LoadResult();

            try
            {
                string? videoDir = Path.GetDirectoryName(videoFilePath);
                if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                string saveDir = Path.Combine(videoDir, "labels");
                if (!Directory.Exists(saveDir))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
                string normalPath = Path.Combine(saveDir, baseFileName + "_labels.json");

                if (!File.Exists(normalPath))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                string loadPath = normalPath;
                result.LoadedFilePath = loadPath;

                // File size check
                FileInfo fileInfo = new FileInfo(loadPath);
                long fileSizeMB = fileInfo.Length / (1024 * 1024);

                // Create backup
                string backupPath = loadPath + ".backup";
                try
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Copy(loadPath, backupPath);
                }
                catch (Exception backupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[JSON 로드] 백업 파일 생성 실패: {backupEx.Message}");
                }

                // Read and parse JSON
                progressCallback?.Invoke("JSON 파일 읽는 중...");

                LabelingDataExtended? labelingData = null;

                await Task.Run(() =>
                {
                    using var fileStream = new FileStream(loadPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                    using var streamReader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true, 8192);

                    string json = streamReader.ReadToEnd();

                    progressCallback?.Invoke("JSON 파싱 중...");

                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    };
                    labelingData = JsonConvert.DeserializeObject<LabelingDataExtended>(json, settings);
                });

                if (labelingData?.Annotations == null)
                    return result;

                // ImageId -> FrameNumber mapping
                var imageIdToFrameNumber = new Dictionary<int, int>();
                if (labelingData.Images != null)
                {
                    foreach (var image in labelingData.Images)
                    {
                        imageIdToFrameNumber[image.Id] = image.FrameNumber;
                        if (!string.IsNullOrEmpty(image.Timestamp))
                        {
                            result.FrameTimestampMap[image.FrameNumber] = image.Timestamp;
                        }
                    }
                }

                if (labelingData.Categories != null)
                {
                    foreach (var category in labelingData.Categories)
                    {
                        result.CategoryMap[category.Id] = category;
                    }
                }

                // Process annotations
                progressCallback?.Invoke("데이터 처리 중...");

                var waypointKeySet = new Dictionary<string, WaypointMarker>();

                foreach (var annotation in labelingData.Annotations)
                {
                    if (annotation.Bbox == null || annotation.Bbox.Length < 4)
                        continue;

                    int trackId = annotation.TrackId;
                    string label = "person";

                    int catId = annotation.CategoryId;
                    if (catId >= 1 && catId <= 20)
                        label = "person";
                    else if (catId >= 21 && catId <= 24)
                        label = "vehicle";
                    else if (catId >= 25 && catId <= 34)
                        label = "event";
                    else if (result.CategoryMap.ContainsKey(catId))
                    {
                        string categoryName = result.CategoryMap[catId].Name;
                        if (categoryName.Contains("car") || categoryName.Contains("motorcycle") ||
                            categoryName.Contains("scooter") || categoryName.Contains("bicycle"))
                            label = "vehicle";
                        else if (categoryName.StartsWith("event_"))
                            label = "event";
                        else if (categoryName.StartsWith("person"))
                            label = "person";
                    }

                    int actualFrameNumber = annotation.ImageId;
                    if (imageIdToFrameNumber.ContainsKey(annotation.ImageId))
                        actualFrameNumber = imageIdToFrameNumber[annotation.ImageId];

                    int personId = 0, vehicleId = 0, eventId = 0;

                    if (label == "person")
                        personId = trackId;
                    else if (label == "vehicle")
                        vehicleId = catId >= 21 && catId <= 24 ? (catId - 20) : trackId;
                    else if (label == "event")
                        eventId = catId >= 25 && catId <= 28 ? (catId - 24) : trackId;

                    var box = new BoundingBox
                    {
                        FrameIndex = actualFrameNumber,
                        Rectangle = new Rectangle(annotation.Bbox[0], annotation.Bbox[1], annotation.Bbox[2], annotation.Bbox[3]),
                        Label = label,
                        PersonId = personId,
                        VehicleId = vehicleId,
                        EventId = eventId,
                        Action = "waypoint"
                    };

                    result.BoundingBoxes.Add(box);

                    if (annotation.Id >= result.NextAnnotationId)
                        result.NextAnnotationId = annotation.Id + 1;

                    // Restore waypoints
                    if (annotation.TrackInfo?.Entry != null && annotation.TrackInfo?.Exit != null)
                    {
                        int entryFrame = annotation.TrackInfo.Entry.Frame;
                        int exitFrame = annotation.TrackInfo.Exit.Frame;

                        int objectId = 0;
                        if (box.Label == "person") objectId = box.PersonId;
                        else if (box.Label == "vehicle") objectId = box.VehicleId;
                        else if (box.Label == "event") objectId = box.EventId;

                        string waypointKey = $"{box.Label}_{objectId}_{entryFrame}_{exitFrame}";

                        Color waypointColor;
                        if (box.Label == "person")
                            waypointColor = Color.FromArgb(255, 107, 107);
                        else if (box.Label == "vehicle")
                            waypointColor = Color.FromArgb(107, 158, 255);
                        else if (box.Label == "event")
                            waypointColor = Color.FromArgb(107, 255, 107);
                        else
                            waypointColor = Color.Black;

                        var waypoint = new WaypointMarker
                        {
                            ObjectId = objectId,
                            Label = box.Label,
                            EntryFrame = entryFrame,
                            ExitFrame = exitFrame,
                            EntryTime = FormatFrameTime(entryFrame, fps),
                            ExitTime = FormatFrameTime(exitFrame, fps),
                            MarkerColor = waypointColor,
                            InteractingObject = (box.Label == "event") ? (annotation.InteractingObject ?? "") : null
                        };

                        if (!waypointKeySet.ContainsKey(waypointKey))
                        {
                            waypointKeySet[waypointKey] = waypoint;
                            result.WaypointMarkers.Add(waypoint);
                        }
                        else
                        {
                            if (box.Label == "event" && !string.IsNullOrWhiteSpace(annotation.InteractingObject))
                            {
                                var existing = waypointKeySet[waypointKey];
                                if (string.IsNullOrWhiteSpace(existing.InteractingObject))
                                    existing.InteractingObject = annotation.InteractingObject;
                            }
                        }
                    }
                }

                // Fix uncolored waypoints
                int waypointIndex = 0;
                foreach (var waypoint in result.WaypointMarkers)
                {
                    if (waypoint.MarkerColor == Color.Black)
                    {
                        waypoint.MarkerColor = MarkerColors[waypointIndex % MarkerColors.Length];
                    }
                    waypointIndex++;
                }

                result.Success = true;
            }
            catch (OutOfMemoryException oomEx)
            {
                result.Success = false;
                result.ErrorMessage = $"메모리 부족으로 파일을 로드할 수 없습니다.\n{oomEx.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"라벨링 데이터 로드 오류: {ex.Message}";
                if (ex.InnerException != null)
                    result.ErrorMessage += $"\n\n상세 정보: {ex.InnerException.Message}";
            }

            return result;
        }

        #endregion

        #region Export JSON

        /// <summary>
        /// Exports annotation data to a COCO-format JSON file.
        /// </summary>
        public void ExportToJsonExtended(
            string filePath,
            string currentVideoFile,
            double fps,
            int frameWidth,
            int frameHeight,
            List<BoundingBox> boundingBoxes,
            List<WaypointMarker> waypointMarkers,
            VideoService? videoService = null)
        {
            try
            {
                var images = new List<ImageInfo>();
                var annotations = new List<AnnotationData>();
                var categories = new Dictionary<int, CategoryData>();

                var frameGroups = boundingBoxes.Where(b => !b.IsDeleted).GroupBy(b => b.FrameIndex).OrderBy(g => g.Key);
                int imageId = 0;
                int nextAnnotationId = 1;

                foreach (var frameGroup in frameGroups)
                {
                    double frameSeconds = frameGroup.Key / fps;
                    DateTime frameTime = DateTime.Now.AddSeconds(frameSeconds);

                    string? subtitleTimestamp = videoService?.GetSubtitleTimestampForFrame(frameGroup.Key);
                    string timestamp = subtitleTimestamp ?? frameTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");

                    var imageInfo = new ImageInfo
                    {
                        Id = imageId,
                        Height = frameHeight,
                        Width = frameWidth,
                        FrameNumber = frameGroup.Key,
                        Timestamp = timestamp
                    };

                    images.Add(imageInfo);

                    foreach (var box in frameGroup)
                    {
                        int boxId = GetBoxId(box);
                        int categoryId = GetCategoryId(box.Label, boxId);
                        string categoryName = GetCategoryName(box.Label, boxId);

                        if (!categories.ContainsKey(categoryId))
                        {
                            categories[categoryId] = new CategoryData
                            {
                                Id = categoryId,
                                Name = categoryName,
                                Supercategory = box.Label
                            };
                        }

                        // Find matching waypoint
                        var matchingWaypoint = waypointMarkers.FirstOrDefault(w =>
                            w.Label == box.Label &&
                            w.ObjectId == boxId &&
                            box.FrameIndex >= w.EntryFrame &&
                            box.FrameIndex <= w.ExitFrame);

                        int entryFrame = box.FrameIndex;
                        int exitFrame = box.FrameIndex;

                        if (matchingWaypoint != null)
                        {
                            entryFrame = matchingWaypoint.EntryFrame;
                            exitFrame = matchingWaypoint.ExitFrame;
                        }
                        else
                        {
                            var sameObjectBoxes = boundingBoxes
                                .Where(b => b.Label == box.Label && GetBoxId(b) == boxId)
                                .ToList();

                            if (sameObjectBoxes.Any())
                            {
                                entryFrame = sameObjectBoxes.Min(b => b.FrameIndex);
                                exitFrame = sameObjectBoxes.Max(b => b.FrameIndex);
                            }
                        }

                        string? entryTimestamp = videoService?.GetSubtitleTimestampForFrame(entryFrame);
                        string? exitTimestamp = videoService?.GetSubtitleTimestampForFrame(exitFrame);

                        double entrySeconds = entryFrame / fps;
                        double exitSeconds = exitFrame / fps;
                        DateTime entryTime = DateTime.Now.AddSeconds(entrySeconds);
                        DateTime exitTime = DateTime.Now.AddSeconds(exitSeconds);

                        var annotation = new AnnotationData
                        {
                            Id = nextAnnotationId++,
                            ImageId = imageId,
                            CategoryId = categoryId,
                            Bbox = new int[] { box.Rectangle.X, box.Rectangle.Y, box.Rectangle.Width, box.Rectangle.Height },
                            Area = box.Rectangle.Width * box.Rectangle.Height,
                            Iscrowd = 0,
                            TrackId = boxId,
                            TrackInfo = new TrackInfo
                            {
                                Entry = new TrackEntry
                                {
                                    Frame = entryFrame,
                                    Timestamp = entryTimestamp ?? entryTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                                },
                                Exit = new TrackEntry
                                {
                                    Frame = exitFrame,
                                    Timestamp = exitTimestamp ?? exitTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                                },
                                CurrentClipCount = 1
                            }
                        };

                        // Event interacting object
                        if (box.Label == "event" && matchingWaypoint != null && !string.IsNullOrWhiteSpace(matchingWaypoint.InteractingObject))
                        {
                            annotation.InteractingObject = matchingWaypoint.InteractingObject;
                        }

                        annotations.Add(annotation);
                    }

                    imageId++;
                }

                var labelingData = new LabelingDataExtended
                {
                    Info = new VideoInfoExtended
                    {
                        Description = "Extended COCO with Tracking",
                        Version = "1.0",
                        Year = DateTime.Now.Year,
                        DateCreated = DateTime.Now.ToString("yyyy-MM-dd"),
                        VideoFile = Path.GetFileName(currentVideoFile)
                    },
                    Licenses = new List<object>(),
                    Images = images,
                    Annotations = annotations,
                    Categories = categories.Values.ToList()
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.None
                };
                string json = JsonConvert.SerializeObject(labelingData, settings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JSON 내보내기 오류: {ex.Message}", ex);
            }
        }

        #endregion

        #region Delete JSON

        /// <summary>
        /// Deletes the JSON file for a given video.
        /// </summary>
        /// <returns>True if a file was deleted, false if no file existed.</returns>
        public bool DeleteJsonFileForVideo(string videoFilePath, string? currentJsonFile = null)
        {
            string? videoDir = Path.GetDirectoryName(videoFilePath);
            if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                return false;

            string saveDir = Path.Combine(videoDir, "labels");

            string? jsonPath = null;

            if (!string.IsNullOrEmpty(currentJsonFile) && File.Exists(currentJsonFile))
            {
                jsonPath = currentJsonFile;
            }
            else
            {
                string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
                string normalPath = Path.Combine(saveDir, baseFileName + "_labels.json");

                if (File.Exists(normalPath))
                    jsonPath = normalPath;
            }

            if (jsonPath == null)
                return false;

            File.Delete(jsonPath);
            return true;
        }

        #endregion

        #region Private Helpers

        private static string FormatFrameTime(int frameIndex, double fps)
        {
            if (fps <= 0) return "00:00:00";
            TimeSpan time = TimeSpan.FromSeconds(frameIndex / fps);
            return time.ToString(@"hh\:mm\:ss");
        }

        #endregion
    }
}
