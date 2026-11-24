using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static readonly List<EnemyController> All = new List<EnemyController>();

    public EnemyData Data;
    public HealthComponent Health;
    public bool Dead { get; private set; }

    PlayerController target;
    SpriteRenderer body;
    SpriteRenderer hpFill;
    SpriteRenderer hpGlow;
    SpriteRenderer hpShine;
    float hpBarWidth;
    float hpShown = 1f;
    float hpTarget = 1f;
    bool isBoss;
    float nextAttack;
    float groundY;
    float scale;
    float baseSpeed;
    Vector3 baseVisualScale = Vector3.one * 0.42f; // 精灵基准尺寸，朝向只改符号不累乘

    /// <summary>数据里的敌人 id → 实际素材名。素材只有 6 张，部分 id 复用相近造型。</summary>
    static string SpriteNameFor(string id)
    {
        switch (id)
        {
            case "grunt":    return "enemy_grunt";
            case "charger":  return "enemy_grunt";   // 冲锋兵复用步兵造型，靠体型/颜色区分
            case "ranged":   return "enemy_archer";
            case "bomber":   return "enemy_spore";
            case "shield":   return "enemy_shield";
            case "elite":    return "boss_minor";    // 精英复用小 boss 造型
            case "boss_mini":  return "boss_minor";
            case "boss_final": return "boss_major";
            default: return "enemy_grunt";
        }
    }

    const float PPU = 200f;
    const float TargetHeight = 2.2f; // 普通怪标准世界高度（data.scale 在此之上再放大）

    public void Setup(EnemyData data, PlayerController player, float x, float floor)
    {
        Data = data;
        target = player;
        groundY = floor;
        scale = Mathf.Max(0.6f, data.scale);
        baseSpeed = data.speed * GameConfig.WorldScale;

        Health = gameObject.AddComponent<HealthComponent>();
        Health.IsPlayer = false;
        Health.Defense = data.kind == "shield" ? 4f : 0f;
        Health.Reset(data.hp);
        Health.Changed += OnHealthChanged;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        body = visual.AddComponent<SpriteRenderer>();
        string spritePath = "Enemies/" + SpriteNameFor(data.id);
        body.sprite = ResourceFactory.Sprite(spritePath, PPU);
        body.sortingOrder = 9;
        if (body.sprite == null)
        {
            Debug.LogWarning("[Enemy] 素材缺失: " + spritePath + " (id=" + data.id + ")");
            body.sprite = ResourceFactory.SolidSprite(new Color(
                data.color != null && data.color.Length > 0 ? data.color[0] : 0.5f,
                data.color != null && data.color.Length > 1 ? data.color[1] : 0.5f,
                data.color != null && data.color.Length > 2 ? data.color[2] : 0.5f));
            baseVisualScale = new Vector3(0.7f, 1.4f, 1f) * scale;
        }
        else
        {
            // 按图片实际世界高度归一化到目标高度，消除不同素材尺寸导致的比例差异
            float spriteH = body.sprite.bounds.size.y; // 像素高 / PPU
            float k = spriteH > 0.01f ? (TargetHeight * scale) / spriteH : 1f;
            baseVisualScale = new Vector3(k, k, 1f);
        }
        visual.transform.localScale = baseVisualScale;

        // Boss 的血量走屏幕顶部横条；小怪用头顶浮动血条
        isBoss = data.kind == "boss";
        if (!isBoss) BuildHealthBar();

        transform.position = new Vector3(x, floor + 0.9f * scale, 0f);
        All.Add(this);
    }

    /// <summary>小怪头顶浮动血条：底板 + 光晕 + 左锚点填充 + 高光 + 金属描边 + 刻度。</summary>
    void BuildHealthBar()
    {
        hpBarWidth = 1.35f * Mathf.Clamp(scale, 0.85f, 1.5f);
        float y = 1.1f * scale + 0.28f;

        GameObject root = new GameObject("HPBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, y, 0f);

        // 内凹底槽
        AddBarPiece(root.transform, "Back", Vector3.zero,
            new Vector3(hpBarWidth + 0.16f, 0.25f, 1f),
            new Color(0.05f, 0.04f, 0.04f, 0.92f), 12);
        AddBarPiece(root.transform, "Shadow", new Vector3(0f, 0.045f, 0f),
            new Vector3(hpBarWidth + 0.06f, 0.1f, 1f),
            new Color(0f, 0f, 0f, 0.28f), 12);

        // 柔光 / 填充 / 顶部高光（三者宽度同步，从左端收缩）
        hpGlow = AddBarPiece(root.transform, "Glow", Vector3.zero,
            new Vector3(hpBarWidth, 0.2f, 1f), new Color(0.4f, 0.9f, 0.4f, 0.25f), 13);
        hpFill = AddBarPiece(root.transform, "Fill", Vector3.zero,
            new Vector3(hpBarWidth, 0.15f, 1f), Color.green, 14);
        hpShine = AddBarPiece(root.transform, "Shine", new Vector3(0f, 0.055f, 0f),
            new Vector3(hpBarWidth, 0.035f, 1f), new Color(1f, 1f, 1f, 0.35f), 15);

        // 金色上下包边 + 深色左右端
        Color frameTop = new Color(0.92f, 0.78f, 0.45f, 0.92f);
        Color frameBottom = new Color(0.3f, 0.23f, 0.14f, 0.95f);
        AddBarPiece(root.transform, "FrameTop", new Vector3(0f, 0.135f, 0f),
            new Vector3(hpBarWidth + 0.16f, 0.025f, 1f), frameTop, 16);
        AddBarPiece(root.transform, "FrameBottom", new Vector3(0f, -0.135f, 0f),
            new Vector3(hpBarWidth + 0.16f, 0.025f, 1f), frameBottom, 16);
        AddBarPiece(root.transform, "FrameLeft", new Vector3(-(hpBarWidth + 0.16f) * 0.5f, 0f, 0f),
            new Vector3(0.035f, 0.28f, 1f), frameTop, 16);
        AddBarPiece(root.transform, "FrameRight", new Vector3((hpBarWidth + 0.16f) * 0.5f, 0f, 0f),
            new Vector3(0.035f, 0.28f, 1f), frameBottom, 16);

        for (int i = 1; i <= 3; i++)
        {
            float tx = -hpBarWidth * 0.5f + hpBarWidth * i / 4f;
            AddBarPiece(root.transform, "Tick" + i, new Vector3(tx, 0f, 0f),
                new Vector3(0.018f, 0.15f, 1f), new Color(0.04f, 0.03f, 0.02f, 0.34f), 17);
        }

        hpTarget = hpShown = Mathf.Clamp01(Health.Ratio);
        ApplyHealthBar();
    }

    static SpriteRenderer AddBarPiece(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResourceFactory.WhiteSolid;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>按当前显示比例刷新宽度与颜色；扣血/回血都会平滑过渡。</summary>
    void ApplyHealthBar()
    {
        if (hpFill == null) return;
        float w = hpBarWidth * Mathf.Max(0.001f, hpShown);
        float left = -(hpBarWidth - w) * 0.5f;
        hpFill.transform.localScale = new Vector3(w, 0.15f, 1f);
        hpFill.transform.localPosition = new Vector3(left, 0f, 0f);
        hpGlow.transform.localScale = new Vector3(w + 0.05f, 0.2f, 1f);
        hpGlow.transform.localPosition = new Vector3(left, 0f, 0f);
        hpShine.transform.localScale = new Vector3(w, 0.035f, 1f);
        hpShine.transform.localPosition = new Vector3(left, 0.055f, 0f);

        Color c = hpShown > 0.55f ? new Color(0.34f, 0.82f, 0.34f)
                : hpShown > 0.25f ? new Color(0.97f, 0.72f, 0.18f)
                                  : new Color(0.92f, 0.2f, 0.15f);
        hpFill.color = c;
        hpShine.color = new Color(Color.Lerp(c, Color.white, 0.6f).r,
            Color.Lerp(c, Color.white, 0.6f).g, Color.Lerp(c, Color.white, 0.6f).b, 0.42f);
        hpGlow.color = new Color(c.r, c.g, c.b, 0.28f);
    }

    void OnDestroy()
    {
        All.Remove(this);
        if (Health != null) Health.Changed -= OnHealthChanged;
    }

    void OnHealthChanged(float cur, float max)
    {
        if (body != null) body.color = Health.Ratio < 0.35f
            ? new Color(1f, 0.65f, 0.65f) : Color.white;
        hpTarget = Mathf.Clamp01(Health.Ratio);
    }

    void Update()
    {
        // 头顶血条平滑收缩/伸长，不受角色朝向翻转影响
        if (!isBoss && hpFill != null && !Mathf.Approximately(hpShown, hpTarget))
        {
            hpShown = Mathf.MoveTowards(hpShown, hpTarget, Time.deltaTime * 1.8f);
            ApplyHealthBar();
        }

        if (Dead || target == null || target.Health == null || target.Health.Dead) return;

        float dx = target.transform.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);
        float aggro = Data.kind == "ranged" ? 8f : 12f;
        if (dist > aggro) return;

        float dir = Mathf.Sign(dx);
        if (body != null && dir != 0f)
        {
            body.transform.localScale = new Vector3(
                Mathf.Abs(baseVisualScale.x) * dir,
                Mathf.Abs(baseVisualScale.y), 1f);
        }

        float attackRange = Data.kind == "ranged" ? 5.5f : 1.1f * scale;
        if (dist > attackRange)
        {
            float speed = Data.kind == "bomber" ? baseSpeed * 1.5f : baseSpeed;
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        }
        else if (Time.time >= nextAttack)
        {
            nextAttack = Time.time + (Data.kind == "boss" ? 1.2f : 1.6f);
            if (Data.kind == "bomber")
            {
                // 自爆：爆炸特效 + 范围伤害
                CombatFx.Flash(transform.position, new Color(1f, 0.55f, 0.2f), 2.2f);
                if (Vector2.Distance(transform.position, target.transform.position) <= 2f)
                    target.TakeHit(Data.damage * 1.6f, this);
                Die();
                return;
            }
            if (Data.kind == "ranged")
            {
                // 远程怪：发射可见箭矢，命中才扣血
                Vector3 from = transform.position + new Vector3(dir * 0.6f, 0.2f, 0f);
                Projectile.Spawn(from, new Vector2(dir, 0.05f), Data.damage,
                    Projectile.Side.Enemy, Projectile.Kind.EnemyArrow, 9f, target, this,
                    target.transform);
            }
            else
            {
                // 近战/Boss：挥砍特效 + 直接伤害
                CombatFx.Slash(transform.position, (int)dir, attackRange + 0.4f);
                target.TakeHit(Data.damage, this);
            }
            EventBus.RaiseMessage(target.transform.position.x.ToString("F0") + " 处受到攻击");
        }
    }

    public void ApplyDamage(float amount, PlayerController source)
    {
        if (Dead || Health == null || Health.Dead) return;
        Health.TakeDamage(amount);
        if (Health.Dead) Die();
    }

    void Die()
    {
        if (Dead) return;
        Dead = true;
        EventBus.RaiseEnemyDied(this);
        Destroy(gameObject, 0.05f);
    }
}
