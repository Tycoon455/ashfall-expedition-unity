using UnityEngine;

/// <summary>战斗特效：飞行投射物 + 命中闪光 + 近战挥砍。全部运行时生成，不依赖预制体。</summary>
public class Projectile : MonoBehaviour
{
    public enum Side { Player, Enemy }
    public enum Kind { Arrow, Magic, EnemyArrow }

    Side side;
    Kind kind;
    Vector2 dir;
    float speed;
    float damage;
    float life = 2.5f;
    float hitRadius = 0.6f;
    PlayerController player;
    EnemyController source;
    Transform target; // 锁定目标，飞行中轻微追踪保证命中（双方 Y 坐标不同）
    Vector3 origin;   // 发射点，用于限制最大飞行距离
    float maxRange;   // 0 表示不限制（怪物箭矢沿用旧行为）
    SpriteRenderer sr;
    float spin;
    Vector3 baseScale;

    public static Projectile Spawn(Vector3 from, Vector2 direction, float dmg, Side side,
        Kind kind, float speed = 11f, PlayerController p = null, EnemyController e = null,
        Transform lockTarget = null, float range = 0f)
    {
        GameObject go = new GameObject("Projectile_" + kind);
        go.transform.position = from;
        Projectile pr = go.AddComponent<Projectile>();
        pr.side = side;
        pr.kind = kind;
        pr.dir = direction.normalized;
        pr.damage = dmg;
        pr.speed = speed;
        pr.player = p;
        pr.source = e;
        pr.target = lockTarget;
        pr.origin = from;
        pr.maxRange = Mathf.Max(0f, range);
        pr.sr = go.AddComponent<SpriteRenderer>();
        pr.sr.sprite = ResourceFactory.WhiteSolid;
        pr.sr.sortingOrder = 30;

        if (kind == Kind.Arrow)
        {
            pr.sr.color = new Color(0.92f, 0.82f, 0.45f);
            pr.baseScale = new Vector3(0.55f, 0.1f, 1f);
            float ang = Mathf.Atan2(pr.dir.y, pr.dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
        else if (kind == Kind.EnemyArrow)
        {
            pr.sr.color = new Color(0.95f, 0.45f, 0.4f);
            pr.baseScale = new Vector3(0.45f, 0.09f, 1f);
            float ang = Mathf.Atan2(pr.dir.y, pr.dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
        else // Magic
        {
            pr.sr.color = new Color(0.5f, 0.85f, 1f);
            pr.baseScale = new Vector3(0.34f, 0.34f, 1f);
            pr.spin = 360f;
        }
        go.transform.localScale = pr.baseScale;
        return pr;
    }

    void Update()
    {
        // 锁定目标时朝目标当前位置修正方向，消除双方高度差导致的脱靶
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 to = (Vector2)target.position - (Vector2)transform.position;
            if (to.sqrMagnitude > 0.01f)
            {
                dir = Vector2.Lerp(dir, to.normalized, 6f * Time.deltaTime).normalized;
                if (spin == 0f) // 箭矢朝向飞行方向
                    transform.rotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        // 远程职业的投射物到达射程尽头即消散，不会继续飞到关卡另一端
        if (maxRange > 0f && Vector2.Distance(origin, transform.position) >= maxRange)
        {
            CombatFx.Flash(transform.position, sr.color, 0.35f);
            Destroy(gameObject);
            return;
        }

        Vector2 pos = transform.position;
        if (side == Side.Player)
        {
            for (int i = EnemyController.All.Count - 1; i >= 0; i--)
            {
                EnemyController e = EnemyController.All[i];
                if (e == null || e.Dead) continue;
                if (Vector2.Distance(pos, (Vector2)e.transform.position) <= hitRadius)
                {
                    e.ApplyDamage(damage, player);
                    CombatFx.Flash(pos, sr.color, 0.8f);
                    Debug.Log("[投射物命中] " + kind + " -> " + e.Data.id + " 伤害 " + damage.ToString("F1"));
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            if (player != null && player.Health != null && !player.Health.Dead &&
                Vector2.Distance(pos, (Vector2)player.transform.position) <= hitRadius)
            {
                player.TakeHit(damage, source);
                CombatFx.Flash(pos, sr.color, 0.8f);
                Destroy(gameObject);
                return;
            }
        }
    }
}

public static class CombatFx
{
    /// <summary>命中/爆炸闪光：瞬间出现、快速放大淡出。</summary>
    public static void Flash(Vector3 at, Color color, float size = 1f)
    {
        GameObject go = new GameObject("FxFlash");
        go.transform.position = at;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResourceFactory.WhiteSolid;
        sr.color = color;
        sr.sortingOrder = 35;
        go.transform.localScale = Vector3.one * size * 0.4f;
        FxAnim anim = go.AddComponent<FxAnim>();
        anim.Init(size, 0.28f, true);
    }

    /// <summary>近战挥砍：在角色前方短暂显示一道斜向光刃。</summary>
    public static void Slash(Vector3 at, int facing, float reach = 1.4f)
    {
        GameObject go = new GameObject("FxSlash");
        go.transform.position = at + new Vector3(facing * reach * 0.5f, 0.2f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResourceFactory.WhiteSolid;
        sr.color = new Color(1f, 0.95f, 0.75f, 0.9f);
        sr.sortingOrder = 34;
        go.transform.rotation = Quaternion.Euler(0f, 0f, 35f * facing);
        FxAnim anim = go.AddComponent<FxAnim>();
        anim.Init(1f, 0.18f, false, new Vector3(0.12f, reach, 1f));
    }
}

/// <summary>特效动画：放大并淡出后自毁。</summary>
public class FxAnim : MonoBehaviour
{
    SpriteRenderer sr;
    float size, dur, age;
    bool expand;
    Vector3 shape;

    public void Init(float targetSize, float duration, bool expandFlag, Vector3? finalShape = null)
    {
        size = targetSize;
        dur = Mathf.Max(0.01f, duration);
        expand = expandFlag;
        shape = finalShape ?? Vector3.one;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / dur);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = (1f - t) * 0.95f;
            sr.color = c;
        }
        Vector3 s = expand ? Vector3.one * size * (0.4f + t * 0.9f)
                           : shape * size * (0.6f + t * 0.5f);
        transform.localScale = s;
        if (t >= 1f) Destroy(gameObject);
    }
}
