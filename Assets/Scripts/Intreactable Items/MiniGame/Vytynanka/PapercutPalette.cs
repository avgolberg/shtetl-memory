using System.Collections.Generic;
using UnityEngine;

//Colours of the Vytynanka papercut: the paper that is cut and the backing paper it is mounted on.
//Both choices are saved as story flags (vytynanka_paper_<id>, vytynanka_mount_<id>); the item rebuilds its picture from them.
public static class PapercutPalette
{
    public const string MadeFlag = "vytynanka_made";
    public const string PaperFlagPrefix = "vytynanka_paper_";
    public const string MountFlagPrefix = "vytynanka_mount_";

    public static readonly (string id, Color color)[] Papers =
    {
        ("cream", new Color32(240, 228, 196, 255)),
        ("blue", new Color32(120, 160, 205, 255)),
        ("red", new Color32(185, 52, 48, 255)),
        ("green", new Color32(110, 155, 105, 255)),
    };

    //The backing paper; the one too close to the cut paper's colour is not offered
    public static readonly (string id, Color color, string tooClose)[] Mounts =
    {
        ("ivory", new Color32(246, 240, 224, 255), "cream"),
        ("darkred", new Color32(118, 30, 34, 255), "red"),
        ("navy", new Color32(32, 46, 88, 255), "blue"),
        ("forest", new Color32(36, 74, 48, 255), "green"),
        ("black", new Color32(30, 27, 26, 255), null),
    };

    public static Color PaperColor(string id)
    {
        foreach (var p in Papers)
            if (p.id == id) return p.color;
        return Papers[0].color;
    }

    public static Color MountColor(string id)
    {
        foreach (var m in Mounts)
            if (m.id == id) return m.color;
        return Mounts[Mounts.Length - 1].color;
    }

    public static bool MountAllowed(string mountId, string paperId)
    {
        foreach (var m in Mounts)
            if (m.id == mountId) return m.tooClose != paperId;
        return false;
    }

    //First backing paper that contrasts with the cut paper
    public static string DefaultMount(string paperId)
    {
        return paperId == "cream" ? "darkred" : "ivory";
    }

    //The saved choices of the kept papercut (defaults if a flag is missing)
    public static (string paper, string mount) SavedChoice()
    {
        SaveController save = SaveController.Instance;
        string paper = Papers[0].id, mount = DefaultMount(paper);
        if (save == null) return (paper, mount);

        foreach (var p in Papers)
            if (save.HasFlag(PaperFlagPrefix + p.id)) paper = p.id;
        mount = DefaultMount(paper);
        foreach (var m in Mounts)
            if (save.HasFlag(MountFlagPrefix + m.id)) mount = m.id;
        return (paper, mount);
    }

    //Exactly one paper flag and one backing flag: the papercut the player keeps now
    public static void SaveChoice(string paper, string mount)
    {
        ClearChoice();
        SaveController.Instance.SetFlag(PaperFlagPrefix + paper);
        SaveController.Instance.SetFlag(MountFlagPrefix + mount);
    }

    public static void ClearChoice()
    {
        foreach (var p in Papers) SaveController.Instance.ClearFlag(PaperFlagPrefix + p.id);
        foreach (var m in Mounts) SaveController.Instance.ClearFlag(MountFlagPrefix + m.id);
    }

    public static IEnumerable<string> AllFlags()
    {
        yield return MadeFlag;
        foreach (var p in Papers) yield return PaperFlagPrefix + p.id;
        foreach (var m in Mounts) yield return MountFlagPrefix + m.id;
    }
}
