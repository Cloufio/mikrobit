using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central, editable catalogue for the microplastic combo achievements.
/// A collected material is never consumed, so one cleanup can contribute to
/// every matching recipe and the player is free to collect materials in any order.
/// </summary>
public sealed class MicroplasticComboTracker : MonoBehaviour
{
    public enum MaterialType
    {
        Fiber,
        Fragment,
        Pellet,
        Film,
        Foam,
        SeaWater,
        GroundWater,
        UsedPlasticBottle,
        SeaFish,
        VehicleTire,
        TeaBag,
        LungOrOrgan,
        MicrowaveContainer,
        SeaSalt,
        ShipOrWallPaint,
        PlasticStraw
    }

    [Serializable]
    public sealed class MaterialRequirement
    {
        public MaterialType material;
        [Min(1)] public int amount;

        public MaterialRequirement(MaterialType material, int amount)
        {
            this.material = material;
            this.amount = amount;
        }
    }

    [Serializable]
    public sealed class ComboDefinition
    {
        public string id;
        public string title;
        [TextArea(2, 4)] public string requirement;
        [TextArea(4, 8)] public string funFact;
        public MaterialRequirement[] materials;

        public ComboDefinition(string id, string title, string requirement, string funFact, params MaterialRequirement[] materials)
        {
            this.id = id;
            this.title = title;
            this.requirement = requirement;
            this.funFact = funFact;
            this.materials = materials;
        }
    }

    private const string UnlockKeyPrefix = "Microbit.ComboUnlocked.";
    private const string AchievementResetVersionKey = "Microbit.AchievementResetVersion";
    private const int AchievementResetVersion = 1;
    private static readonly List<ComboDefinition> Definitions = new()
    {
        new ComboDefinition("microfiber-clutter", "Microfiber Clutter", "3x Fiber",
            "Saat pakaian polyester atau nilon dicuci, serat halusnya bisa lepas ke air. Dalam satu kali cuci, jumlahnya dapat mencapai lebih dari 700.000 serat mikroplastik.",
            new MaterialRequirement(MaterialType.Fiber, 3)),
        new ComboDefinition("shattered-bottle", "Shattered Bottle", "2x Fragment + 1x Used Plastic Bottle",
            "Botol plastik yang mengapung di laut terus terkena matahari dan ombak. Lama-lama botolnya retak lalu pecah menjadi serpihan kecil yang tajam.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.UsedPlasticBottle, 1)),
        new ComboDefinition("microbead-scrub", "Microbead Scrub", "3x Pellet (Nurdle)",
            "Butiran kecil ini dulu sering ada di sabun wajah berscrub. Saat dibilas, ukurannya terlalu kecil untuk tertahan di penyaring air lalu ikut mengalir ke sungai.",
            new MaterialRequirement(MaterialType.Pellet, 3)),
        new ComboDefinition("degraded-plastic-sheet", "Degraded Plastic Sheet", "2x Film + 1x Seawater",
            "Kantong kresek bisa bertahan sampai 500 tahun. Di laut, plastik ini makin rapuh dan pecah menjadi potongan tipis yang mudah dimakan penyu.",
            new MaterialRequirement(MaterialType.Film, 2), new MaterialRequirement(MaterialType.SeaWater, 1)),
        new ComboDefinition("styrofoam-dust", "Styrofoam Dust", "3x Foam (Styrofoam)",
            "Gabus makanan mudah hancur menjadi remah ringan. Karena sangat ringan, remah ini gampang terbawa angin lalu masuk ke saluran air dan laut.",
            new MaterialRequirement(MaterialType.Foam, 3)),
        new ComboDefinition("contaminated-fish", "Contaminated Fish", "2x Pellet + 1x Sea Fish",
            "Ikan bisa mengira butiran plastik sebagai telur ikan atau plankton. Saat tertelan, plastiknya dapat menumpuk di perut ikan dan ikut masuk ke rantai makanan.",
            new MaterialRequirement(MaterialType.Pellet, 2), new MaterialRequirement(MaterialType.SeaFish, 1)),
        new ComboDefinition("tire-rubber-dust", "Tire Rubber Dust", "2x Fragment + 1x Vehicle Tire",
            "Gesekan ban dengan jalan menghasilkan serpihan karet yang sangat kecil. Saat hujan, serpihan ini dapat terbawa ke selokan dan menjadi sumber mikroplastik di daratan.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.VehicleTire, 1)),
        new ComboDefinition("synthetic-tea-bag", "Synthetic Tea Bag", "2x Film + 1x Tea Bag",
            "Sebagian kantong teh dibuat dengan bahan plastik sintetis. Ketika diseduh air panas, partikel plastik yang sangat kecil bisa ikut masuk ke minuman.",
            new MaterialRequirement(MaterialType.Film, 2), new MaterialRequirement(MaterialType.TeaBag, 1)),
        new ComboDefinition("microplastic-soup", "Microplastic Soup", "1x Pellet + 1x Fragment + 1x Fiber",
            "Berbagai mikroplastik di laut bisa ikut masuk ke tubuh manusia lewat makanan dan minuman. Rata-rata jumlahnya diperkirakan sekitar 5 gram setiap minggu, kira-kira seberat satu kartu kredit.",
            new MaterialRequirement(MaterialType.Pellet, 1), new MaterialRequirement(MaterialType.Fragment, 1), new MaterialRequirement(MaterialType.Fiber, 1)),
        new ComboDefinition("inhaled-fiber", "Inhaled Fiber", "2x Fiber + 1x Lung / Organ",
            "Serat dari pakaian sintetis dan karpet bisa melayang di udara. Ukurannya sangat kecil sehingga sebagian dapat terhirup dan masuk ke paru-paru.",
            new MaterialRequirement(MaterialType.Fiber, 2), new MaterialRequirement(MaterialType.LungOrOrgan, 1)),
        new ComboDefinition("leached-plastic", "Leached Plastic", "2x Fragment + 1x Microwave Plastic Container",
            "Wadah plastik yang dipanaskan di microwave bisa mengalami retakan sangat kecil. Retakan ini dapat melepaskan partikel plastik ke makanan.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.MicrowaveContainer, 1)),
        new ComboDefinition("plastified-salt", "Plastified Salt", "2x Pellet + 1x Sea Salt",
            "Garam yang dibuat dari air laut bisa mengandung jejak mikroplastik. Partikelnya berasal dari pencemaran yang sudah ada di perairan tempat garam dibuat.",
            new MaterialRequirement(MaterialType.Pellet, 2), new MaterialRequirement(MaterialType.SeaSalt, 1)),
        new ComboDefinition("paint-flakes", "Paint Flakes", "2x Fragment + 1x Ship or Wall Paint",
            "Cat dinding dan cat kapal yang mengelupas bisa berubah menjadi serpihan plastik kecil. Serpihan ini dapat hanyut lalu mengotori laut tanpa mudah terlihat.",
            new MaterialRequirement(MaterialType.Fragment, 2), new MaterialRequirement(MaterialType.ShipOrWallPaint, 1)),
        new ComboDefinition("brittle-trash", "Brittle Trash", "2x Foam + 1x Plastic Straw",
            "Sedotan dan styrofoam yang terus terkena panas matahari akan makin rapuh. Saat patah, keduanya berubah menjadi partikel kecil yang tajam.",
            new MaterialRequirement(MaterialType.Foam, 2), new MaterialRequirement(MaterialType.PlasticStraw, 1)),
        new ComboDefinition("toxic-leachate", "Toxic Leachate", "1x Film + 1x Fragment + 1x Groundwater",
            "Saat hujan turun di tempat pembuangan sampah, airnya bisa membawa serpihan plastik masuk ke tanah. Air sumur di sekitar tempat itu pun berisiko ikut tercemar.",
            new MaterialRequirement(MaterialType.Film, 1), new MaterialRequirement(MaterialType.Fragment, 1), new MaterialRequirement(MaterialType.GroundWater, 1))
    };

    private static MicroplasticComboTracker instance;
    private readonly Dictionary<MaterialType, int> collectedMaterials = new();
    private static readonly List<string> newlyUnlockedThisRun = new();

    public static event Action<ComboDefinition> ComboUnlocked;
    public static IReadOnlyList<ComboDefinition> AllDefinitions => Definitions;
    /// <summary>Achievement IDs that became unlocked during the current playthrough only.</summary>
    public static IReadOnlyList<string> NewlyUnlockedThisRun => newlyUnlockedThisRun;

    /// <summary>Starts a fresh run without changing the player's permanent unlocks.</summary>
    public static void BeginRun()
    {
        if (instance == null)
        {
            CreateRuntimeTracker();
        }

        newlyUnlockedThisRun.Clear();
        instance.collectedMaterials.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeTracker()
    {
        if (instance != null)
        {
            return;
        }

        GameObject trackerObject = new("Microplastic Combo Tracker");
        instance = trackerObject.AddComponent<MicroplasticComboTracker>();
        UnityEngine.Object.DontDestroyOnLoad(trackerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        ResetLegacyUnlocksOnce();
    }

    /// <summary>Called only when a trash object has actually been cleaned.</summary>
    public static void RecordTrashCollected(string trashObjectName)
    {
        if (instance == null)
        {
            CreateRuntimeTracker();
        }

        if (!TryClassifyTrash(trashObjectName, out MaterialType material))
        {
            return;
        }

        instance.Record(material);
    }

    public static bool IsUnlocked(string id)
    {
        ResetLegacyUnlocksOnce();
        return !string.IsNullOrWhiteSpace(id) && PlayerPrefs.GetInt(UnlockKeyPrefix + id, 0) == 1;
    }

    public static int GetCollectedCount(MaterialType material)
    {
        return instance != null && instance.collectedMaterials.TryGetValue(material, out int count) ? count : 0;
    }

    [ContextMenu("Reset Current Run Collection")]
    public void ResetCurrentRunCollection()
    {
        collectedMaterials.Clear();
    }

    [ContextMenu("Reset All Combo Unlocks")]
    public void ResetAllComboUnlocks()
    {
        foreach (ComboDefinition definition in Definitions)
        {
            PlayerPrefs.DeleteKey(UnlockKeyPrefix + definition.id);
        }

        PlayerPrefs.Save();
    }

    private static void ResetLegacyUnlocksOnce()
    {
        if (PlayerPrefs.GetInt(AchievementResetVersionKey, 0) >= AchievementResetVersion)
        {
            return;
        }

        foreach (ComboDefinition definition in Definitions)
        {
            PlayerPrefs.DeleteKey(UnlockKeyPrefix + definition.id);
        }

        PlayerPrefs.SetInt(AchievementResetVersionKey, AchievementResetVersion);
        PlayerPrefs.Save();
    }

    private void Record(MaterialType material)
    {
        collectedMaterials.TryGetValue(material, out int amount);
        collectedMaterials[material] = amount + 1;

        foreach (ComboDefinition definition in Definitions)
        {
            if (IsUnlocked(definition.id) || !RequirementsMet(definition))
            {
                continue;
            }

            PlayerPrefs.SetInt(UnlockKeyPrefix + definition.id, 1);
            PlayerPrefs.Save();
            newlyUnlockedThisRun.Add(definition.id);
            ComboUnlocked?.Invoke(definition);
            Debug.Log($"Unlocked microplastic combo: {definition.title}");
        }
    }

    private bool RequirementsMet(ComboDefinition definition)
    {
        foreach (MaterialRequirement requirement in definition.materials)
        {
            if (!collectedMaterials.TryGetValue(requirement.material, out int amount) || amount < requirement.amount)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryClassifyTrash(string objectName, out MaterialType material)
    {
        string key = objectName.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("(clone)", string.Empty);

        if (key.Contains("fiber") || key.Contains("serat")) { material = MaterialType.Fiber; return true; }
        if (key.Contains("pellet") || key.Contains("nurdle") || key.Contains("microbead")) { material = MaterialType.Pellet; return true; }
        if (key.Contains("film")) { material = MaterialType.Film; return true; }
        if (key.Contains("foam") || key.Contains("styro") || key.Contains("gabus")) { material = MaterialType.Foam; return true; }
        if (key.Contains("airlaut") || key.Contains("seawater")) { material = MaterialType.SeaWater; return true; }
        if (key.Contains("airtanah") || key.Contains("groundwater")) { material = MaterialType.GroundWater; return true; }
        if (key.Contains("botolplastik") || key.Contains("plasticbottle") || key.Contains("bottle")) { material = MaterialType.UsedPlasticBottle; return true; }
        if (key.Contains("ikanlaut") || key.Contains("ikanmati") || key.Contains("seafish")) { material = MaterialType.SeaFish; return true; }
        if (key.Contains("bankendaraan") || key.Contains("rubbertire") || key.Contains("tire")) { material = MaterialType.VehicleTire; return true; }
        if (key.Contains("kantongteh") || key.Contains("teabag")) { material = MaterialType.TeaBag; return true; }
        if (key.Contains("paru") || key.Contains("lung") || key.Contains("organ")) { material = MaterialType.LungOrOrgan; return true; }
        if (key.Contains("wadahplastikmicrowave") || key.Contains("microwave")) { material = MaterialType.MicrowaveContainer; return true; }
        if (key.Contains("garamlaut") || key.Contains("seasalt")) { material = MaterialType.SeaSalt; return true; }
        if (key.Contains("catkapal") || key.Contains("cepatkapal") || key.Contains("paint")) { material = MaterialType.ShipOrWallPaint; return true; }
        if (key.Contains("sedotan") || key.Contains("straw")) { material = MaterialType.PlasticStraw; return true; }
        if (key.Contains("fragment") || key.Contains("serpihan")) { material = MaterialType.Fragment; return true; }

        material = default;
        return false;
    }
}
