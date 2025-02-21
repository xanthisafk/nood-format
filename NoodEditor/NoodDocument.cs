using System.IO;
using System.IO.Compression;
using System.Text;

namespace NoodEditor
{
    public class NoodDocument
    {
        // File signature to identify .nood files
        private const string FILE_SIGNATURE = "NOOD";
        private const ushort VERSION = 1;

        public class Page
        {
            public float Width { get; set; }
            public float Height { get; set; }
            public List<PageElement> Elements { get; set; } = new List<PageElement>();
        }

        public List<Page> Pages { get; private set; } = new List<Page>();

        // Save document to .nood file
        public void SaveToFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(Encoding.ASCII.GetBytes(FILE_SIGNATURE));
                writer.Write(VERSION);

                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter contentWriter = new BinaryWriter(ms))
                {
                    contentWriter.Write(Pages.Count);

                    foreach (var page in Pages)
                    {
                        contentWriter.Write(page.Width);
                        contentWriter.Write(page.Height);
                        contentWriter.Write(page.Elements.Count);

                        foreach (var element in page.Elements)
                        {
                            contentWriter.Write((byte)element.Type);
                            contentWriter.Write(element.X);
                            contentWriter.Write(element.Y);

                            if (element.Type == PageElement.ElementType.Text)
                            {
                                contentWriter.Write(element.FontSize);
                                contentWriter.Write(element.Color);
                                contentWriter.Write(element.IsBold);
                                contentWriter.Write(element.IsItalic);
                                contentWriter.Write(element.IsUnderline);
                                contentWriter.Write(element.IsStrikethrough);
                            }
                            else if (element.Type == PageElement.ElementType.Image)
                            {
                                contentWriter.Write(element.Width);
                                contentWriter.Write(element.Height);
                            }

                            contentWriter.Write(element.Data.Length);
                            contentWriter.Write(element.Data);
                        }
                    }

                    byte[] uncompressedContent = ms.ToArray();
                    using (MemoryStream compressedMs = new MemoryStream())
                    {
                        using (GZipStream gzip = new GZipStream(compressedMs, CompressionMode.Compress))
                        {
                            gzip.Write(uncompressedContent, 0, uncompressedContent.Length);
                        }

                        byte[] compressedContent = compressedMs.ToArray();
                        writer.Write(compressedContent.Length);
                        writer.Write(compressedContent);
                    }
                }
            }
        }

        public static NoodDocument LoadFromFile(string filePath)
        {
            NoodDocument doc = new NoodDocument();

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                byte[] signature = reader.ReadBytes(4);
                if (Encoding.ASCII.GetString(signature) != FILE_SIGNATURE)
                    throw new Exception("Invalid file format");

                ushort version = reader.ReadUInt16();
                if (version != VERSION)
                    throw new Exception("Unsupported version");

                int compressedLength = reader.ReadInt32();
                byte[] compressedContent = reader.ReadBytes(compressedLength);

                using (MemoryStream ms = new MemoryStream(compressedContent))
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                using (MemoryStream decompressedMs = new MemoryStream())
                {
                    gzip.CopyTo(decompressedMs);
                    decompressedMs.Position = 0;

                    using (BinaryReader contentReader = new BinaryReader(decompressedMs))
                    {
                        int pageCount = contentReader.ReadInt32();

                        for (int i = 0; i < pageCount; i++)
                        {
                            Page page = new Page
                            {
                                Width = contentReader.ReadSingle(),
                                Height = contentReader.ReadSingle()
                            };

                            int elementCount = contentReader.ReadInt32();
                            for (int j = 0; j < elementCount; j++)
                            {
                                PageElement element = new PageElement
                                {
                                    Type = (PageElement.ElementType)contentReader.ReadByte(),
                                    X = contentReader.ReadSingle(),
                                    Y = contentReader.ReadSingle()
                                };

                                if (element.Type == PageElement.ElementType.Text)
                                {
                                    element.FontSize = contentReader.ReadSingle();
                                    element.Color = contentReader.ReadInt32();
                                    element.IsBold = contentReader.ReadBoolean();
                                    element.IsItalic = contentReader.ReadBoolean();
                                    element.IsUnderline = contentReader.ReadBoolean();
                                    element.IsStrikethrough = contentReader.ReadBoolean();
                                }
                                else if (element.Type == PageElement.ElementType.Image)
                                {
                                    element.Width = contentReader.ReadSingle();
                                    element.Height = contentReader.ReadSingle();
                                }

                                int dataLength = contentReader.ReadInt32();
                                element.Data = contentReader.ReadBytes(dataLength);

                                page.Elements.Add(element);
                            }

                            doc.Pages.Add(page);
                        }
                    }
                }
            }

            return doc;
        }

        // Helper methods to add content
        public void AddPage(float width, float height)
        {
            Pages.Add(new Page { Width = width, Height = height });
        }

        public void AddText(int pageIndex, string text, float x, float y, float fontSize = 12, int color = 0)
        {
            if (pageIndex >= Pages.Count)
                throw new ArgumentException("Invalid page index");

            Pages[pageIndex].Elements.Add(new PageElement
            {
                Type = PageElement.ElementType.Text,
                X = x,
                Y = y,
                FontSize = fontSize,
                Color = color,
                Data = Encoding.UTF8.GetBytes(text)
            });
        }

        public void AddImage(int pageIndex, byte[] imageData, float x, float y)
        {
            if (pageIndex >= Pages.Count)
                throw new ArgumentException("Invalid page index");

            Pages[pageIndex].Elements.Add(new PageElement
            {
                Type = PageElement.ElementType.Image,
                X = x,
                Y = y,
                Data = imageData
            });
        }
    }
}