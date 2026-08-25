#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  RelicTreeIconStubs.cs  [Editor Only]
//  유물 트리 노드 69종의 **자리표시 PNG** 를 굽는다.
//
//  ■ 무엇을 위한 것인가
//    진짜 그림은 밖에서(코덱스) `Docs/RelicTree_Icon_Spec.md` 를 보고 만들어
//    이 폴더에 **같은 파일명으로 덮어씌운다.**
//    이 도구는 그 "덮어씌울 자리" 를 미리 만들어 두는 역할만 한다 —
//    파일이 있어야 아틀라스가 잡히고, 연동이 맞는지 그림 없이도 확인할 수 있다.
//
//  ■ 자리표시가 그리는 것
//    계열 색 원반 + 티어 수만큼의 눈금.
//    "그림이 아직 없다" 가 한눈에 보여야 하므로 일부러 단순하게 둔다.
//    진짜 그림과 헷갈리면 어느 것이 남았는지 셀 수 없다.
//
//  ⚠ 이미 있는 파일은 건드리지 않는다 (기본)
//    진짜 그림을 넣은 자리를 자리표시로 되돌리면 작업이 통째로 날아간다.
//    전부 다시 깔려면 '자리표시 강제 재생성' 을 쓸 것.
//
//  메뉴: Tools > Project K > 아이콘·텍스처 > 유물 트리 …
// ============================================================

public static class RelicTreeIconStubs
{
    const int Size = 128;   // Docs/RelicTree_Icon_Spec.md 의 최종 해상도와 같다

    // ── 메뉴 ─────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Icon + "유물 트리 자리표시 생성 (빈 자리만)",
              priority = ProjectKMenu.IconPrio + 60)]
    public static void GenerateMissing() => Generate(overwrite: false);

    [MenuItem(ProjectKMenu.Icon + "유물 트리 자리표시 강제 재생성 (전부 덮어씀)",
              priority = ProjectKMenu.IconPrio + 61)]
    public static void GenerateAll()
    {
        if (!EditorUtility.DisplayDialog(
                "자리표시 강제 재생성",
                "69장을 전부 자리표시로 덮어씁니다.\n" +
                "이미 넣어 둔 진짜 그림도 사라집니다. 계속할까요?",
                "덮어쓴다", "취소"))
            return;

        Generate(overwrite: true);
    }

    [MenuItem(ProjectKMenu.Icon + "유물 트리 아이콘 임포트 재적용",
              priority = ProjectKMenu.IconPrio + 62)]
    public static void ReimportAll()
    {
        int n = 0;
        foreach (var id in AllNodeIds())
        {
            string path = RelicIconKey.PathOf(id);
            if (!File.Exists(path)) continue;

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            // 설정의 정본은 IconImportSetup 이다 — 아이콘 폴더 전체가 같은 규칙을 쓴다.
            IconImportSetup.Apply(ti, path);
            ti.SaveAndReimport();
            n++;
        }
        Debug.Log($"[RelicTreeIconStubs] 임포트 설정 재적용 — {n}장.");
    }

    /// <summary>
    /// 파일이 하나도 없는 노드를 콘솔에 나열한다.
    /// 진짜 그림이 몇 장 들어왔는지 세는 용도 — 자리표시와 진짜를 구분하지는 못한다.
    /// </summary>
    [MenuItem(ProjectKMenu.Icon + "유물 트리 아이콘 누락 점검",
              priority = ProjectKMenu.IconPrio + 63)]
    public static void ReportMissing()
    {
        var missing = new List<string>();
        foreach (var id in AllNodeIds())
            if (!File.Exists(RelicIconKey.PathOf(id))) missing.Add(RelicIconKey.Of(id));

        if (missing.Count == 0)
        {
            Debug.Log($"[RelicTreeIconStubs] 69종 전부 있음 ({RelicIconKey.FolderPath}).");
            return;
        }
        Debug.LogWarning($"[RelicTreeIconStubs] 누락 {missing.Count}장:\n  " +
                         string.Join("\n  ", missing));
    }

    // ── 생성 ─────────────────────────────────────────────────

    static void Generate(bool overwrite)
    {
        Directory.CreateDirectory(RelicIconKey.FolderPath);

        int made = 0, kept = 0;
        foreach (var def in RelicTreeCatalog.All)
        {
            string path = RelicIconKey.PathOf(def.Id);
            if (!overwrite && File.Exists(path)) { kept++; continue; }

            File.WriteAllBytes(path, Draw(def).EncodeToPNG());
            made++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[RelicTreeIconStubs] 자리표시 {made}장 생성, {kept}장 유지 " +
                  $"→ {RelicIconKey.FolderPath}");
    }

    /// <summary>계열 색 원반 + 티어 눈금. 진짜 그림과 헷갈리지 않게 일부러 단순하다.</summary>
    static Texture2D Draw(RelicNodeDef def)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var px  = new Color32[Size * Size];   // 기본값 = 완전 투명

        Color c  = RelicTreePopup.ColorOf(def.Branch);
        var  fill = (Color32)c;
        var  line = (Color32)(c * 0.35f);
        float cx = (Size - 1) * 0.5f, cy = (Size - 1) * 0.5f;

        // 원반 — 반지름은 명세의 '중앙 주제 88~104px' 안에 들어간다
        const float R = 44f, Ring = 3f;
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d > R) continue;
            px[y * Size + x] = d > R - Ring ? line : fill;
        }

        // 티어 눈금 — 아래쪽에 티어 수만큼. 어느 깊이의 노드인지 자리표시에서도 읽히게.
        int  tier = Mathf.Clamp(def.Tier, 0, 5);
        const int TickW = 10, TickH = 5, Gap = 3;
        int total = tier * TickW + Mathf.Max(0, tier - 1) * Gap;
        int startX = Mathf.RoundToInt(cx - total * 0.5f);
        for (int t = 0; t < tier; t++)
        for (int y = 12; y < 12 + TickH; y++)
        for (int x = startX + t * (TickW + Gap); x < startX + t * (TickW + Gap) + TickW; x++)
            if (x >= 0 && x < Size) px[y * Size + x] = line;

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static IEnumerable<RelicNodeId> AllNodeIds()
    {
        foreach (var def in RelicTreeCatalog.All) yield return def.Id;
    }
}
#endif
