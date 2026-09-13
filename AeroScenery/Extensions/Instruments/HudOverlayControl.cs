using System;
using System.Drawing;
using System.Windows.Forms;

namespace AeroScenery.Extensions.Instruments
{
    public class HudOverlayControl : UserControl
    {
        public double Pitch { get; set; } // In degrees, e.g. -10 to +10
        public double Roll { get; set; }  // In degrees, e.g. -45 to +45

        public double AltitudeFt { get; set; }
        public double SpeedKt { get; set; }
        public double VerticalSpeedFtM { get; set; }
        public double HeadingDeg { get; set; }
        public double ElevationFt { get; set; }

        public HudOverlayControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint|
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int width = Width;
            int height = Height;
            float centerX = width / 2f;
            float centerY = height / 2f;

            //float pitchOffset = (float)(-Pitch * 7.0); 
            float pitchOffset = (float)(Pitch * 7.0); // Pitch scaling (pixels per degree): 7 instead of 5 
            float rollAngle = (float)(-Roll); // anti-clockwise

            using (var pen = new Pen(Color.White, 2)) // Color.White instead of Color.LimeGreen
            {
                // ==== 1. Pitch ladder with rolling rotation ====

                g.TranslateTransform(centerX, centerY);
                g.RotateTransform(rollAngle);
                g.TranslateTransform(0, pitchOffset);

                // brown surface as a backdrop for the artificial horizon
                using (Brush brownBrush = new SolidBrush(Color.FromArgb(200, 160, 82, 45)))
                {
                    //var rectBrown = new RectangleF(-Width, 0, Width * 2, Height);
                    var rectBrown = new RectangleF(-Width, 0, Width * 2, Height * 4);
                    g.FillRectangle(brownBrush, rectBrown);
                }

                for (int pitch = -85; pitch <= 85; pitch += 5) // +/- 85 statt 45
                {
                    if (pitch == 0) continue; // The centre will be drawn in later

                    float y = -pitch * 7.0f; // vertical position: 7 instead of 5

                    int lineLength = (pitch % 10 == 0) ? 60 : 40;
                    int notchSize = 10;

                    // Main line
                    g.DrawLine(pen, -lineLength, y, lineLength, y);

                    // Fold mark: small angle left/right
                    if (pitch > 0)
                    {
                        // Climb: Lines pointing downwards
                        g.DrawLine(pen, -lineLength, y, -lineLength + notchSize, y + notchSize);
                        g.DrawLine(pen, lineLength, y, lineLength - notchSize, y + notchSize);
                    }
                    else
                    {
                        // Descent: Lines pointing upwards
                        g.DrawLine(pen, -lineLength, y, -lineLength + notchSize, y - notchSize);
                        g.DrawLine(pen, lineLength, y, lineLength - notchSize, y - notchSize);
                    }

                    // Pitch value
                    //string label = $"{Math.Abs(pitch)}°";
                    string label = $"{pitch}°"; // Also display negative values during descent 
                    var font = new Font("Arial", 10);
                    var size = g.MeasureString(label, font);
                    g.DrawString(label, font, Brushes.White, -lineLength - size.Width - 5, y - size.Height / 2); // Brushes.White statt Brushes.LimeGreen
                    g.DrawString(label, font, Brushes.White, lineLength + 5, y - size.Height / 2);
                }

                // ==== 2. horizon line ====
                g.DrawLine(pen, -100, 0, 100, 0);

                g.ResetTransform();


                // ROLL CIRCLE (centred at the top)
                float rollRadius = 100f;
                float rollTopMargin = 24; // Distance from the top edge: 24 used instead of 60
                PointF centerTop = new PointF(centerX, rollTopMargin + rollRadius);

                using (var rollPen = new Pen(Color.Black, 2)) // Color.Black instead of Color.DarkGreen
                {
                    g.DrawArc(rollPen, centerTop.X - rollRadius, centerTop.Y - rollRadius, rollRadius * 2, rollRadius * 2, 180, 180);


                    // Markings for typical bank angles
                    int[] bankAngles = { -90, -60, -45, -30, -20, -10, 0, 10, 20, 30, 45, 60, 90 };
                    foreach (int angle in bankAngles)
                    {
                        double rad = angle * Math.PI / 180;
                        float x1 = centerTop.X + (float)(rollRadius * Math.Sin(rad));
                        float y1 = centerTop.Y - (float)(rollRadius * Math.Cos(rad));
                        float len = (angle % 30 == 0) ? 10 : 6;

                        float x2 = centerTop.X + (float)((rollRadius - len) * Math.Sin(rad));
                        float y2 = centerTop.Y - (float)((rollRadius - len) * Math.Cos(rad));

                        g.DrawLine(rollPen, x1, y1, x2, y2);
                    }

                    // ... after drawing the pitch lines, etc.
                    //g.ResetTransform(); // ⬅ Important! Resetting the rotation

                    // Marker angle in radians
                    double rollRad = Roll * Math.PI / 180.0;

                    // Position on the circle (pointing from the centre outwards)
                    float markerInnerRadius = rollRadius - 20;
                    float markerOuterRadius = rollRadius - 5;

                    // Calculate three points
                    PointF p1 = new PointF(
                        centerTop.X + (float)(markerOuterRadius * Math.Sin(rollRad)),
                        centerTop.Y - (float)(markerOuterRadius * Math.Cos(rollRad)));

                    PointF p2 = new PointF(
                        centerTop.X + (float)(markerInnerRadius * Math.Sin(rollRad + 0.05)),
                        centerTop.Y - (float)(markerInnerRadius * Math.Cos(rollRad + 0.05)));

                    PointF p3 = new PointF(
                        centerTop.X + (float)(markerInnerRadius * Math.Sin(rollRad - 0.05)),
                        centerTop.Y - (float)(markerInnerRadius * Math.Cos(rollRad - 0.05)));

                    // Draw a red triangle
                    using (var redBrush = new SolidBrush(Color.Red))
                    {
                        g.FillPolygon(redBrush, new[] { p1, p2, p3 });
                    }

                    // Roll scale markings in red (on the outside of the semicircle)
                    using (var font = new Font("Arial", 9, FontStyle.Bold))
                    using (var redBrush = new SolidBrush(Color.LightGray))
                    {
                        int[] rollMarks = new int[] { -90, -60, -45, -30, -20, -10, 0, 10, 20, 30, 45, 60, 90 };

                        float labelRadius = rollRadius + 12; // Just outside the semicircle

                        foreach (int angle in rollMarks)
                        {
                            double rad = angle * Math.PI / 180.0;

                            // Calculate position
                            float x = centerTop.X + (float)(labelRadius * Math.Sin(rad));
                            float y = centerTop.Y - (float)(labelRadius * Math.Cos(rad));

                            string label = angle.ToString();

                            // Text size for centring
                            SizeF textSize = g.MeasureString(label, font);
                            g.DrawString(label, font, redBrush, x - textSize.Width / 2, y - textSize.Height / 2);
                        }
                    }



                    // ==== 3. Central aeroplane symbol ====
                    float fuselageRadius = 6;
                    g.FillEllipse(Brushes.Black, centerX - fuselageRadius, centerY - fuselageRadius, fuselageRadius * 2, fuselageRadius * 2);
                    g.DrawLine(rollPen, centerX - 30, centerY, centerX + 30, centerY); // Flügel
                    g.DrawLine(rollPen, centerX, centerY - 15, centerX, centerY); // Heckleitwerk
                   
                }

                // Formatting function
                string FormatValue(double value, string unit = "") => $"{Math.Round(value)}{unit}";

                // Distances & Dimensions
                int marginSide = 10;
                int marginBottom = 8;
                int marginTop = 4;
                // int spacingY = 38;
                int boxWidth = 80;
                int boxHeight = 22;

                // Positions
                int posXLeft = marginSide;
                int posYTop = marginTop;
                int posXRight = Width - boxWidth - marginSide;
                int posYRightTop = marginTop;
                int posYBottom = Height - boxHeight - marginBottom;
                int posYRightBottom = Height - boxHeight - marginBottom;

                // Drawing materials
                using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                using (var bgBrush = new SolidBrush(Color.FromArgb(160, 80, 80, 80))) // halbtransparent grau
                using (var penBox = new Pen(Color.White, 1))
                {
                    // Drawing method for label + rectangle
                    void DrawHudBox(string label, string value, int x, int y)
                    {
                        // Label
                        g.DrawString(label, font, textBrush, x, y);

                        // Rectangle with text underneath
                        var rect = new Rectangle(x, y + 14, boxWidth, boxHeight);
                        g.FillRectangle(bgBrush, rect);
                        g.DrawRectangle(penBox, rect);
                        g.DrawString(value, font, textBrush, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }

                    // SPEED (top left)
                    DrawHudBox("SPD", FormatValue(SpeedKt, " kt"), posXLeft, posYTop);

                    // HDG (bottom left)
                    DrawHudBox("HDG", FormatValue(HeadingDeg, "°"), posXLeft, posYBottom - 14);

                    // ALT (top right)
                    DrawHudBox("ALT", FormatValue(AltitudeFt, " ft"), posXRight, posYRightTop);

                    // VS (bottom right)
                    DrawHudBox("VS", FormatValue(VerticalSpeedFtM, " ft/m"), posXRight, posYRightBottom - 14);

                    // AGL (above VS), only if available
                    if (ElevationFt > -100)
                    {
                        DrawHudBox("AGL", FormatValue(AltitudeFt - ElevationFt, " ft"), posXRight, posYRightBottom - 56);
                    }
                }



            }
        }

        /*
        protected override void OnPaint2(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = Width;
            var height = Height;
            
            // Fixe seitliche Marker links/rechts
            int margin = 20;

            // Horizontlinie (Pitch und Roll)
            float centerX = width / 2f;
            float centerY = height / 2f;

            // Pitch verschiebt Linie vertikal (max ±10 Grad → z. B. ±50 px)
            float pitchOffset = (float)(-Pitch * 3.0);  // z. B. ±50 Pixel

            // Roll dreht die Linie
            float rollAngle = (float)(-Roll); // Gegenuhrzeigersinn
       
            using (var pen = new Pen(Color.Black, 2))
            {
                // Zeichenbefehle hier drin
                g.DrawLine(pen, margin, 0, margin, height);
                g.DrawLine(pen, width - margin, 0, width - margin, height);

                g.TranslateTransform(centerX, centerY + pitchOffset);
                g.RotateTransform(rollAngle);
                g.DrawLine(pen, -100, 0, 100, 0);
                g.ResetTransform();
            }
        }
        */

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // HudOverlayControl
            // 
            this.Name = "HudOverlayControl";
            this.ResumeLayout(false);

        }
    }
}
