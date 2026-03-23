using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace KeepSessionAlive
{
    internal static class PptxExporter
    {
        public static void Create(string filePath, List<Bitmap> images, Action<string> log = null)
        {
            void Log(string msg) => log?.Invoke($"{DateTime.Now:HH:mm:ss} [PPTX] {msg}\r\n");

            Log($"Starting export — {images.Count} image(s) → {filePath}");

            using (var ppt = PresentationDocument.Create(filePath, PresentationDocumentType.Presentation))
            {
                Log("Created PresentationDocument");

                var presentationPart = ppt.AddPresentationPart();
                presentationPart.Presentation = new Presentation();

                // --- Slide Master ---
                var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
                var slideMaster = new SlideMaster(
                    new CommonSlideData(new ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(new D.TransformGroup()))),
                    new P.ColorMap
                    {
                        Background1        = D.ColorSchemeIndexValues.Light1,
                        Text1              = D.ColorSchemeIndexValues.Dark1,
                        Background2        = D.ColorSchemeIndexValues.Light2,
                        Text2              = D.ColorSchemeIndexValues.Dark2,
                        Accent1            = D.ColorSchemeIndexValues.Accent1,
                        Accent2            = D.ColorSchemeIndexValues.Accent2,
                        Accent3            = D.ColorSchemeIndexValues.Accent3,
                        Accent4            = D.ColorSchemeIndexValues.Accent4,
                        Accent5            = D.ColorSchemeIndexValues.Accent5,
                        Accent6            = D.ColorSchemeIndexValues.Accent6,
                        Hyperlink          = D.ColorSchemeIndexValues.Hyperlink,
                        FollowedHyperlink  = D.ColorSchemeIndexValues.FollowedHyperlink,
                    },
                    new P.SlideLayoutIdList());
                slideMasterPart.SlideMaster = slideMaster;
                Log("SlideMaster created with ColorMap");

                // --- Slide Layout ---
                var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
                var slideLayout = new SlideLayout { Type = SlideLayoutValues.Blank, Preserve = true };
                slideLayout.Append(
                    new CommonSlideData(new ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(new D.TransformGroup()))),
                    new ColorMapOverride(new D.MasterColorMapping()));
                slideLayoutPart.SlideLayout = slideLayout;
                slideLayoutPart.AddPart(slideMasterPart);   // back-reference required
                slideLayoutPart.SlideLayout.Save();
                Log($"SlideLayout saved (rId={slideMasterPart.GetIdOfPart(slideLayoutPart)})");

                slideMaster.SlideLayoutIdList.Append(
                    new SlideLayoutId { Id = 2147483649U, RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart) });
                slideMasterPart.SlideMaster.Save();
                Log("SlideMaster saved");

                presentationPart.Presentation.Append(new SlideMasterIdList(
                    new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) }));

                // Slide size — widescreen 16:9 (in EMUs: 1 inch = 914400 EMUs)
                long slideW = 12192000L; // 13.333 inches
                long slideH = 6858000L;  // 7.5 inches

                presentationPart.Presentation.Append(
                    new SlideSize { Cx = (Int32Value)(int)slideW, Cy = (Int32Value)(int)slideH, Type = SlideSizeValues.Screen16x9 });
                presentationPart.Presentation.Append(
                    new NotesSize { Cx = 6858000, Cy = 9144000 });

                var slideIdList = new SlideIdList();
                presentationPart.Presentation.Append(slideIdList);

                uint slideId = 256;
                for (int i = 0; i < images.Count; i++)
                {
                    Log($"Processing image {i + 1}/{images.Count}: {images[i].Width}x{images[i].Height} px, format={images[i].PixelFormat}");

                    var slidePart = presentationPart.AddNewPart<SlidePart>();
                    var imgPart = slidePart.AddImagePart(ImagePartType.Png);

                    long pngBytes;
                    using (var ms = new MemoryStream())
                    {
                        images[i].Save(ms, ImageFormat.Png);
                        pngBytes = ms.Length;
                        ms.Position = 0;
                        imgPart.FeedData(ms);
                    }
                    Log($"  PNG encoded: {pngBytes:N0} bytes, rId={slidePart.GetIdOfPart(imgPart)}");

                    string rId = slidePart.GetIdOfPart(imgPart);

                    // Fit image within slide preserving aspect ratio
                    float imgW = images[i].Width;
                    float imgH = images[i].Height;
                    float scaleX = slideW / imgW;
                    float scaleY = slideH / imgH;
                    float scale = Math.Min(scaleX, scaleY);

                    long picW = (long)(imgW * scale);
                    long picH = (long)(imgH * scale);
                    long offX = (slideW - picW) / 2;
                    long offY = (slideH - picH) / 2;
                    Log($"  Layout: scale={scale:F4}, picW={picW}, picH={picH}, offX={offX}, offY={offY}");

                    slidePart.Slide = new Slide(
                        new CommonSlideData(new ShapeTree(
                            new P.NonVisualGroupShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                                new P.NonVisualGroupShapeDrawingProperties(),
                                new ApplicationNonVisualDrawingProperties()),
                            new GroupShapeProperties(new D.TransformGroup()),
                            new P.Picture(
                                new P.NonVisualPictureProperties(
                                    new P.NonVisualDrawingProperties { Id = 2, Name = $"Screenshot {i + 1}" },
                                    new P.NonVisualPictureDrawingProperties(new D.PictureLocks { NoChangeAspect = true }),
                                    new ApplicationNonVisualDrawingProperties()),
                                new P.BlipFill(
                                    new D.Blip { Embed = rId },
                                    new D.Stretch(new D.FillRectangle())),
                                new P.ShapeProperties(
                                    new D.Transform2D(
                                        new D.Offset { X = offX, Y = offY },
                                        new D.Extents { Cx = picW, Cy = picH }),
                                    new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle })))),
                        new ColorMapOverride(new D.MasterColorMapping()));

                    slidePart.AddPart(slideLayoutPart);
                    slidePart.Slide.Save();
                    Log($"  Slide {i + 1} saved (rId={presentationPart.GetIdOfPart(slidePart)})");

                    slideIdList.Append(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
                }

                presentationPart.Presentation.Save();
                Log("Presentation saved — closing document");
            }

            // Post-write sanity check
            if (File.Exists(filePath))
            {
                long fileSize = new FileInfo(filePath).Length;
                Log($"File written successfully: {fileSize:N0} bytes at {filePath}");
                if (fileSize < 4096)
                    Log("WARNING: file is suspiciously small — may be corrupt");
            }
            else
            {
                Log("ERROR: output file does not exist after write!");
            }
        }
    }
}
