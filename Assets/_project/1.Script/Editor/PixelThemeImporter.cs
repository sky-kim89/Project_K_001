using UnityEditor;
using UnityEngine;

// ============================================================
//  PixelThemeImporter.cs  [Editor Only]
//  UI/PixelTheme 폴더에 **새로 들어온** PNG 를 도트용 스프라이트로 임포트한다.
//
//  ■ 왜
//    Codex 가 아이콘을 만들어 넣으면 .meta 가 없는 상태로 들어온다.
//    기본 임포트(Bilinear·압축)는 도트를 뭉갠다 — 매번 손으로 고치지 않게 한다.
//
//  ⚠ 이미 .meta 가 있는 파일은 건드리지 않는다 (importSettingsMissing)
//    9-slice border 를 손으로 잡아 둔 스프라이트가 재임포트 때 초기화되면 안 된다.
// ============================================================
public class PixelThemeImporter : AssetPostprocessor
{
    const string StatIconDir = "Assets/_project/3.Textures/Icons/Stats/";

    // .meta 없이 들어온 9-slice 의 경계 (left, bottom, right, top) — 그림을 보고 잡았다.
    //  Codex 는 .meta 를 만들지 않으므로 경계가 필요한 새 그림은 여기에 한 줄 추가한다.
    static readonly System.Collections.Generic.Dictionary<string, Vector4> Borders = new()
    {
        ["ui_info_panel_9slice"] = new Vector4(20f, 20f, 20f, 20f),   // 모서리 사각 장식까지
        ["ui_header_row"]        = new Vector4(84f, 0f, 84f, 0f),     // 좌우 금 꺾쇠 — 가로로만 늘린다
    };

    void OnPreprocessTexture()
    {
        // 스탯 아이콘도 Codex 가 .meta 없이 넣는다 — 같은 규칙으로 들인다 (ui_icon_ 처럼 Bilinear)
        bool statIcon = assetPath.StartsWith(StatIconDir);
        if (!assetPath.StartsWith(EditorUIBuilder.PixelThemeDir) && !statIcon) return;
        var ti = (TextureImporter)assetImporter;

        // ⚠ 경계 표는 .meta 가 있어도 매번 적용한다 (2026-10-02)
        //   처음엔 '새 파일일 때만' 걸었는데, 이 코드가 컴파일되기 전에 .meta 가 먼저 생겨
        //   경계 0 으로 굳었다 → 정보 칸 그림이 칸 크기만큼 통째로 늘어나 테두리가 글자를 덮었다.
        if (Borders.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(assetPath), out var border))
            ti.spriteBorder = border;

        if (!assetImporter.importSettingsMissing) return;

        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        // ⚠ 아이콘(ui_icon_*)만 Bilinear — 128 원본을 30~76 으로 '줄여' 쓴다.
        //   Point 로 줄이면 픽셀이 들쭉날쭉 빠져 외곽선이 끊겨 보인다 (Icons/Stats 와 같은 설정).
        //   프레임·버튼·걸이는 원본 크기 그대로 그리므로 Point 가 맞다.
        ti.filterMode          = statIcon || System.IO.Path.GetFileName(assetPath).StartsWith("ui_icon_")
                                 ? FilterMode.Bilinear : FilterMode.Point;
        ti.mipmapEnabled       = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression  = TextureImporterCompression.Uncompressed;
    }
}
