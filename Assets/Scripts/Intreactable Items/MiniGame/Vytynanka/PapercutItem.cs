using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//"My vytynanka" in the inventory: the player's papercut on its backing paper, in the colours they chose.
//The picture is built from the baked silhouette mask and the two saved colours (PapercutPalette), so nothing else is saved.
[RequireComponent(typeof(Image))]
public class PapercutItem : MonoBehaviour
{
    [SerializeField] private Texture2D iconMask; //Small readable copy of the silhouette (VytynankaSetup bakes it)

    private const int IconSize = 256;
    private static readonly Dictionary<string, Sprite> cache = new();

    //On load the item is created from the prefab; it draws the kept papercut from the saved colours
    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var (paper, mount) = PapercutPalette.SavedChoice();
        Sprite icon = BuildIcon(paper, mount);
        if (icon != null)
            GetComponent<Image>().sprite = icon;
    }

    //After a new papercut replaced the old one (the inventory may be closed, so inactive items too)
    public static void RefreshAll()
    {
        foreach (var item in FindObjectsByType<PapercutItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            item.Refresh();
    }

    //A small mounted papercut: the backing sheet with a soft shadow, the papercut on it
    public Sprite BuildIcon(string paper, string mount)
    {
        if (iconMask == null || !iconMask.isReadable)
            return null;

        string key = paper + "/" + mount;
        if (cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Color paperColor = PapercutPalette.PaperColor(paper);
        Color mountColor = PapercutPalette.MountColor(mount);

        float aspect = (float)iconMask.width / iconMask.height;
        //The backing sheet nearly fills the icon; the papercut sits on it with an even margin (about 7%)
        int sheetH = Mathf.RoundToInt(IconSize * 0.95f);
        int sheetW = Mathf.RoundToInt(sheetH * aspect);
        int sheetX = (IconSize - sheetW) / 2, sheetY = (IconSize - sheetH) / 2 + 3;
        int cutH = Mathf.RoundToInt(sheetH * 0.86f), cutW = Mathf.RoundToInt(cutH * aspect);
        int cutX = (IconSize - cutW) / 2, cutY = sheetY + (sheetH - cutH) / 2;

        var pixels = new Color[IconSize * IconSize];
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                Color c = Color.clear;

                //Shadow of the sheet, then the sheet
                if (Inside(x - 4, y + 4, sheetX, sheetY, sheetW, sheetH))
                    c = new Color(0f, 0f, 0f, 0.35f);
                if (Inside(x, y, sheetX, sheetY, sheetW, sheetH))
                    c = mountColor;

                if (Inside(x, y, cutX - 2, cutY - 2, cutW + 4, cutH + 4))
                {
                    float shadow = MaskAt(x - 2, y + 2, cutX, cutY, cutW, cutH) * 0.35f;
                    c = Color.Lerp(c, Color.black, shadow);
                    float alpha = MaskAt(x, y, cutX, cutY, cutW, cutH);
                    c = Color.Lerp(c, paperColor, alpha);
                }

                pixels[y * IconSize + x] = c;
            }
        }

        var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false) { name = "Papercut " + key };
        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f));
        cache[key] = sprite;
        return sprite;
    }

    private static bool Inside(int x, int y, int rx, int ry, int rw, int rh)
    {
        return x >= rx && x < rx + rw && y >= ry && y < ry + rh;
    }

    private float MaskAt(int x, int y, int rx, int ry, int rw, int rh)
    {
        if (!Inside(x, y, rx, ry, rw, rh))
            return 0f;
        return iconMask.GetPixelBilinear((x - rx + 0.5f) / rw, (y - ry + 0.5f) / rh).a;
    }
}
