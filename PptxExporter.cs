using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

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
                var presentationPart = ppt.AddPresentationPart();

                // ── Create skeleton parts so the SDK assigns relationship IDs ──────────
                var slideMasterPart  = presentationPart.AddNewPart<SlideMasterPart>();
                var slideLayoutPart  = slideMasterPart.AddNewPart<SlideLayoutPart>();
                var themePart        = slideMasterPart.AddNewPart<ThemePart>();

                string masterRId     = presentationPart.GetIdOfPart(slideMasterPart);
                string layoutRId     = slideMasterPart.GetIdOfPart(slideLayoutPart);
                string masterBackRId = slideLayoutPart.AddPart(slideMasterPart); // layout→master back-ref

                Log($"masterRId={masterRId}  layoutRId={layoutRId}  masterBackRId={masterBackRId}");

                // ── Write raw XML for parts that need strict compliance ───────────────
                WriteRaw(themePart,        ThemeXml());
                WriteRaw(slideLayoutPart,  SlideLayoutXml());
                WriteRaw(slideMasterPart,  SlideMasterXml(layoutRId));
                Log("Theme, SlideLayout, SlideMaster written");

                // ── Presentation element ─────────────────────────────────────────────
                // Element order per OOXML schema: sldMasterIdLst → sldIdLst → sldSz → notesSz
                long slideW = 12192000L;
                long slideH = 6858000L;

                var slideIdList = new SlideIdList();
                presentationPart.Presentation = new Presentation(
                    new SlideMasterIdList(
                        new SlideMasterId { Id = 2147483648U, RelationshipId = masterRId }),
                    slideIdList,
                    new SlideSize { Cx = (Int32Value)(int)slideW, Cy = (Int32Value)(int)slideH,
                                    Type = SlideSizeValues.Screen16x9 },
                    new NotesSize { Cx = 6858000, Cy = 9144000 });

                // ── Slides ───────────────────────────────────────────────────────────
                uint slideId = 256;
                for (int i = 0; i < images.Count; i++)
                {
                    Log($"Image {i + 1}/{images.Count}: {images[i].Width}x{images[i].Height} px");

                    var slidePart = presentationPart.AddNewPart<SlidePart>();
                    var imgPart   = slidePart.AddImagePart(ImagePartType.Png);

                    long pngBytes;
                    using (var ms = new MemoryStream())
                    {
                        images[i].Save(ms, ImageFormat.Png);
                        pngBytes = ms.Length;
                        ms.Position = 0;
                        imgPart.FeedData(ms);
                    }

                    string imgRId = slidePart.GetIdOfPart(imgPart);
                    slidePart.AddPart(slideLayoutPart); // establishes slide→layout relationship
                    Log($"  PNG {pngBytes:N0} bytes  imgRId={imgRId}");

                    float scale = Math.Min(slideW / (float)images[i].Width,
                                          slideH / (float)images[i].Height);
                    long picW = (long)(images[i].Width  * scale);
                    long picH = (long)(images[i].Height * scale);
                    long offX = (slideW - picW) / 2;
                    long offY = (slideH - picH) / 2;
                    Log($"  scale={scale:F4}  picW={picW}  picH={picH}  offX={offX}  offY={offY}");

                    WriteRaw(slidePart, SlideXml(imgRId, $"Screenshot {i + 1}", offX, offY, picW, picH));

                    string slideRId = presentationPart.GetIdOfPart(slidePart);
                    slideIdList.Append(new SlideId { Id = slideId++, RelationshipId = slideRId });
                    Log($"  Slide {i + 1} written  slideRId={slideRId}");
                }

                presentationPart.Presentation.Save();
                Log("Presentation saved — closing document");
            }

            if (File.Exists(filePath))
            {
                long sz = new FileInfo(filePath).Length;
                Log($"File written: {sz:N0} bytes");
                if (sz < 4096) Log("WARNING: suspiciously small — may be corrupt");
            }
            else
            {
                Log("ERROR: output file missing after write");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        static void WriteRaw(OpenXmlPart part, string xml)
        {
            using (var sw = new StreamWriter(
                part.GetStream(FileMode.Create, FileAccess.Write),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                sw.Write(xml);
        }

        // ── Raw XML templates ─────────────────────────────────────────────────────

        static string ThemeXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<a:theme xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main"" name=""Office Theme"">
  <a:themeElements>
    <a:clrScheme name=""Office"">
      <a:dk1><a:sysClr val=""windowText"" lastClr=""000000""/></a:dk1>
      <a:lt1><a:sysClr val=""window"" lastClr=""FFFFFF""/></a:lt1>
      <a:dk2><a:srgbClr val=""44546A""/></a:dk2>
      <a:lt2><a:srgbClr val=""E7E6E6""/></a:lt2>
      <a:accent1><a:srgbClr val=""4472C4""/></a:accent1>
      <a:accent2><a:srgbClr val=""ED7D31""/></a:accent2>
      <a:accent3><a:srgbClr val=""A9D18E""/></a:accent3>
      <a:accent4><a:srgbClr val=""FFC000""/></a:accent4>
      <a:accent5><a:srgbClr val=""5A96C8""/></a:accent5>
      <a:accent6><a:srgbClr val=""70AD47""/></a:accent6>
      <a:hlink><a:srgbClr val=""0563C1""/></a:hlink>
      <a:folHlink><a:srgbClr val=""954F72""/></a:folHlink>
    </a:clrScheme>
    <a:fontScheme name=""Office"">
      <a:majorFont>
        <a:latin typeface=""Calibri Light""/>
        <a:ea typeface=""""/>
        <a:cs typeface=""""/>
      </a:majorFont>
      <a:minorFont>
        <a:latin typeface=""Calibri""/>
        <a:ea typeface=""""/>
        <a:cs typeface=""""/>
      </a:minorFont>
    </a:fontScheme>
    <a:fmtScheme name=""Office"">
      <a:fillStyleLst>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
      </a:fillStyleLst>
      <a:lnStyleLst>
        <a:ln w=""6350""><a:solidFill><a:schemeClr val=""phClr""/></a:solidFill></a:ln>
        <a:ln w=""12700""><a:solidFill><a:schemeClr val=""phClr""/></a:solidFill></a:ln>
        <a:ln w=""19050""><a:solidFill><a:schemeClr val=""phClr""/></a:solidFill></a:ln>
      </a:lnStyleLst>
      <a:effectStyleLst>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
      </a:effectStyleLst>
      <a:bgFillStyleLst>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
        <a:solidFill><a:schemeClr val=""phClr""/></a:solidFill>
      </a:bgFillStyleLst>
    </a:fmtScheme>
  </a:themeElements>
</a:theme>";

        static string SlideMasterXml(string layoutRId) =>
$@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<p:sldMaster xmlns:p=""http://schemas.openxmlformats.org/presentationml/2006/main""
             xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main""
             xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <p:cSld>
    <p:spTree>
      <p:nvGrpSpPr>
        <p:cNvPr id=""1"" name=""""/>
        <p:cNvGrpSpPr/>
        <p:nvPr/>
      </p:nvGrpSpPr>
      <p:grpSpPr>
        <a:xfrm>
          <a:off x=""0"" y=""0""/>
          <a:ext cx=""0"" cy=""0""/>
          <a:chOff x=""0"" y=""0""/>
          <a:chExt cx=""0"" cy=""0""/>
        </a:xfrm>
      </p:grpSpPr>
    </p:spTree>
  </p:cSld>
  <p:clrMap bg1=""lt1"" tx1=""dk1"" bg2=""lt2"" tx2=""dk2""
            accent1=""accent1"" accent2=""accent2"" accent3=""accent3""
            accent4=""accent4"" accent5=""accent5"" accent6=""accent6""
            hlink=""hlink"" folHlink=""folHlink""/>
  <p:sldLayoutIdLst>
    <p:sldLayoutId id=""2147483649"" r:id=""{layoutRId}""/>
  </p:sldLayoutIdLst>
  <p:txStyles>
    <p:titleStyle>
      <a:lvl1pPr algn=""ctr""><a:defRPr lang=""en-US"" smtClean=""0""/></a:lvl1pPr>
    </p:titleStyle>
    <p:bodyStyle>
      <a:lvl1pPr><a:defRPr lang=""en-US"" smtClean=""0""/></a:lvl1pPr>
    </p:bodyStyle>
    <p:otherStyle>
      <a:defPPr><a:defRPr lang=""en-US"" smtClean=""0""/></a:defPPr>
    </p:otherStyle>
  </p:txStyles>
</p:sldMaster>";

        static string SlideLayoutXml() =>
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<p:sldLayout xmlns:p=""http://schemas.openxmlformats.org/presentationml/2006/main""
             xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main""
             xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""
             type=""blank"" preserve=""1"">
  <p:cSld name=""Blank"">
    <p:spTree>
      <p:nvGrpSpPr>
        <p:cNvPr id=""1"" name=""""/>
        <p:cNvGrpSpPr/>
        <p:nvPr/>
      </p:nvGrpSpPr>
      <p:grpSpPr>
        <a:xfrm>
          <a:off x=""0"" y=""0""/>
          <a:ext cx=""0"" cy=""0""/>
          <a:chOff x=""0"" y=""0""/>
          <a:chExt cx=""0"" cy=""0""/>
        </a:xfrm>
      </p:grpSpPr>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sldLayout>";

        static string SlideXml(string imgRId, string name, long offX, long offY, long picW, long picH) =>
$@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<p:sld xmlns:p=""http://schemas.openxmlformats.org/presentationml/2006/main""
       xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main""
       xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <p:cSld>
    <p:spTree>
      <p:nvGrpSpPr>
        <p:cNvPr id=""1"" name=""""/>
        <p:cNvGrpSpPr/>
        <p:nvPr/>
      </p:nvGrpSpPr>
      <p:grpSpPr>
        <a:xfrm>
          <a:off x=""0"" y=""0""/>
          <a:ext cx=""0"" cy=""0""/>
          <a:chOff x=""0"" y=""0""/>
          <a:chExt cx=""0"" cy=""0""/>
        </a:xfrm>
      </p:grpSpPr>
      <p:pic>
        <p:nvPicPr>
          <p:cNvPr id=""2"" name=""{System.Security.SecurityElement.Escape(name)}""/>
          <p:cNvPicPr><a:picLocks noChangeAspect=""1""/></p:cNvPicPr>
          <p:nvPr/>
        </p:nvPicPr>
        <p:blipFill>
          <a:blip r:embed=""{imgRId}""/>
          <a:stretch><a:fillRect/></a:stretch>
        </p:blipFill>
        <p:spPr>
          <a:xfrm>
            <a:off x=""{offX}"" y=""{offY}""/>
            <a:ext cx=""{picW}"" cy=""{picH}""/>
          </a:xfrm>
          <a:prstGeom prst=""rect""><a:avLst/></a:prstGeom>
        </p:spPr>
      </p:pic>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>";
    }
}
