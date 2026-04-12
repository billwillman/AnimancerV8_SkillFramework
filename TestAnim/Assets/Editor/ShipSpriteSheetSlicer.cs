using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor 右键菜单工具：将帆船 sprite sheet PNG 自动切片为 5方向 x 4帧 的 Sprite 序列帧。
/// 布局规范（从上到下）：Row1=Up, Row2=Down, Row3=UpLeft, Row4=DownLeft, Row5=Left
/// </summary>
public static class ShipSpriteSheetSlicer
{
    private const int Columns = 4;
    private const int Rows = 5;

    private static readonly string[] DirectionNames = { "Up", "Down", "UpLeft", "DownLeft", "Left" };

    [MenuItem("Assets/Ship Tools/Slice Ship Sprite Sheet", false, 2000)]
    private static void SliceShipSpriteSheet()
    {
        var selectedObjects = Selection.objects;
        var texturePaths = new System.Collections.Generic.List<string>();

        foreach (var obj in selectedObjects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            texturePaths.Add(path);
        }

        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Ship Sprite Slicer", "请选中至少一张 PNG 贴图。", "确定");
            return;
        }

        // 在批量编辑块之前处理覆盖确认（避免在 StartAssetEditing 内弹对话框）
        var pathsToProcess = new System.Collections.Generic.List<string>();
        foreach (var path in texturePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            if (importer.spriteImportMode == SpriteImportMode.Multiple && importer.spritesheet != null && importer.spritesheet.Length > 0)
            {
                if (!EditorUtility.DisplayDialog("Ship Sprite Slicer",
                    $"贴图 \"{System.IO.Path.GetFileName(path)}\" 已有 {importer.spritesheet.Length} 个切片。\n是否覆盖？",
                    "覆盖", "跳过"))
                {
                    continue;
                }
            }

            pathsToProcess.Add(path);
        }

        if (pathsToProcess.Count == 0)
            return;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in pathsToProcess)
            {
                ProcessTexture(path);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ShipSpriteSheetSlicer] 已完成 {pathsToProcess.Count} 张贴图的序列帧切片。");
    }

    [MenuItem("Assets/Ship Tools/Slice Ship Sprite Sheet", true)]
    private static bool SliceShipSpriteSheetValidate()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
            return false;

        foreach (var obj in selectedObjects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                if (AssetImporter.GetAtPath(path) is TextureImporter)
                    return true;
            }
        }

        return false;
    }

    private static void ProcessTexture(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        // 获取贴图实际尺寸
        int texWidth = 0, texHeight = 0;
        importer.GetSourceTextureWidthAndHeight(out texWidth, out texHeight);

        if (texWidth <= 0 || texHeight <= 0)
        {
            Debug.LogError($"[ShipSpriteSheetSlicer] 无法获取贴图尺寸: {assetPath}");
            return;
        }

        int frameW = texWidth / Columns;
        int frameH = texHeight / Rows;

        if (texWidth % Columns != 0 || texHeight % Rows != 0)
        {
            Debug.LogWarning($"[ShipSpriteSheetSlicer] 贴图 {assetPath} 尺寸 ({texWidth}x{texHeight}) 无法被 {Columns}x{Rows} 整除，帧大小取整为 {frameW}x{frameH}。");
        }

        // 设置导入参数
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // 生成切片数据
        var textureName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        importer.spritesheet = GenerateSpriteMetaData(textureName, texWidth, texHeight, frameW, frameH);

        // 保存并重新导入
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[ShipSpriteSheetSlicer] 切片完成: {assetPath} ({Rows}方向 x {Columns}帧, 帧大小 {frameW}x{frameH})");
    }

    private static SpriteMetaData[] GenerateSpriteMetaData(string textureName, int texWidth, int texHeight, int frameW, int frameH)
    {
        var sprites = new SpriteMetaData[Rows * Columns];

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int index = row * Columns + col;

                // Unity Sprite Rect: 左下角原点
                // Row 0 (图片顶部) -> y = texHeight - frameH
                // Row 4 (图片底部) -> y = 0
                int x = col * frameW;
                int y = texHeight - (row + 1) * frameH;

                sprites[index] = new SpriteMetaData
                {
                    name = $"{textureName}_{DirectionNames[row]}_{col}",
                    rect = new Rect(x, y, frameW, frameH),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f), // BottomCenter
                };
            }
        }

        return sprites;
    }
}
