using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地面掉落物：金币/装备以发光旋转的宝珠呈现，颜色按品质区分，带名称浮字。
/// 装备在击杀时已入包，宝珠提供可见反馈；玩家走近或超时后自动消散。
/// </summary>
public class LootDrop : MonoBehaviour
{
    public enum Kind { Gold, Item }

    public static readonly List<LootDrop> All = new List<LootDrop>();

    Kind kind;
    string itemId;
    int rarity;
    SpriteRenderer orb;
    SpriteRenderer halo;
    float baseY;
    float anim;
    float life;
    Color tint;

    public static void Spawn(Vector3 at, Kind kind, string itemId, int rarity, string text)
    {
        GameObject go = new GameObject(kind == Kind.Gold ? "LootGold" : "LootItem");
        LootDrop drop = go.AddComponent<LootDrop>();
        drop.Setup(at, kind, itemId, rarity, text);
    }

    void Setup(Vector3 at, Kind kind, string itemId, int rarity, string text)
    {
        this.kind = kind;
        this.itemId = itemId;
        this.rarity = rarity;
        tint = kind == Kind.Gold
            ? new Color(0.98f, 0.78f, 0.32f)
            : GameConfig.RarityColor(rarity);

        baseY = GameConfig.GroundY + 0.45f;
        float x = at.x + Random.Range(-0.45f, 0.45f);
        transform.position = new Vector3(x, baseY, 0f);

        GameObject haloGo = new GameObject("Halo");
        haloGo.transform.SetParent(transform, false);
        halo = haloGo.AddComponent<SpriteRenderer>();
        halo.sprite = ResourceFactory.GlowSprite;
        halo.color = new Color(tint.r, tint.g, tint.b, 0.55f);
        haloGo.transform.localScale = Vector3.one * (kind == Kind.Gold ? 0.75f : 1.05f);
        halo.sortingOrder = 14;

        GameObject orbGo = new GameObject("Orb");
        orbGo.transform.SetParent(transform, false);
        orb = orbGo.AddComponent<SpriteRenderer>();
        orb.sprite = ResourceFactory.GlowSprite;
        orb.color = tint;
        orbGo.transform.localScale = Vector3.one * (kind == Kind.Gold ? 0.34f : 0.48f);
        orb.sortingOrder = 15;

        // 文字反馈走 HUD 的 EventBus 飘字（世界空间 TextMesh 在部分环境下不出字）
        life = 9f;
        anim = Random.Range(0f, 6f);
        All.Add(this);
    }

    void Update()
    {
        anim += Time.deltaTime;
        life -= Time.deltaTime;

        Vector3 p = transform.position;
        p.y = baseY + 0.12f * Mathf.Sin(anim * 3f);
        transform.position = p;

        float pulse = 0.85f + 0.15f * Mathf.Sin(anim * 5f);
        orb.transform.localScale = Vector3.one * (kind == Kind.Gold ? 0.34f : 0.48f) * pulse;
        Color hc = halo.color;
        hc.a = 0.38f + 0.18f * (0.5f + 0.5f * Mathf.Sin(anim * 4f));
        halo.color = hc;

        // 玩家走近即消散（装备已入包，这里只收掉视觉提示）
        GameController gc = GameController.Instance;
        if (gc != null && gc.Player != null &&
            Mathf.Abs(gc.Player.transform.position.x - transform.position.x) < 1.1f)
            life = Mathf.Min(life, 0.35f);

        if (life < 0.5f)
        {
            float a = Mathf.Clamp01(life / 0.5f);
            orb.color = new Color(tint.r, tint.g, tint.b, a);
            hc.a *= a;
            halo.color = hc;
        }
        if (life <= 0f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    public static void ClearAll()
    {
        for (int i = All.Count - 1; i >= 0; i--)
            if (All[i] != null) Destroy(All[i].gameObject);
        All.Clear();
    }
}
