using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Bakes the finished Vytynanka papercut (white paper, transparent openings) from the existing art, without new drawing:
// - the template line drawing (TemplateGhostImage) gives the curves: its lines split the sheet into regions;
// - the owner's traced cut lines (TracePath dots) decide which regions are paper: inside the outline (Path0, closed along the
//   fold and the bottom edge) and outside every hole line is paper, the rest is cut away;
// - each region takes the majority vote (every traced hole lands on its drawn region; checked 2026-10-31);
// - the drawn lines: a line between two different openings stays paper, a thin bridge exactly as drawn (otherwise the petals of
//   a flower merge into one big opening); a line between paper and an opening is split down the middle; other lines are paper;
// - a light smoothing (it keeps the thin bridges and small openings), then the half is mirrored.
// Writes Generated/papercut_silhouette.png (Sprite, for the mini-game) and papercut_icon_mask.png (small, readable, for the item).
// A sharper result needs only a higher-resolution copy of the same template on TemplateGhostImage, then a re-run.
public static class PapercutSilhouetteBaker
{
    const string OutDir = "Assets/Sprites/Items/Vytynanka/Generated";
    public const string SilhouettePath = OutDir + "/papercut_silhouette.png";
    public const string IconMaskPath = OutDir + "/papercut_icon_mask.png";
    const int HalfWidth = 700; //Pixels of the half sheet; the full papercut is twice as wide
    const int IconHeight = 240;
    const float LineThreshold = 215f / 255f; //Darker template pixels are drawn lines
    const float FoldSnap = 5f; //Canvas units: dots this close to the fold are on it

    public static (Sprite silhouette, Texture2D iconMask) Bake(Image ghost, Transform pathsParent)
    {
        RectTransform ghostRect = ghost.rectTransform;
        Rect rect = ghostRect.rect;
        int w = HalfWidth, h = Mathf.RoundToInt(HalfWidth * rect.height / rect.width);
        float unit = w / rect.width;

        bool[] line = ReadLines(ghost.sprite, w, h);
        bool[] vector = TracedPaper(ghostRect, pathsParent, w, h, unit);
        bool[] paper = Classify(line, vector, w, h);

        float[] alpha = paper.Select(p => p ? 1f : 0f).ToArray();
        BoxBlur(alpha, w, h, 1, 2);
        for (int i = 0; i < alpha.Length; i++) alpha[i] = alpha[i] >= 0.5f ? 1f : 0f;
        BoxBlur(alpha, w, h, 1, 2);

        Directory.CreateDirectory(OutDir);
        var full = new Color32[w * 2 * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[y * w + x]) * 255f);
                var c = new Color32(255, 255, 255, a);
                full[y * w * 2 + w + x] = c;     //The cut half: right of the fold
                full[y * w * 2 + w - 1 - x] = c; //Its mirror
            }
        }
        WritePng(SilhouettePath, full, w * 2, h);
        WritePng(IconMaskPath, Downsample(full, w * 2, h, IconHeight), Mathf.RoundToInt(IconHeight * w * 2f / h), IconHeight);

        ImportSilhouette();
        ImportIconMask();
        return (AssetDatabase.LoadAssetAtPath<Sprite>(SilhouettePath), AssetDatabase.LoadAssetAtPath<Texture2D>(IconMaskPath));
    }

    //Drawn lines of the template, stretched to the sheet like the UI shows it
    static bool[] ReadLines(Sprite sprite, int w, int h)
    {
        string path = AssetDatabase.GetAssetPath(sprite.texture);
        var source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(path)); //Readable copy; the imported texture stays as it is
        Rect r = sprite.rect;
        float sx = (float)source.width / sprite.texture.width, sy = (float)source.height / sprite.texture.height;

        var line = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (r.x + (x + 0.5f) / w * r.width) * sx / source.width;
                float v = (r.y + (y + 0.5f) / h * r.height) * sy / source.height;
                Color c = source.GetPixelBilinear(u, v);
                float luminance = c.a < 0.5f ? 1f : 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                line[y * w + x] = luminance < LineThreshold;
            }
        }
        Object.DestroyImmediate(source);
        return line;
    }

    //Paper by the traced cut lines: inside the outline, outside the holes
    static bool[] TracedPaper(RectTransform ghostRect, Transform pathsParent, int w, int h, float unit)
    {
        Rect rect = ghostRect.rect;
        var paths = pathsParent.GetComponentsInChildren<TracePath>(true).OrderBy(p => p.transform.GetSiblingIndex()).ToList();

        List<Vector2> Pixels(TracePath path)
        {
            path.CachePoints();
            return path.Points.Select(p =>
            {
                var rt = (RectTransform)p.transform;
                Vector2 local = ghostRect.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
                float x = (local.x - rect.xMin) * unit, y = (local.y - rect.yMin) * unit;
                if (x < FoldSnap * unit) x = -3f * unit; //On the fold: cut from the fold itself, no seam
                return new Vector2(x, y);
            }).ToList();
        }

        var paper = new bool[w * h];
        var outline = Pixels(paths[0]);
        //Close the outline along the bottom edge and up the fold
        outline.Add(new Vector2(outline[outline.Count - 1].x, -5f * unit));
        outline.Add(new Vector2(-3f * unit, -5f * unit));
        outline.Add(new Vector2(-3f * unit, outline[0].y + 5f * unit));
        Fill(paper, w, h, outline, true);

        foreach (var hole in paths.Skip(1))
            Fill(paper, w, h, Pixels(hole), false);
        return paper;
    }

    static void Fill(bool[] mask, int w, int h, List<Vector2> polygon, bool value)
    {
        var crossings = new List<float>();
        for (int y = 0; y < h; y++)
        {
            float yc = y + 0.5f;
            crossings.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                    crossings.Add(a.x + (yc - a.y) / (b.y - a.y) * (b.x - a.x));
            }
            crossings.Sort();
            for (int i = 0; i + 1 < crossings.Count; i += 2)
            {
                int from = Mathf.Max(0, Mathf.CeilToInt(crossings[i] - 0.5f));
                int to = Mathf.Min(w - 1, Mathf.FloorToInt(crossings[i + 1] - 0.5f));
                for (int x = from; x <= to; x++) mask[y * w + x] = value;
            }
        }
    }

    //Each region between drawn lines is paper if most of it is paper by the traced lines; the lines join their nearest region
    static bool[] Classify(bool[] line, bool[] vector, int w, int h)
    {
        int n = w * h;
        var label = new int[n];
        var votes = new List<int> { 0 }; var sizes = new List<int> { 0 };
        var stack = new Stack<int>();

        for (int start = 0; start < n; start++)
        {
            if (line[start] || label[start] != 0) continue;
            int id = votes.Count, vote = 0, size = 0;
            label[start] = id;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop(); size++;
                if (vector[i]) vote++;
                int x = i % w, y = i / w;
                if (x > 0) Visit(i - 1); if (x < w - 1) Visit(i + 1);
                if (y > 0) Visit(i - w); if (y < h - 1) Visit(i + w);
            }
            votes.Add(vote); sizes.Add(size);

            void Visit(int j)
            {
                if (!line[j] && label[j] == 0) { label[j] = id; stack.Push(j); }
            }
        }

        var paper = new bool[n];
        var known = new bool[n];
        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (label[i] == 0) continue;
            paper[i] = votes[label[i]] * 2 > sizes[label[i]];
            known[i] = true;
            queue.Enqueue(i);
        }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), x = i % w, y = i / w;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int j = ny * w + nx;
                if (known[j]) continue;
                known[j] = true;
                paper[j] = paper[i];
                queue.Enqueue(j);
            }
        }

        //Drawn lines by their neighbours: between two openings a paper bridge, between paper and an opening the nearest side (above)
        var regionPaper = new bool[votes.Count];
        for (int r = 1; r < votes.Count; r++) regionPaper[r] = votes[r] * 2 > sizes[r];
        var holeLabels = new HashSet<int>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!line[i]) continue;

            bool nearPaper = false;
            holeLabels.Clear();
            for (int dy = -BridgeRadius; dy <= BridgeRadius && !nearPaper; dy++)
            for (int dx = -BridgeRadius; dx <= BridgeRadius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int l = label[ny * w + nx];
                if (l == 0) continue;
                if (regionPaper[l]) { nearPaper = true; break; }
                holeLabels.Add(l);
            }

            if (nearPaper) continue;
            paper[i] = holeLabels.Count != 1; //Two openings: bridge; one: inside that opening; none: paper
        }
        return paper;
    }

    const int BridgeRadius = 7; //Pixels: a little over half of a drawn line's width at this size

    static void BoxBlur(float[] data, int w, int h, int radius, int passes)
    {
        var temp = new float[data.Length];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f; int count = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = Mathf.Clamp(x + k, 0, w - 1);
                    sum += data[y * w + xx]; count++;
                }
                temp[y * w + x] = sum / count;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f; int count = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int yy = Mathf.Clamp(y + k, 0, h - 1);
                    sum += temp[yy * w + x]; count++;
                }
                data[y * w + x] = sum / count;
            }
        }
    }

    static Color32[] Downsample(Color32[] source, int w, int h, int targetH)
    {
        int targetW = Mathf.RoundToInt(targetH * (float)w / h);
        var result = new Color32[targetW * targetH];
        for (int y = 0; y < targetH; y++)
        for (int x = 0; x < targetW; x++)
        {
            int x0 = x * w / targetW, x1 = Mathf.Max(x0 + 1, (x + 1) * w / targetW);
            int y0 = y * h / targetH, y1 = Mathf.Max(y0 + 1, (y + 1) * h / targetH);
            int sum = 0, count = 0;
            for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++) { sum += source[yy * w + xx].a; count++; }
            result[y * targetW + x] = new Color32(255, 255, 255, (byte)(sum / count));
        }
        return result;
    }

    static void WritePng(string path, Color32[] pixels, int w, int h)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    static void ImportSilhouette()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SilhouettePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    static void ImportIconMask()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(IconMaskPath);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true; //PapercutItem builds the inventory picture from it
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
