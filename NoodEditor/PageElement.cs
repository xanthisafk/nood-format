public class PageElement
{
    public enum ElementType : byte
    {
        Text = 0,
        Image = 1
    }
    public ElementType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public byte[] Data { get; set; }
    public float FontSize { get; set; }
    public int Color { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public bool IsStrikethrough { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}