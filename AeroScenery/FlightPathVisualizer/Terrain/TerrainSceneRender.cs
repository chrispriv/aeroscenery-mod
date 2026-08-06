using AeroScenery.Common;
using AeroScenery.Data;
using AForge.Math.Metrics;
using GMap.NET.MapProviders;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace AeroScenery.FlightPathVisualizer.Terrain
{
    public class TerrainSceneRenderer
    {
        double yawInitial, pitchInitial, distanceInitial;
        private System.Windows.Point lastMousePos;
        private bool isDragging = false;
        private double yaw = 0;       // Links-Rechts-Drehung
        private double pitch = 20;    // Hoch-Runter-Drehung
        private double distance = 300; // Abstand zur Kamera
        private PerspectiveCamera currentCamera;
        private PerspectiveCamera initialCamera;
        private Point3D targetPosition; // Zentrum, auf das Kamera schaut
        private MeshGeometry3D currentMesh;

        public Viewport3D RenderTerrainMesh(MeshGeometry3D mesh, double altitude, double headingDegrees, double pitchDegrees, double rollDegrees)
        {
            //this.settings = settingsService.GetSettings();
            this.currentMesh = mesh; // 🔁 Speichern

            var viewport = new Viewport3D
            {
                Width = 1280, //WHD 16:9
                Height = 720
            };

            // Licht (sowohl Umgebungs- als auch Richtungslampe)
            var lights = new Model3DGroup();
            lights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));
            viewport.Children.Add(new ModelVisual3D { Content = lights });

            
            // Kamera-Ausrichtung
            var bounds = mesh.Bounds;
            double centerX = bounds.X + bounds.SizeX / 2;
            double centerY = bounds.Y + bounds.SizeY / 2;

            // Heading in Radiant umrechnen
            double headingRad = headingDegrees * Math.PI / 180.0;

            // Kamera-Rückstand hinter dem Flugzeug (z. B. 300 m)
            double camDistance = 300;
            double camHeight = 20; // z. B. 25 m über Flugzeug

            // Kamera-Position hinter dem Flugzeug (in Flugrichtung versetzt)
            double camX = centerX + Math.Sin(headingRad) * camDistance;
            double camY = centerY + Math.Cos(headingRad) * camDistance;

            double camZ = altitude + camHeight;
            
            // Blickrichtung zur Flugzeugposition
            var lookDir = new Vector3D(centerX - camX, centerY - camY, altitude - camZ);


            /*
            //#CHANHE
            //var camera = new PerspectiveCamera
            var camera = new PerspectiveCamera
            {
                Position = new Point3D(camX, camY, camZ),
                LookDirection = lookDir,
                UpDirection = new Vector3D(0, 0, 1),
                FieldOfView = 65,
                NearPlaneDistance = 1,
                FarPlaneDistance = 60000
            };

            //#ERROR
            //cameraController = new HelixToolkit.Wpf.CameraController(camera);
            
            viewport.Camera = camera;
            */

            targetPosition = new Point3D(centerX, centerY, altitude); // Zielpunkt, z. B. Flugzeug

            //double yawInitial = headingDegrees;
            //double pitchInitial = 20;
            yawInitial = headingDegrees;
            pitchInitial = 20;

            yaw = yawInitial;
            pitch = pitchInitial;
            distance = camDistance;
            distanceInitial = distance;

            currentCamera = new PerspectiveCamera();
            initialCamera = currentCamera;
            //viewport.Camera = currentCamera;
            UpdateCameraTransform(); // initiale Position berechnen

            currentCamera.UpDirection = new Vector3D(0, 0, 1);
            currentCamera.FieldOfView = 65;
            currentCamera.NearPlaneDistance = 1;
            currentCamera.FarPlaneDistance = 60000;

            viewport.Camera = currentCamera;


            // Mesh-Material (LightGray für Testzwecke)
            var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
            //var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(200, 80, 80))); // NICHT Color.FromArgb!
            var terrainModel = new GeometryModel3D(mesh, material);

            viewport.Children.Add(new ModelVisual3D { Content = terrainModel });

            var hudPlane = CreateSimpleHudPlaneAt(centerX, centerY, altitude, headingDegrees, pitchDegrees, rollDegrees, 3.0); // Skaliert auf 250%
            viewport.Children.Add(hudPlane);

            //#TRY
            // Maus-Events für Kamera
            AttachMouseControls(viewport);

            return viewport;
        }

        //#TRY
        private void AttachMouseControls(Viewport3D viewport)
        {
            viewport.MouseMove += Viewport_MouseMove;
            viewport.MouseWheel += Viewport_MouseWheel;
            viewport.MouseDown += Viewport_MouseDown;
            viewport.MouseUp += Viewport_MouseUp;
        }

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition((UIElement)sender);
            ((UIElement)sender).CaptureMouse();

            var settings = AeroSceneryManager.Instance.Settings;
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                lastMousePos = e.GetPosition((UIElement)sender);
                ((UIElement)sender).CaptureMouse();
            }
            //else if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            else if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed && settings.MovingMapElevationEnable3DCapture == true)
            {
                // Aktion bei Klick mit der mittleren Maustaste
                //var settings = AeroSceneryManager.Instance.Settings;
                string outputPath = System.IO.Path.Combine(settings.AeroSceneryDBDirectory, "elevation");
                ModellExporter3D.ColladaExporter(currentMesh, System.IO.Path.Combine(outputPath, "terrainModell.dae"));
                MessageBox.Show($"Terrain 3D Modell saved as terrainModell.dae in {outputPath}\\.", "3D Modell Export");
                e.Handled = true;   // Verhindert ggf. weitere Verarbeitung
            }
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point pos = e.GetPosition((UIElement)sender);
                double dx = pos.X - lastMousePos.X;
                double dy = pos.Y - lastMousePos.Y;

                yaw += dx * 0.5;     // Empfindlichkeit anpassbar
                pitch -= dy * 0.5;
                pitch = Math.Max(-89, Math.Min(89, pitch)); // Begrenzung

                UpdateCameraTransform();

                lastMousePos = pos;
            }
        }


        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = e.Delta > 0 ? -20 : 20;
            distance = Math.Max(10, Math.Min(1000, distance + delta));
            UpdateCameraTransform();
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                // Rechtsklick = Kamera zurücksetzen
                //ResetCameraView();
                System.Windows.Point pos = e.GetPosition((UIElement)sender);
                yaw = yawInitial;
                pitch = pitchInitial;
                distance = distanceInitial;
                UpdateCameraTransform();
                lastMousePos = pos;
            }
        }

        private void UpdateCameraTransform()
        {
            if (currentCamera == null) return;

            double yawRad = yaw * Math.PI / 180.0;
            double pitchRad = pitch * Math.PI / 180.0;

            double x = distance * Math.Cos(pitchRad) * Math.Sin(yawRad);
            double y = distance * Math.Cos(pitchRad) * Math.Cos(yawRad);
            double z = distance * Math.Sin(pitchRad);

            var camPos = new Point3D(
                targetPosition.X + x,
                targetPosition.Y + y,
                targetPosition.Z + z
            );

            currentCamera.Position = camPos;
            currentCamera.LookDirection = new Vector3D(
                targetPosition.X - camPos.X,
                targetPosition.Y - camPos.Y,
                targetPosition.Z - camPos.Z
            );
        }

        public static ModelVisual3D CreateSimpleHudPlaneAt(double x, double y, double z, double headingDegrees, double pitchDegrees, double rollDegrees, double scale)
        {
            var model = CreateSimpleHudPlane(scale * 100);

            // Modell in XY-Ebene erzeugt, daher Rotation in XZ-Ebene nötig
            var upright = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90));

            var baseGroup = new Model3DGroup();
            baseGroup.Children.Add(model);
            baseGroup.Transform = upright;

            // 🟢 Heading um Z-Achse statt Y!
            var heading = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), -headingDegrees));

            // Pitch um X-Achse
            var pitch = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), -pitchDegrees));

            // Roll um Z-Achse (lokal)
            var roll = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), -rollDegrees));

            var translate = new TranslateTransform3D(x, y, z);

            var transformGroup = new Transform3DGroup();

            transformGroup.Children.Add(pitch);     // dann Pitch
            transformGroup.Children.Add(roll);      // dann Roll
            transformGroup.Children.Add(heading);   // zuerst Heading
            transformGroup.Children.Add(translate); // zuletzt Position

            return new ModelVisual3D
            {
                Content = baseGroup,
                Transform = transformGroup
            };
        }


        public static Model3D CreateSimpleHudPlane(double scale)
        {
            var group = new Model3DGroup();
            var material = MaterialHelper.CreateMaterial(Colors.Yellow);

            var thickness = 0.02 * scale;
            var hightOffset = 0.05 * scale;

            var center = new Point3D(0, hightOffset, 0);
            var meshBuilder = new MeshBuilder();

            // Rumpf (kleiner Kreis oder Punkt)
            meshBuilder.AddSphere(center, 0.03 * scale, 8, 8);

            // Flügel (zwei waagrechte Linien links/rechts)
            var wingSpan = 0.2 * scale;
            var leftWing = new Point3D(-wingSpan, hightOffset, 0);
            var rightWing = new Point3D(wingSpan, hightOffset, 0);
            meshBuilder.AddCylinder(leftWing, rightWing, thickness, 6);

            // Heckflosse (kurze vertikale Linie nach oben)
            var tailHeight = 0.1 * scale;
            var topTail = new Point3D(0, tailHeight + hightOffset, 0);
            meshBuilder.AddCylinder(center, topTail, thickness, 6);

            // Rumpf (lange vertikale Linie nach vorne)
            var fuselageLenght = 0.25 * scale;
            var fuselage = new Point3D(0, hightOffset, fuselageLenght);
            var fuselageBack = new Point3D(0, hightOffset, - 0.05 * scale);
            meshBuilder.AddCylinder(fuselageBack, fuselage, thickness * 2.5, 6);

            var mesh = meshBuilder.ToMesh();
            group.Children.Add(new GeometryModel3D { Geometry = mesh, Material = material });

            return group;
        }

    }
}
