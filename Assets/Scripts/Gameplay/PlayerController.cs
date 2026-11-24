using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public HealthComponent Health;
    public StaminaComponent Stamina;
    public CombatComponent Combat;
    public InventoryComponent Inventory;
    public SetBonusComponent Bonus;

    public bool InputEnabled;
    public float Facing { get { return facing; } }

    float facing = 1f;
    float verticalVelocity;
    float nextAttack, nextDodge, nextSkill;
    float dodgeUntil;
    float dodgeDir = 1f;
    float levelWidth = 44f;
    float baseSpeed = 4f;
    SpriteRenderer body;
    GameObject visual;
    Vector3 baseVisualScale = Vector3.one * 0.55f; // 精灵基准尺寸，朝向只改符号不累乘
    float punchScale = 1f; // 攻击瞬间的临时放大系数

    public void Setup(string professionId, InventoryComponent inv, SetBonusComponent bonusComp, float startX)
    {
        GameActorTag existingTag = GetComponent<GameActorTag>();
        if (existingTag == null) existingTag = gameObject.AddComponent<GameActorTag>();
        existingTag.professionId = professionId;

        ProfessionData prof = GameData.Profession(professionId);
        if (prof == null) prof = GameData.Profession("warrior");

        Health = GetComponent<HealthComponent>();
        if (Health == null) Health = gameObject.AddComponent<HealthComponent>();
        Health.IsPlayer = true;
        Health.Defense = prof.defense + (inv == null ? 0 : inv.BonusDefense());
        Health.Reset(prof.max_hp + (inv == null ? 0 : inv.BonusHp()));

        Stamina = GetComponent<StaminaComponent>();
        if (Stamina == null) Stamina = gameObject.AddComponent<StaminaComponent>();
        Stamina.BaseMax = prof.max_stamina;
        Stamina.BaseRegen = 15f;
        Stamina.Configure(Health);

        Combat = GetComponent<CombatComponent>();
        if (Combat == null) Combat = gameObject.AddComponent<CombatComponent>();
        Combat.BaseDamage = prof.attack;
        float skillCd = prof.skill == "旋风斩" ? 4f :
                        prof.skill == "三连射" ? 3.2f : 5f;
        Combat.SetProfessionProfile(professionId, skillCd);

        Inventory = inv;
        Bonus = bonusComp;
        Combat.ApplySetBonus(Bonus, inv);
        Stamina.BonusMax = inv == null ? 0f : inv.BonusStamina() +
            (Bonus != null && Bonus.Has("max_stamina")
                ? prof.max_stamina * (Bonus.Mult("max_stamina") - 1f) : 0f);
        Stamina.RegenMult = Bonus == null ? 1f : 1f + Bonus.Add("stamina_regen");
        Stamina.Recalculate();

        baseSpeed = prof.speed * GameConfig.WorldScale;
        Health.DeathSaves = Bonus != null && Bonus.Has("deathsave") ? 1 : 0;

        // 重复 Setup（换职业/读档）时先销毁旧模型，避免多个角色精灵重叠
        Transform oldVisual = transform.Find("Visual");
        if (oldVisual != null) DestroyImmediate(oldVisual.gameObject);
        visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        body = visual.AddComponent<SpriteRenderer>();
        body.sprite = ResourceFactory.Sprite("Characters/" + professionId, 220f);
        body.sortingOrder = 10;
        if (body.sprite == null)
        {
            body.sprite = ResourceFactory.SolidSprite(new Color(0.7f, 0.5f, 0.3f));
            baseVisualScale = new Vector3(0.8f, 1.6f, 1f);
        }
        else
        {
            baseVisualScale = Vector3.one * 0.55f;
        }
        visual.transform.localScale = baseVisualScale;
        int visualCount = 0;
        foreach (Transform c in transform) if (c.name == "Visual") visualCount++;
        Debug.Log("[Setup] 职业=" + professionId + " Visual数量=" + visualCount);

        transform.position = new Vector3(startX, GameConfig.GroundY + 1.1f, 0f);
        InputEnabled = true;
        Health.Changed -= OnHealthChanged; // 先解绑，避免重复 Setup 累积订阅
        Health.Changed += OnHealthChanged;
    }

    void OnDestroy()
    {
        if (Health != null) Health.Changed -= OnHealthChanged;
    }

    void OnHealthChanged(float cur, float max)
    {
        if (body == null) return;
        body.color = Health.Ratio <= GameConfig.DesperationHpThreshold
            ? new Color(1f, 0.62f, 0.62f) : Color.white;
    }

    public void SetLevelWidth(float width)
    {
        levelWidth = width;
    }

    public void RefreshGear()
    {
        Health.Defense = (GameData.Profession(ProfessionId()).defense) +
                         (Inventory == null ? 0 : Inventory.BonusDefense());
        Combat.ApplySetBonus(Bonus, Inventory);
        Stamina.Recalculate();
        Health.DeathSaves = Bonus != null && Bonus.Has("deathsave") ? 1 : 0;
    }

    string ProfessionId()
    {
        return GetComponent<GameActorTag>() == null ? "warrior" : GetComponent<GameActorTag>().professionId;
    }

    /// <summary>只在职业有效射程内、面向方向上锁定最近敌人，避免远程职业全图索敌。</summary>
    Transform FindTarget(float maxRange)
    {
        EnemyController best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < EnemyController.All.Count; i++)
        {
            EnemyController e = EnemyController.All[i];
            if (e == null || e.Dead) continue;
            float dx = e.transform.position.x - transform.position.x;
            if (Mathf.Sign(dx) != facing && Mathf.Abs(dx) > 0.5f) continue;
            float d = Mathf.Abs(dx);
            if (d > maxRange) continue;
            if (d < bestD) { bestD = d; best = e; }
        }
        return best == null ? null : best.transform;
    }

    void Update()
    {
        if (!InputEnabled || Health == null || Health.Dead) return;

        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        // 闪避期间锁定冲刺方向：单按 K 也会朝当前朝向冲出去，不再原地空闪
        bool dodging = Time.time < dodgeUntil;
        if (dodging) h = dodgeDir;
        if (Mathf.Abs(h) > 0.01f) facing = Mathf.Sign(h);

        float speed = baseSpeed * Combat.MoveSpeedMult;
        if (dodging) speed *= GameConfig.DodgeSpeedMult + Combat.DodgeSpeedBonus;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + h * speed * Time.deltaTime, 0f, levelWidth);

        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(p.y - (GameConfig.GroundY + 1.1f)) < 0.02f)
            verticalVelocity = 7.5f;
        verticalVelocity -= 22f * Time.deltaTime;
        p.y += verticalVelocity * Time.deltaTime;
        float floor = GameConfig.GroundY + 1.1f;
        if (p.y < floor) { p.y = floor; verticalVelocity = 0f; }
        transform.position = p;

        if (visual != null && facing != 0f)
            visual.transform.localScale = new Vector3(
                Mathf.Abs(baseVisualScale.x) * facing * punchScale,
                Mathf.Abs(baseVisualScale.y) * punchScale, 1f);

        if ((Input.GetKey(KeyCode.J) || Input.GetMouseButton(0)) && Time.time >= nextAttack)
            DoAttack();
        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextDodge)
            DoDodge();
        if (Input.GetKeyDown(KeyCode.L) && Time.time >= nextSkill)
            DoSkill();
    }

    void DoAttack()
    {
        if (!Stamina.TrySpend(GameConfig.StaminaAttack)) return;
        Stamina.MarkCombat();
        nextAttack = Time.time + Combat.AttackInterval;
        float dmg = Combat.RollDamage(Health);
        string prof = ProfessionId();

        if (prof == "ranger" || prof == "mage")
        {
            // 远程职业：只在有效射程内索敌，投射物到达最大距离后自动消散；硬上限防止套装把射程叠到全图
            float range = Mathf.Min(Combat.AttackRange, prof == "ranger" ? 12f : 9f);
            Vector3 from = transform.position + new Vector3(facing * 0.7f, 0.35f, 0f);
            Vector2 aim = new Vector2(facing, 0.08f);
            Transform target = FindTarget(range);
            if (prof == "ranger")
                Projectile.Spawn(from, aim, dmg, Projectile.Side.Player, Projectile.Kind.Arrow,
                    13f, this, null, target, range);
            else
                Projectile.Spawn(from, aim, dmg, Projectile.Side.Player, Projectile.Kind.Magic,
                    9f, this, null, target, range);
            StartCoroutine(Punch());
            return;
        }

        // 战士近战：范围判定 + 挥砍特效
        // 基础 1.5f 太近，战士近战单独放大到 1.7 倍（≈2.55），套装 range 加成仍叠加其上；
        // 只改战士，不影响游侠/法师技能半径与怪物攻击距离。
        float meleeRange = Combat.AttackRange * 1.7f;
        int hits = 0;
        int maxHits = 1 + Mathf.Max(0, Combat.ExtraTargets);
        for (int i = EnemyController.All.Count - 1; i >= 0; i--)
        {
            EnemyController e = EnemyController.All[i];
            if (e == null || e.Dead) continue;
            float dx = e.transform.position.x - transform.position.x;
            if (Mathf.Sign(dx) != facing && Mathf.Abs(dx) > 0.4f) continue;
            if (Mathf.Abs(dx) > meleeRange) continue;
            e.ApplyDamage(dmg, this);
            hits++;
            if (hits >= maxHits) break;
        }
        if (hits > 0) EventBus.RaiseMessage("命中 " + hits + " 个敌人");
        CombatFx.Slash(transform.position, (int)facing, meleeRange);
        StartCoroutine(Punch());
    }

    void DoDodge()
    {
        if (!Stamina.TrySpend(GameConfig.StaminaDodge * Combat.SkillCostMult)) return;
        Stamina.MarkCombat();
        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        // 有方向输入就朝输入方向冲，没有就朝当前朝向冲（避免贴着怪物按 K 不动）
        dodgeDir = Mathf.Abs(h) > 0.01f ? Mathf.Sign(h) : (facing == 0f ? 1f : facing);
        float duration = GameConfig.DodgeDuration * Mathf.Max(0.1f, Combat.DodgeDistanceMult);
        nextDodge = Time.time + 0.75f;
        dodgeUntil = Time.time + duration;
        Combat.GrantIFrame(Combat.IFrameForDodge(Health, duration));
        EventBus.RaiseMessage("闪避（无敌冲刺 · 方向 " + (dodgeDir > 0 ? "右" : "左") + "）");
    }

    void DoSkill()
    {
        float cost = GameConfig.StaminaSkill * Combat.SkillCostMult;
        if (!Stamina.TrySpend(cost)) return;
        Stamina.MarkCombat();
        nextSkill = Time.time + Combat.SkillCooldown;
        float dmg = Combat.RollDamage(Health) * 2.1f;
        string prof = ProfessionId();
        float range = Mathf.Min(Combat.AttackRange, prof == "ranger" ? 12f : 9f);
        // 法师冰环、游侠三连射都受职业射程约束，不再覆盖整张关卡
        float radius = prof == "warrior" ? Combat.AttackRange * 1.8f : range;

        int hits = 0;

        if (prof == "ranger")
        {
            // 三连射：三支箭依次飞行，各自命中结算，同样受射程限制
            Transform target = FindTarget(range);
            for (int k = 0; k < 3; k++)
            {
                Vector3 from = transform.position + new Vector3(facing * 0.7f, 0.35f + k * 0.02f, 0f);
                Projectile.Spawn(from, new Vector2(facing, 0.06f * (k - 1)), dmg * 0.7f,
                    Projectile.Side.Player, Projectile.Kind.Arrow, 14f, this, null, target, range);
            }
            EventBus.RaiseMessage("三连射！");
            StartCoroutine(Punch());
            return;
        }

        for (int i = EnemyController.All.Count - 1; i >= 0; i--)
        {
            EnemyController e = EnemyController.All[i];
            if (e == null || e.Dead) continue;
            float dx = e.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > radius) continue;
            e.ApplyDamage(dmg, this);
            CombatFx.Flash(e.transform.position, prof == "mage"
                ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.8f, 0.4f), 1.1f);
            hits++;
        }
        EventBus.RaiseMessage((prof == "mage" ? "冰环" : "旋风斩") + "命中 " + hits + " 个敌人");
        CombatFx.Flash(transform.position, prof == "mage"
            ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.85f, 0.5f), radius * 0.8f);
        StartCoroutine(Punch());
    }

    IEnumerator Punch()
    {
        if (visual == null) yield break;
        punchScale = 1.12f;
        yield return new WaitForSeconds(0.09f);
        punchScale = 1f;
    }

    public void TakeHit(float amount, EnemyController source)
    {
        if (Health == null || Health.Dead || Combat.IsInvulnerable) return;
        Health.TakeDamage(amount);
        Stamina.MarkCombat();
        if (Combat.Reflect > 0f && source != null && !source.Dead)
            source.ApplyDamage(amount * Combat.Reflect, null);
    }

    public void FullHeal()
    {
        Health.Reset(Health.MaxHp);
        Stamina.Refill();
    }
}

/// <summary>记录职业信息，供动态创建的角色读取。</summary>
public class GameActorTag : MonoBehaviour
{
    public string professionId = "warrior";
}
