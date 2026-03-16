using AOLTv1.Models;

namespace AOLTv1.Services
{
    /// <summary>
    /// Manual tracking service: forced inertia tracking (interpolation between keyframes)
    /// and disappearance handling for bounding boxes.
    /// </summary>
    public class TrackingService
    {
        #region State Dictionaries

        /// <summary>Failure ranges per waypoint key (label_id).</summary>
        public Dictionary<string, List<(int start, int end)>> WaypointFailureRanges { get; set; } = new();

        /// <summary>Whether inertia tracking is enabled per key.</summary>
        public Dictionary<string, bool> InertiaTrackingEnabled { get; set; } = new();

        /// <summary>Manually adjusted frame indices per key.</summary>
        public Dictionary<string, HashSet<int>> ManuallyAdjustedFrames { get; set; } = new();

        /// <summary>Start frames for forced inertia tracking per key.</summary>
        public Dictionary<string, int> ForcedInertiaTrackingStartFrames { get; set; } = new();

        /// <summary>Disappeared ranges per key (startFrame, endFrame?). Null endFrame = pending.</summary>
        public Dictionary<string, List<(int startFrame, int? endFrame)>> DisappearedRanges { get; set; } = new();

        /// <summary>Threshold for continuous absence detection (in frames).</summary>
        public int DisappearanceThreshold { get; set; } = 5;

        #endregion

        #region Delegates for Callbacks

        /// <summary>
        /// Delegate to retrieve the object ID (PersonId/VehicleId/EventId) from a BoundingBox.
        /// Must be provided by the caller.
        /// </summary>
        public Func<BoundingBox, int> GetBoxId { get; set; } = box =>
        {
            if (box.Label == "person") return box.PersonId;
            if (box.Label == "vehicle") return box.VehicleId;
            if (box.Label == "event") return box.EventId;
            return 0;
        };

        #endregion

        #region Forced Inertia Tracking

        /// <summary>
        /// Performs forced inertia tracking: interpolates bounding boxes between frame A and frame B,
        /// using manually adjusted keyframes as anchors.
        /// </summary>
        /// <param name="selectedBox">The selected bounding box (template).</param>
        /// <param name="aFrame">Start frame.</param>
        /// <param name="bFrame">End frame.</param>
        /// <param name="boundingBoxes">The global bounding box list (will be mutated).</param>
        /// <returns>
        /// A result containing the number of interpolated boxes and the boxes that were modified
        /// (for undo purposes), or an error message.
        /// </returns>
        public TrackingResult PerformForcedInertiaTracking(
            BoundingBox selectedBox,
            int aFrame,
            int bFrame,
            List<BoundingBox> boundingBoxes)
        {
            if (selectedBox == null || aFrame >= bFrame)
            {
                return TrackingResult.Error("잘못된 프레임 범위입니다.");
            }

            int boxId = GetBoxId(selectedBox);
            string key = $"{selectedBox.Label}_{boxId}";

            // Find box at frame A
            var boxA = boundingBoxes.FirstOrDefault(b =>
                b.FrameIndex == aFrame &&
                b.Label == selectedBox.Label &&
                GetBoxId(b) == boxId &&
                !b.IsDeleted);

            if (boxA == null)
            {
                return TrackingResult.Error($"프레임 {aFrame}에서 해당 박스를 찾을 수 없습니다.");
            }

            // Build success frames dictionary
            var successFrames = new Dictionary<int, Rectangle>();
            successFrames[aFrame] = boxA.Rectangle;

            // Add manually adjusted frames between A and B
            if (ManuallyAdjustedFrames.ContainsKey(key))
            {
                foreach (int adjustedFrame in ManuallyAdjustedFrames[key])
                {
                    if (adjustedFrame > aFrame && adjustedFrame <= bFrame)
                    {
                        var boxAtFrame = boundingBoxes.FirstOrDefault(b =>
                            b.FrameIndex == adjustedFrame &&
                            b.Label == selectedBox.Label &&
                            GetBoxId(b) == boxId &&
                            !b.IsDeleted);

                        if (boxAtFrame != null)
                        {
                            successFrames[adjustedFrame] = boxAtFrame.Rectangle;
                        }
                    }
                }
            }

            // Find or create box at frame B
            var boxB = boundingBoxes.FirstOrDefault(b =>
                b.FrameIndex == bFrame &&
                b.Label == selectedBox.Label &&
                GetBoxId(b) == boxId &&
                !b.IsDeleted);

            if (boxB == null && selectedBox.FrameIndex == bFrame)
            {
                boxB = selectedBox;
            }

            if (boxB != null)
            {
                successFrames[bFrame] = boxB.Rectangle;
            }
            else
            {
                // Copy from A
                var boxBFromA = CloneBoundingBox(boxA);
                boxBFromA.FrameIndex = bFrame;
                boxBFromA.Rectangle = boxA.Rectangle;

                boundingBoxes.Add(boxBFromA);
                boxB = boxBFromA;
                successFrames[bFrame] = boxB.Rectangle;
            }

            if (successFrames.Count < 2)
            {
                return TrackingResult.Error(
                    $"보간할 수 있는 성공 프레임이 부족합니다.\n\n" +
                    $"a 프레임: {aFrame}\nb 프레임: {bFrame}\n" +
                    $"성공 프레임: {successFrames.Count}개\n\n최소 2개의 성공 프레임이 필요합니다.");
            }

            var sortedSuccessFrames = successFrames.Keys.OrderBy(f => f).ToList();

            // Collect boxes to modify (for undo)
            var boxesToModify = new List<BoundingBox>();
            for (int frameIdx = aFrame + 1; frameIdx < bFrame; frameIdx++)
            {
                if (!successFrames.ContainsKey(frameIdx))
                {
                    var box = FindOrCreateBoxAtFrame(frameIdx, selectedBox, boundingBoxes);
                    boxesToModify.Add(CloneBoundingBox(box));
                }
            }

            // Apply interpolation
            int interpolatedCount = 0;
            for (int frameIdx = aFrame + 1; frameIdx < bFrame; frameIdx++)
            {
                if (successFrames.ContainsKey(frameIdx))
                    continue;

                var box = FindOrCreateBoxAtFrame(frameIdx, selectedBox, boundingBoxes);

                int? prevSuccess = FindPreviousSuccessFrame(frameIdx, sortedSuccessFrames);
                int? nextSuccess = FindNextSuccessFrame(frameIdx, sortedSuccessFrames);

                if (prevSuccess.HasValue && nextSuccess.HasValue)
                {
                    box.Rectangle = InterpolateRect(
                        successFrames[prevSuccess.Value],
                        successFrames[nextSuccess.Value],
                        prevSuccess.Value,
                        nextSuccess.Value,
                        frameIdx);
                    interpolatedCount++;
                }
                else if (prevSuccess.HasValue)
                {
                    box.Rectangle = successFrames[prevSuccess.Value];
                    interpolatedCount++;
                }
                else if (nextSuccess.HasValue)
                {
                    box.Rectangle = successFrames[nextSuccess.Value];
                    interpolatedCount++;
                }
            }

            return new TrackingResult
            {
                Success = true,
                InterpolatedCount = interpolatedCount,
                SuccessFrameCount = successFrames.Count,
                AFrame = aFrame,
                BFrame = bFrame,
                ModifiedBoxes = boxesToModify
            };
        }

        #endregion

        #region Interpolation Helpers

        /// <summary>
        /// Linear interpolation of rectangle position and size between two keyframes.
        /// </summary>
        public static Rectangle InterpolateRect(Rectangle prev, Rectangle next, int prevFrame, int nextFrame, int currentFrame)
        {
            double ratio = (double)(currentFrame - prevFrame) / (nextFrame - prevFrame);

            int x = (int)(prev.X + (next.X - prev.X) * ratio);
            int y = (int)(prev.Y + (next.Y - prev.Y) * ratio);
            int width = (int)(prev.Width + (next.Width - prev.Width) * ratio);
            int height = (int)(prev.Height + (next.Height - prev.Height) * ratio);

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// Finds an existing bounding box at the given frame or creates a new one from the template.
        /// </summary>
        public BoundingBox FindOrCreateBoxAtFrame(int frameIndex, BoundingBox templateBox, List<BoundingBox> boundingBoxes)
        {
            int boxId = GetBoxId(templateBox);

            var existing = boundingBoxes.FirstOrDefault(b =>
                b.FrameIndex == frameIndex &&
                b.Label == templateBox.Label &&
                GetBoxId(b) == boxId &&
                !b.IsDeleted);

            if (existing != null)
                return existing;

            var newBox = new BoundingBox
            {
                FrameIndex = frameIndex,
                Label = templateBox.Label,
                PersonId = templateBox.PersonId,
                VehicleId = templateBox.VehicleId,
                EventId = templateBox.EventId,
                Rectangle = templateBox.Rectangle,
                Action = "waypoint"
            };

            boundingBoxes.Add(newBox);
            return newBox;
        }

        private int? FindPreviousSuccessFrame(int currentFrame, List<int> sortedSuccessFrames)
        {
            for (int i = sortedSuccessFrames.Count - 1; i >= 0; i--)
            {
                if (sortedSuccessFrames[i] < currentFrame)
                    return sortedSuccessFrames[i];
            }
            return null;
        }

        private int? FindNextSuccessFrame(int currentFrame, List<int> sortedSuccessFrames)
        {
            foreach (var frame in sortedSuccessFrames)
            {
                if (frame > currentFrame)
                    return frame;
            }
            return null;
        }

        private BoundingBox CloneBoundingBox(BoundingBox box)
        {
            return new BoundingBox
            {
                FrameIndex = box.FrameIndex,
                Rectangle = new Rectangle(box.Rectangle.Location, box.Rectangle.Size),
                Label = box.Label,
                PersonId = box.PersonId,
                VehicleId = box.VehicleId,
                EventId = box.EventId,
                Action = box.Action,
                VehicleName = box.VehicleName,
                EventName = box.EventName
            };
        }

        #endregion

        #region Disappearance Handling

        /// <summary>
        /// Processes disappeared ranges at a specific return frame (called when re-tracking starts).
        /// Confirms pending disappearance ranges and marks boxes as deleted.
        /// </summary>
        public void ProcessDisappearedRangesAtFrame(
            WaypointMarker waypoint,
            int returnFrame,
            List<BoundingBox> boundingBoxes)
        {
            int boxId = waypoint.ObjectId;
            string key = $"{waypoint.Label}_{boxId}";

            if (!DisappearedRanges.ContainsKey(key))
                return;

            var pendingRanges = DisappearedRanges[key]
                .Where(r => !r.endFrame.HasValue && r.startFrame < returnFrame)
                .ToList();

            foreach (var pendingRange in pendingRanges)
            {
                int aFrame = pendingRange.startFrame;
                int endFrame = returnFrame - 1;

                if (endFrame >= aFrame)
                {
                    var index = DisappearedRanges[key].IndexOf(pendingRange);
                    DisappearedRanges[key][index] = (aFrame, endFrame);

                    System.Diagnostics.Debug.WriteLine($"[재추적 시점 복귀 감지] {key}: 프레임 {aFrame}~{endFrame} (복귀: {returnFrame})");

                    DeleteBoxesInRange(key, waypoint, aFrame, endFrame, boundingBoxes);
                }
            }
        }

        /// <summary>
        /// Processes all pending disappeared ranges for a waypoint and confirms them
        /// when a subsequent box is found.
        /// </summary>
        public void ProcessDisappearedRanges(
            WaypointMarker waypoint,
            List<BoundingBox> boundingBoxes)
        {
            int boxId = waypoint.ObjectId;
            string key = $"{waypoint.Label}_{boxId}";

            if (!DisappearedRanges.ContainsKey(key))
                return;

            var boxesInWaypoint = boundingBoxes
                .Where(b =>
                    b.Label == waypoint.Label &&
                    GetBoxId(b) == boxId &&
                    b.FrameIndex >= waypoint.EntryFrame &&
                    b.FrameIndex <= waypoint.ExitFrame &&
                    !b.IsDeleted)
                .OrderBy(b => b.FrameIndex)
                .ToList();

            var pendingRanges = DisappearedRanges[key]
                .Where(r => !r.endFrame.HasValue)
                .ToList();

            foreach (var pendingRange in pendingRanges)
            {
                int aFrame = pendingRange.startFrame;

                int? bFrame = boxesInWaypoint
                    .Where(b => b.FrameIndex > aFrame)
                    .Select(b => (int?)b.FrameIndex)
                    .FirstOrDefault();

                if (bFrame.HasValue && bFrame.Value > aFrame)
                {
                    int endFrame = bFrame.Value - 1;

                    var index = DisappearedRanges[key].IndexOf(pendingRange);
                    DisappearedRanges[key][index] = (aFrame, endFrame);

                    System.Diagnostics.Debug.WriteLine($"[사라짐 구간 확정] {key}: 프레임 {aFrame}~{endFrame} (복귀: {bFrame.Value})");

                    DeleteBoxesInRange(key, waypoint, aFrame, endFrame, boundingBoxes);
                }
            }
        }

        /// <summary>
        /// Detects continuous absence of bounding boxes within a waypoint range
        /// and marks those ranges as disappeared.
        /// </summary>
        public void DetectContinuousAbsence(
            WaypointMarker waypoint,
            List<BoundingBox> boundingBoxes)
        {
            int boxId = waypoint.ObjectId;
            string key = $"{waypoint.Label}_{boxId}";

            var boxesInWaypoint = boundingBoxes
                .Where(b =>
                    b.Label == waypoint.Label &&
                    GetBoxId(b) == boxId &&
                    b.FrameIndex >= waypoint.EntryFrame &&
                    b.FrameIndex <= waypoint.ExitFrame &&
                    !b.IsDeleted)
                .Select(b => b.FrameIndex)
                .OrderBy(f => f)
                .ToList();

            // Find empty frame ranges
            var emptyRanges = new List<(int start, int end)>();
            int currentStart = -1;

            for (int frame = waypoint.EntryFrame; frame <= waypoint.ExitFrame; frame++)
            {
                bool hasBox = boxesInWaypoint.Contains(frame);

                if (!hasBox && currentStart == -1)
                {
                    currentStart = frame;
                }
                else if (hasBox && currentStart != -1)
                {
                    int end = frame - 1;
                    if (end >= currentStart)
                    {
                        emptyRanges.Add((currentStart, end));
                    }
                    currentStart = -1;
                }
            }

            // Handle trailing empty range
            if (currentStart != -1)
            {
                emptyRanges.Add((currentStart, waypoint.ExitFrame));
            }

            // Process ranges that exceed the threshold
            foreach (var emptyRange in emptyRanges)
            {
                int duration = emptyRange.end - emptyRange.start + 1;

                if (duration >= DisappearanceThreshold)
                {
                    bool alreadyProcessed = DisappearedRanges.ContainsKey(key) &&
                        DisappearedRanges[key].Any(r =>
                            r.startFrame == emptyRange.start &&
                            r.endFrame.HasValue &&
                            r.endFrame.Value == emptyRange.end);

                    if (!alreadyProcessed)
                    {
                        System.Diagnostics.Debug.WriteLine($"[연속 부재 감지] {key}: 프레임 {emptyRange.start}~{emptyRange.end} ({duration}프레임)");

                        if (!DisappearedRanges.ContainsKey(key))
                        {
                            DisappearedRanges[key] = new List<(int, int?)>();
                        }
                        DisappearedRanges[key].Add((emptyRange.start, emptyRange.end));

                        DeleteBoxesInRange(key, waypoint, emptyRange.start, emptyRange.end, boundingBoxes);
                    }
                }
            }
        }

        /// <summary>
        /// Marks boxes in the given frame range as deleted.
        /// </summary>
        private void DeleteBoxesInRange(
            string key,
            WaypointMarker waypoint,
            int startFrame,
            int endFrame,
            List<BoundingBox> boundingBoxes)
        {
            int boxId = waypoint.ObjectId;
            int deletedCount = 0;

            var boxesToDelete = boundingBoxes
                .Where(b =>
                    b.Label == waypoint.Label &&
                    GetBoxId(b) == boxId &&
                    b.FrameIndex >= startFrame &&
                    b.FrameIndex <= endFrame &&
                    !b.IsDeleted)
                .ToList();

            foreach (var box in boxesToDelete)
            {
                box.IsDeleted = true;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[박스 삭제] {key}: 프레임 {startFrame}~{endFrame}에서 {deletedCount}개 박스 삭제");
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of a forced inertia tracking operation.
    /// </summary>
    public class TrackingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int InterpolatedCount { get; set; }
        public int SuccessFrameCount { get; set; }
        public int AFrame { get; set; }
        public int BFrame { get; set; }
        public List<BoundingBox>? ModifiedBoxes { get; set; }

        public static TrackingResult Error(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
