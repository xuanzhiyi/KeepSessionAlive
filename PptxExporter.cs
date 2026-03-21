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
        public static void Create(string filePath, List<Bitmap> images)
        {
            using (var ppt = PresentationDocument.Create(filePath, PresentationDocumentType.Presentation))
            {
                var presentationPart = ppt.AddPresentationPart();
                presentationPart.Presentation = new Presentation();

                var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
                slideMasterPart.SlideMaster = new SlideMaster(
                    new CommonSlideData(new ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(new D.TransformGroup()))),
                    new P.SlideLayoutIdList());

                var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
                slideLayoutPart.SlideLayout = new SlideLayout(
                    new CommonSlideData(new ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(new D.TransformGroup()))));
                slideLayoutPart.SlideLayout.Save();

                slideMasterPart.SlideMaster.SlideLayoutIdList.Append(
                    new SlideLayoutId { Id = 2147483649U, RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart) });
                slideMasterPart.SlideMaster.Save();

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
                    var slidePart = presentationPart.AddNewPart<SlidePart>();
                    var imgPart = slidePart.AddImagePart(ImagePartType.Png);

                    using (var ms = new MemoryStream())
                    {
                        images[i].Save(ms, ImageFormat.Png);
                        ms.Position = 0;
                        imgPart.FeedData(ms);
                    }

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
                                    new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle })))));

                    slidePart.AddPart(slideLayoutPart);
                    slidePart.Slide.Save();

                    slideIdList.Append(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
                }

                presentationPart.Presentation.Save();
            }
        }
    }
}
