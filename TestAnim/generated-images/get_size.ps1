$code = @"
using System;
using System.Drawing;

public class ImageInfo
{
    public static void GetInfo(string file)
    {
        using (Bitmap bmp = new Bitmap(file))
        {
            Console.WriteLine(string.Format("Image {0}: {1}x{2}", System.IO.Path.GetFileName(file), bmp.Width, bmp.Height));
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
$folder = "d:\Work\AnimancerV8_SkillFramework\TestAnim\generated-images"
Get-ChildItem $folder -Filter "*.png" | ForEach-Object {
    [ImageInfo]::GetInfo($_.FullName)
}
