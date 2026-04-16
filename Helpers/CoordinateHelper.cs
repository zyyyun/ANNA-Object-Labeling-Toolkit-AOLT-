namespace ASLTv1.Helpers
{
    /// <summary>
    /// Static helper methods for coordinate transformations between image space and
    /// PictureBox (view) space, accounting for Zoom mode aspect ratio scaling.
    /// </summary>
    public static class CoordinateHelper
    {
        /// <summary>
        /// Calculates the actual display rectangle of the image within a PictureBox
        /// when using Zoom sizing mode (maintains aspect ratio with letterboxing).
        /// </summary>
        public static RectangleF GetImageDisplayRectangle(PictureBox pictureBox)
        {
            if (pictureBox.Image == null)
                return RectangleF.Empty;

            float imageAspect = (float)pictureBox.Image.Width / pictureBox.Image.Height;
            float controlAspect = (float)pictureBox.Width / pictureBox.Height;

            float renderWidth, renderHeight;
            float renderX = 0, renderY = 0;

            if (imageAspect > controlAspect)
            {
                // Image is wider -- fit to width, letterbox top/bottom
                renderWidth = pictureBox.Width;
                renderHeight = pictureBox.Width / imageAspect;
                renderY = (pictureBox.Height - renderHeight) / 2f;
            }
            else
            {
                // Image is taller -- fit to height, letterbox left/right
                renderHeight = pictureBox.Height;
                renderWidth = pictureBox.Height * imageAspect;
                renderX = (pictureBox.Width - renderWidth) / 2f;
            }

            return new RectangleF(renderX, renderY, renderWidth, renderHeight);
        }

        /// <summary>
        /// Converts a point from image coordinates to PictureBox (view) coordinates.
        /// </summary>
        public static PointF ImageToView(PointF imagePoint, PictureBox pictureBox)
        {
            if (pictureBox.Image == null)
                return imagePoint;

            var displayRect = GetImageDisplayRectangle(pictureBox);

            float scaleX = displayRect.Width / pictureBox.Image.Width;
            float scaleY = displayRect.Height / pictureBox.Image.Height;

            return new PointF(
                displayRect.X + imagePoint.X * scaleX,
                displayRect.Y + imagePoint.Y * scaleY
            );
        }

        /// <summary>
        /// Converts a rectangle from image coordinates to PictureBox (view) coordinates.
        /// </summary>
        public static RectangleF ImageToView(RectangleF imageRect, PictureBox pictureBox)
        {
            var topLeft = ImageToView(new PointF(imageRect.X, imageRect.Y), pictureBox);
            var bottomRight = ImageToView(new PointF(imageRect.Right, imageRect.Bottom), pictureBox);

            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );
        }

        /// <summary>
        /// Converts a point from PictureBox (view) coordinates to image coordinates.
        /// </summary>
        public static PointF ViewToImage(PointF viewPoint, PictureBox pictureBox)
        {
            if (pictureBox.Image == null)
                return viewPoint;

            var displayRect = GetImageDisplayRectangle(pictureBox);

            float scaleX = pictureBox.Image.Width / displayRect.Width;
            float scaleY = pictureBox.Image.Height / displayRect.Height;

            return new PointF(
                (viewPoint.X - displayRect.X) * scaleX,
                (viewPoint.Y - displayRect.Y) * scaleY
            );
        }

        /// <summary>
        /// Converts a rectangle from PictureBox (view) coordinates to image coordinates.
        /// </summary>
        public static RectangleF ViewToImage(RectangleF viewRect, PictureBox pictureBox)
        {
            var topLeft = ViewToImage(new PointF(viewRect.X, viewRect.Y), pictureBox);
            var bottomRight = ViewToImage(new PointF(viewRect.Right, viewRect.Bottom), pictureBox);

            return new RectangleF(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y
            );
        }

        /// <summary>
        /// 바운딩 박스 좌표를 이미지 범위 내로 클램핑합니다.
        /// </summary>
        public static Rectangle ClampToImage(Rectangle rect, int imageWidth, int imageHeight)
        {
            int x = Math.Max(0, Math.Min(rect.X, imageWidth - 1));
            int y = Math.Max(0, Math.Min(rect.Y, imageHeight - 1));
            int w = Math.Min(rect.Width, imageWidth - x);
            int h = Math.Min(rect.Height, imageHeight - y);
            return new Rectangle(x, y, Math.Max(1, w), Math.Max(1, h));
        }
    }
}
