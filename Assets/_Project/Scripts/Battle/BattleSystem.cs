using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Runs a battle of one to three fighters a side. On the left the player, who plays cards, and up
// to two allies (companions travelling with the player); on the right one to three enemies. Once
// the player ends their turn, each ally acts, then each enemy, left to right. Won when every enemy
// is down, lost when the player is; a fallen ally just leaves the fight.
public class BattleSystem : MonoBehaviour
{
    public BattleState CurrentBattleState { get; private set; } = BattleState.Running;

    public event Action<DeckManager> HandChanged;

    [Header("Characters")]
    [Tooltip("The first enemy. Copies of it make the other enemies and the allies.")]
    [SerializeField] private EnemyCharacter enemy;
    [SerializeField] private PlayerCharacter player;

    [Header("UI")]
    [SerializeField] private DeckUI deckUI;
    [SerializeField] private BattleEndUI battleEndUI;
    [SerializeField] private BattleTurnUI battleTurnUI;
    [SerializeField] private HandUI handUI;
    [Tooltip("The first enemy's intent. Copies of it show the other fighters'.")]
    [SerializeField] private EnemyIntentView enemyIntentView;

    [Header("Quests")]
    [SerializeField] private List<QuestData> questCatalog = new();

    [Header("Turn Pacing")]
    [SerializeField] private float enemyIntentTelegraphDuration = 0.5f;
    [SerializeField] private float enemyIntentHideDuration = 0.15f;
    [SerializeField] private float enemyActionRecoveryDelay = 0.75f;
    [Tooltip("Pause between two abilities of the same turn, and between two fighters' turns.")]
    [SerializeField] private float enemyActionGapDelay = 0.3f;
    [Tooltip("How long a fallen fighter takes to fade away.")]
    [SerializeField] private float defeatFadeDuration = 0.4f;
    [Tooltip("How long the fighters still standing take to regroup after one falls.")]
    [SerializeField] private float regroupDuration = 0.5f;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;
    private BattleEncounterResolver encounter;
    private BattleSystemPlayerActions playerActions;
    private Canvas spriteSortedHpCanvas;
    private readonly HashSet<Character> fallen = new();
    private readonly HashSet<EnemyCharacter> actedThisRound = new();
    private readonly List<Action> unsubscribers = new();

    // The one-on-one layout, as the scene has it: kept to arrange the fighters again after one falls.
    private readonly Dictionary<Character, (Vector3 scale, int sortingOrder)> sceneLooks = new();
    private Vector3 playerScenePosition;
    private float enemySceneX;
    private float groundFeet;

    private bool aiTurnPlaying;
    private bool regroupRequested;
    private Coroutine regroupRoutine;

    // When regrouping, a side moves only if someone on it would move or resize at least this much
    // (world units; share of their size).
    private const float MinRegroupMove = 1.5f;
    private const float MinRegroupResize = 0.06f;

    private bool turnTransitionInProgress;
    private bool skipTurnTransitionRequested;
    private bool victoryHandled;

    public bool IsTurnTransitionInProgress => turnTransitionInProgress;
    public IReadOnlyList<EnemyCharacter> Enemies => encounter != null
        ? encounter.Enemies
        : Array.Empty<EnemyCharacter>();

    private bool IsDecided => !player.IsAlive || turnManager.AllEnemiesDefeated;

    private void Start()
    {
        QuestManager.RegisterQuests(questCatalog);

        if (player == null)
        {
            DevLog.Log("Player not assigned");
            return;
        }

        if (enemy == null)
        {
            DevLog.Log("Enemy not assigned");
            return;
        }

        encounter = new BattleEncounterResolver();
        encounter.Resolve(enemy, FindHpBar(enemy), enemyIntentView);

        deckManager = new DeckManager();
        deckManager.Initialize(player.DeckData);

        deckUI.Bind(deckManager.Deck);

        turnManager = new TurnManager(player, encounter.Allies, encounter.Enemies, deckManager);
        cardPlayer = new CardPlayer(player, deckManager, turnManager);
        playerActions = new BattleSystemPlayerActions(
            () => deckManager,
            () => cardPlayer,
            () => turnManager,
            CanPlay,
            AfterAction,
            NotifyHandChanged,
            RequestEndPlayerTurn);

        ArrangeFighters();
        foreach (EnemyCharacter fighter in turnManager.GetAiFighters())
            WatchFighter(fighter);

        turnManager.StartBattle();
        foreach (KeyValuePair<EnemyCharacter, EnemyIntentView> intent in encounter.IntentViews)
            intent.Value.BindEnemy(intent.Key, deferInitialRevealUntilHandFlyFinishes: true);

        StartCoroutine(StartBattleRoutine());
    }

    private IEnumerator StartBattleRoutine()
    {
        BeginTurnTransition();

        yield return null;
        while (FadeManager.Instance != null && FadeManager.Instance.IsFading)
            yield return null;

        if (battleTurnUI != null)
            yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Player, ShouldSkipTurnTransition);

        RevealIntents();

        turnManager.DrawStartingHand();
        NotifyHandChanged();

        EndTurnTransition();
    }

    public void RequestSkipTurnTransition()
    {
        if (!turnTransitionInProgress || CurrentBattleState != BattleState.Running)
            return;

        skipTurnTransitionRequested = true;
        battleTurnUI?.RequestSkip();
        foreach (EnemyIntentView intentView in encounter.IntentViews.Values)
            intentView.SkipToHidden();
    }

    private void BeginTurnTransition()
    {
        skipTurnTransitionRequested = false;
        turnTransitionInProgress = true;
    }

    private void EndTurnTransition()
    {
        skipTurnTransitionRequested = false;
        turnTransitionInProgress = false;
    }

    private bool ShouldSkipTurnTransition()
    {
        return skipTurnTransitionRequested;
    }

    private IEnumerator WaitSkippable(float seconds)
    {
        if (seconds <= 0f || ShouldSkipTurnTransition())
            yield break;

        float elapsed = 0f;
        while (elapsed < seconds && !ShouldSkipTurnTransition())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void RequestEndPlayerTurn()
    {
        if (turnTransitionInProgress)
            return;

        StartCoroutine(EndPlayerTurnRoutine());
    }

    private IEnumerator EndPlayerTurnRoutine()
    {
        if (turnManager == null || !turnManager.TryBeginEnemyTurn())
            yield break;

        BeginTurnTransition();
        actedThisRound.Clear();

        // The player's side first, if any ally is still standing.
        if (HasFighterToAct(turnManager.Allies))
        {
            if (battleTurnUI != null)
                yield return battleTurnUI.PlayAllyTurnAnnouncement(ShouldSkipTurnTransition);

            yield return PlayFighterTurns(turnManager.Allies);
        }

        if (!IsDecided)
        {
            if (battleTurnUI != null)
                yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Enemy, ShouldSkipTurnTransition);

            yield return PlayFighterTurns(turnManager.Enemies);
        }

        yield return WaitSkippable(enemyActionRecoveryDelay);

        AfterAction();

        if (CurrentBattleState != BattleState.Running)
        {
            EndTurnTransition();
            yield break;
        }

        if (battleTurnUI != null && !ShouldSkipTurnTransition())
            yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Player, ShouldSkipTurnTransition);
        else
            battleTurnUI?.ForceHide();

        turnManager.StartNextPlayerTurn();
        turnManager.PlanNextAiActions();
        RevealIntents();
        NotifyHandChanged();

        EndTurnTransition();
    }

    private IEnumerator PlayFighterTurns(IReadOnlyList<EnemyCharacter> side)
    {
        bool first = true;
        foreach (EnemyCharacter fighter in new List<EnemyCharacter>(side))
        {
            if (IsDecided)
                yield break;

            if (!CanAct(fighter))
                continue;

            if (!first)
                yield return WaitSkippable(enemyActionGapDelay);

            first = false;
            yield return PlayFighterTurn(fighter);
        }
    }

    private static bool HasFighterToAct(IReadOnlyList<EnemyCharacter> side)
    {
        foreach (EnemyCharacter fighter in side)
        {
            if (CanAct(fighter))
                return true;
        }

        return false;
    }

    private static bool CanAct(EnemyCharacter fighter)
    {
        return fighter != null && fighter.IsAlive && fighter.Brain != null;
    }

    private IEnumerator PlayFighterTurn(EnemyCharacter fighter)
    {
        // Nobody attacks while the others regroup: the attack puts its fighter back where it started.
        while (regroupRoutine != null)
            yield return null;

        aiTurnPlaying = true;
        yield return PlayFighterTurnActions(fighter);
        aiTurnPlaying = false;
    }

    private IEnumerator PlayFighterTurnActions(EnemyCharacter fighter)
    {
        actedThisRound.Add(fighter);

        encounter.IntentViews.TryGetValue(fighter, out EnemyIntentView intentView);
        if (intentView != null && !ShouldSkipTurnTransition())
        {
            yield return intentView.ShowCurrentIntentDuringEnemyTurn(
                enemyIntentTelegraphDuration,
                ShouldSkipTurnTransition);
            yield return intentView.HideIntent(enemyIntentHideDuration, ShouldSkipTurnTransition);
        }
        else
        {
            intentView?.SkipToHidden();
        }

        turnManager.BeginAiTurn(fighter);

        // Read after the turn has started: a stun may have just cut the plan short.
        var abilities = new List<EnemyAbilityData>(fighter.Brain.CurrentPlan.Abilities);
        fighter.TryGetComponent(out EnemyAttackLungeAnimation animation);

        for (int i = 0; i < abilities.Count; i++)
        {
            if (!fighter.IsAlive || IsDecided)
                break;

            if (i > 0)
                yield return WaitSkippable(enemyActionGapDelay);

            EnemyAbilityData ability = abilities[i];
            Character target = fighter.Brain.GetPrimaryTarget(ability);
            bool executed = false;

            void ExecuteOnce()
            {
                if (executed)
                    return;

                executed = true;
                turnManager.ExecuteAiAbility(fighter, ability);
            }

            if (animation != null)
                yield return animation.Play(fighter, target, ExecuteOnce, ShouldSkipTurnTransition);
            else
                ExecuteOnce();

            if (!executed)
                ExecuteOnce();
        }

        turnManager.EndAiTurn(fighter);
    }

    public void SetVictory()
    {
        CurrentBattleState = BattleState.Victory;
    }

    public void SetDefeat()
    {
        CurrentBattleState = BattleState.Defeat;
    }

    public bool CanPlay()
    {
        return CurrentBattleState == BattleState.Running
               && !turnTransitionInProgress
               && (handUI == null || !handUI.IsAnimatingCards);
    }

    public void AfterAction()
    {
        if (turnManager != null && turnManager.AllEnemiesDefeated)
        {
            HandleVictory();
            return;
        }

        if (player.CurrentHealth <= 0)
        {
            SetDefeat();
            handUI?.ReleaseAllBattleCardDrags();
            battleEndUI?.ShowDefeat();
        }
    }

    private void HandleVictory()
    {
        if (victoryHandled)
            return;

        victoryHandled = true;
        SetVictory();

        List<CardData> droppedLoot = RollAndCollectEnemyLoot();

        foreach (EnemyCharacter defeated in Enemies)
        {
            string defeatFlag = defeated != null && defeated.Data != null ? defeated.Data.storyFlagOnDefeat : null;
            if (!string.IsNullOrWhiteSpace(defeatFlag))
                StoryFlagManager.SetFlag(defeatFlag);
        }

        ExplorationPlayerSession.SaveHealth(player.CurrentHealth);
        DefeatedEncounters.MarkDefeated(PendingBattleEnemy.EncounterId);
        QuestManager.HandleEncounterDefeated(PendingBattleEnemy.EncounterId, player.InventoryData);

        handUI?.ReleaseAllBattleCardDrags();
        battleEndUI?.ShowVictory(droppedLoot);
    }

    private List<CardData> RollAndCollectEnemyLoot()
    {
        var droppedLoot = new List<CardData>();
        foreach (EnemyCharacter defeated in Enemies)
        {
            if (defeated != null)
                droppedLoot.AddRange(EnemyLootRoller.Roll(defeated.Data));
        }

        if (player?.InventoryData == null)
            return droppedLoot;

        for (int i = 0; i < droppedLoot.Count; i++)
            player.InventoryData.AddCard(droppedLoot[i]);

        return droppedLoot;
    }

    public bool TryPlayCardFromHand(int index, ICombatant target)
    {
        return playerActions != null && playerActions.TryPlayCardFromHand(index, target);
    }

    public void EndTurn()
    {
        if (turnTransitionInProgress)
        {
            RequestSkipTurnTransition();
            return;
        }

        playerActions?.EndTurn();
    }

    // Unless the battle is one on one, each side stands in file along a diagonal (the player in
    // front of the allies, the enemy from the map in front of the others); everyone faces the
    // other side.
    private void ArrangeFighters()
    {
        var playerSide = new List<Character> { player };
        foreach (EnemyCharacter ally in encounter.Allies)
        {
            // Copies of the first enemy: they start out where it stands.
            ally.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, ally.transform.position.z);
            playerSide.Add(ally);
        }

        HealthBarUI playerHpBar = FindHpBar(player);
        if (playerHpBar != null)
            encounter.HpBars[player] = playerHpBar;

        // The one-on-one layout, before anyone moves.
        groundFeet = BattleFormation.FeetOf(player);
        playerScenePosition = player.transform.position;
        enemySceneX = encounter.Enemies[0].transform.position.x;
        foreach (Character fighter in playerSide)
            RememberSceneLook(fighter);
        foreach (EnemyCharacter foe in encounter.Enemies)
            RememberSceneLook(foe);

        if (playerSide.Count > 1 || encounter.Enemies.Count > 1)
        {
            PlaceInFormation(playerSide, encounter.Enemies);

            // It would pull the front enemy back up to the player's HP bar; the file places the
            // enemies itself. A lone enemy is still lined up by it, on the player's ground line.
            if (encounter.Enemies.Count > 1 && TryGetComponent(out BattleHpBarBaselineAligner aligner))
                aligner.enabled = false;
        }

        Transform enemySide = encounter.Enemies[0].transform;
        BattleFacing.FaceTowards(player, player.Data != null ? player.Data.profile : null, enemySide);
        foreach (EnemyCharacter ally in encounter.Allies)
            BattleFacing.FaceTowards(ally, ally.Data != null ? ally.Data.profile : null, enemySide);
        foreach (EnemyCharacter foe in encounter.Enemies)
            BattleFacing.FaceTowards(foe, foe.Data != null ? foe.Data.profile : null, player.transform);
    }

    // Places the fighters still standing as their numbers call for, at once: one on one as the
    // scene has them, otherwise each side in file (BattleFormation), everyone as big as their
    // number calls for.
    private void PlaceInFormation(IReadOnlyList<Character> playerSide, IReadOnlyList<EnemyCharacter> enemies)
    {
        Camera camera = Camera.main;
        if (camera == null || playerSide.Count == 0 || enemies.Count == 0)
            return;

        foreach (Character fighter in playerSide)
            RestoreSceneLook(fighter);
        foreach (EnemyCharacter foe in enemies)
            RestoreSceneLook(foe);

        if (playerSide.Count == 1 && enemies.Count == 1)
        {
            playerSide[0].transform.position = playerScenePosition;
            BattleFormation.StandAt(enemies[0], enemySceneX, groundFeet);
        }
        else
        {
            PlaceInFile(playerSide, enemies, camera);
        }

        DrawHpBarsAmongSprites(playerSide, camera);
        DrawHpBarsAmongSprites(enemies, camera);
    }

    private void PlaceInFile(IReadOnlyList<Character> playerSide, IReadOnlyList<EnemyCharacter> enemies, Camera camera)
    {
        int fighterCount = playerSide.Count + enemies.Count;
        float playerSideScale = BattleFormation.CrowdScale(fighterCount, BattleFormation.PlayerFile);
        float enemySideScale = BattleFormation.CrowdScale(fighterCount, BattleFormation.EnemyFile);
        foreach (Character fighter in playerSide)
            BattleFormation.ScaleKeepingFeet(fighter, playerSideScale);
        foreach (EnemyCharacter foe in enemies)
            BattleFormation.ScaleKeepingFeet(foe, enemySideScale);

        float HpBarWidth(Character fighter) => GetHpBarWorldWidth(fighter, camera);

        float enemySideEdge = GetEnemySideOuterEdge(camera);
        float playerSideInnerEdge = camera.transform.position.x - BattleFormation.MiddleGap / 2f;

        if (enemies.Count == playerSide.Count)
        {
            // As many on each side: the player's side as far from its edge as the deck pile is
            // from the other, the enemies mirroring it.
            float playerSideMirrorEdge = BattleFormation.ScreenEdge(
                camera, -1f, BattleFormation.MarginOf(camera, enemySideEdge, 1f));
            BattleFormation.ArrangeInFile(
                playerSide, BattleFormation.PlayerFile, -1f,
                playerSideMirrorEdge, playerSideInnerEdge, groundFeet, camera, HpBarWidth);
            BattleFormation.ArrangeMirrored(
                playerSide, enemies, BattleFormation.EnemyFile.RankScale,
                enemySideEdge, camera, HpBarWidth);
            return;
        }

        // First each side against its edge (the enemies' clear of the deck pile), so the enemies
        // get all the room there is and only shrink if they must...
        float playerSideEdge = BattleFormation.ScreenEdge(camera, -1f);
        float playerSideInner = BattleFormation.ArrangeInFile(
            playerSide, BattleFormation.PlayerFile, -1f,
            playerSideEdge, playerSideInnerEdge, groundFeet, camera, HpBarWidth);

        float enemySideInner = BattleFormation.ArrangeInFile(
            enemies, BattleFormation.EnemyFile, 1f,
            enemySideEdge, playerSideInner + BattleFormation.MiddleGap, groundFeet, camera, HpBarWidth);

        // ...then both sides move in, about to the middle of the screen.
        BattleFormation.SpreadSides(
            playerSide, playerSideEdge, playerSideInner,
            enemies, enemySideEdge, enemySideInner,
            BattleFormation.MarginOf(camera, enemySideEdge, 1f), camera);
    }

    // Size and drawing order before the formation changed them.
    private void RememberSceneLook(Character fighter)
    {
        int sortingOrder = fighter.TryGetComponent(out SpriteRenderer sprite) ? sprite.sortingOrder : 0;
        sceneLooks[fighter] = (fighter.transform.localScale, sortingOrder);
    }

    private void RestoreSceneLook(Character fighter)
    {
        if (!sceneLooks.TryGetValue(fighter, out (Vector3 scale, int sortingOrder) look))
            return;

        fighter.transform.localScale = look.scale;
        if (fighter.TryGetComponent(out SpriteRenderer sprite))
            sprite.sortingOrder = look.sortingOrder;
    }

    // Asked for once a fighter has fallen: the ones still standing regroup, as their new numbers
    // call for (two against three becomes two against two...).
    private void RequestRegroup()
    {
        regroupRequested = true;
        regroupRoutine ??= StartCoroutine(RegroupWhenIdle());
    }

    // Waits for the fighter acting to finish (an attack puts its fighter back where it started).
    private IEnumerator RegroupWhenIdle()
    {
        while (regroupRequested)
        {
            regroupRequested = false;
            while (aiTurnPlaying)
                yield return null;

            if (CurrentBattleState != BattleState.Running || IsDecided)
                break;

            yield return MoveIntoFormation();
        }

        regroupRoutine = null;
    }

    // The formation is worked out by placing everyone at once, then everyone walks there from
    // where they stood.
    private IEnumerator MoveIntoFormation()
    {
        var playerSide = new List<Character> { player };
        foreach (EnemyCharacter ally in encounter.Allies)
        {
            if (!fallen.Contains(ally))
                playerSide.Add(ally);
        }

        var enemies = new List<EnemyCharacter>();
        foreach (EnemyCharacter foe in encounter.Enemies)
        {
            if (!fallen.Contains(foe))
                enemies.Add(foe);
        }

        var standing = new List<Transform>();
        foreach (Character fighter in playerSide)
            standing.Add(fighter.transform);
        foreach (EnemyCharacter foe in enemies)
            standing.Add(foe.transform);

        var fromPositions = new Vector3[standing.Count];
        var fromScales = new Vector3[standing.Count];
        for (int i = 0; i < standing.Count; i++)
        {
            fromPositions[i] = standing[i].position;
            fromScales[i] = standing[i].localScale;
        }

        PlaceInFormation(playerSide, enemies);

        var toPositions = new Vector3[standing.Count];
        var toScales = new Vector3[standing.Count];
        for (int i = 0; i < standing.Count; i++)
        {
            toPositions[i] = standing[i].position;
            toScales[i] = standing[i].localScale;
        }

        // A side that would hardly move stays as it is (e.g. the player's from two against two to
        // two against one): a shuffle of a few pixels looks like a glitch.
        KeepIfHardlyMoving(0, playerSide.Count);
        KeepIfHardlyMoving(playerSide.Count, standing.Count);

        void KeepIfHardlyMoving(int first, int end)
        {
            for (int i = first; i < end; i++)
            {
                float resize = Mathf.Abs(toScales[i].y - fromScales[i].y) / Mathf.Max(0.0001f, Mathf.Abs(fromScales[i].y));
                if (Vector3.Distance(fromPositions[i], toPositions[i]) >= MinRegroupMove || resize >= MinRegroupResize)
                    return;
            }

            for (int i = first; i < end; i++)
            {
                toPositions[i] = fromPositions[i];
                toScales[i] = fromScales[i];
            }
        }

        for (float elapsed = 0f; elapsed < regroupDuration && !ShouldSkipTurnTransition(); elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / regroupDuration);
            for (int i = 0; i < standing.Count; i++)
            {
                standing[i].position = Vector3.Lerp(fromPositions[i], toPositions[i], t);
                standing[i].localScale = Vector3.Lerp(fromScales[i], toScales[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < standing.Count; i++)
        {
            standing[i].position = toPositions[i];
            standing[i].localScale = toScales[i];
        }
    }

    // In a side standing in file, the HP bars stay under their fighters, but the ones standing in
    // front hide the bars of the ones behind. The battle UI canvas is drawn over every sprite, so
    // the bars of the ones behind move to a canvas drawn among them. The front one's bar, which
    // nobody stands in front of, stays on (or goes back to) the UI canvas, over the dark band
    // behind the cards.
    private void DrawHpBarsAmongSprites(IReadOnlyList<Character> side, Camera camera)
    {
        for (int i = 0; i < side.Count; i++)
        {
            Character fighter = side[i];
            if (!encounter.HpBars.TryGetValue(fighter, out HealthBarUI hpBar) || !fighter.TryGetComponent(out SpriteRenderer sprite))
                continue;

            if (i == 0)
            {
                hpBar.DrawOnUi();
                continue;
            }

            if (spriteSortedHpCanvas == null)
                spriteSortedHpCanvas = CreateSpriteSortedHpCanvas(camera, hpBar);

            hpBar.DrawAmongSprites(spriteSortedHpCanvas, sprite.sortingLayerID, sprite.sortingOrder + 1);
        }
    }

    // Covers the screen like the battle UI canvas (same scaling, so the bars keep their size) but
    // is drawn at the sprites' depth, sorted among them.
    private static Canvas CreateSpriteSortedHpCanvas(Camera camera, HealthBarUI hpBar)
    {
        var root = new GameObject("FighterHpBars", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = Mathf.Abs(camera.transform.position.z);

        Canvas uiCanvas = hpBar.GetComponentInParent<Canvas>();
        if (uiCanvas != null && uiCanvas.rootCanvas.TryGetComponent(out CanvasScaler uiScaler))
        {
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = uiScaler.uiScaleMode;
            scaler.referenceResolution = uiScaler.referenceResolution;
            scaler.screenMatchMode = uiScaler.screenMatchMode;
            scaler.matchWidthOrHeight = uiScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = uiScaler.referencePixelsPerUnit;
            scaler.scaleFactor = uiScaler.scaleFactor;
        }

        return canvas;
    }

    // How far right the enemies may stand: up to the deck pile, which sits on the right edge.
    private float GetEnemySideOuterEdge(Camera camera)
    {
        const float margin = 0.5f;
        float screenEdge = BattleFormation.ScreenEdge(camera, 1f);
        if (deckUI == null || !(deckUI.transform is RectTransform deckRect))
            return screenEdge;

        var corners = new Vector3[4];
        deckRect.GetWorldCorners(corners);
        float deckLeft = UiPointToWorldX(deckRect, corners[0], camera);
        return Mathf.Min(screenEdge, deckLeft - margin);
    }

    // So the formation keeps HP bars wider than their fighter on screen too.
    private float GetHpBarWorldWidth(Character fighter, Camera camera)
    {
        if (!encounter.HpBars.TryGetValue(fighter, out HealthBarUI hpBar) || !(hpBar.transform is RectTransform barRect))
            return 0f;

        var corners = new Vector3[4];
        barRect.GetWorldCorners(corners);
        return Mathf.Abs(UiPointToWorldX(barRect, corners[2], camera) - UiPointToWorldX(barRect, corners[0], camera));
    }

    private static float UiPointToWorldX(RectTransform rect, Vector3 uiWorldPoint, Camera camera)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas = canvas.rootCanvas;

        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, uiWorldPoint);
        return camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -camera.transform.position.z)).x;
    }

    private static HealthBarUI FindHpBar(Character fighter)
    {
        foreach (HealthBarUI hpBar in FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hpBar.Target == fighter)
                return hpBar;
        }

        return null;
    }

    private void WatchFighter(EnemyCharacter fighter)
    {
        void OnHealthChanged(int current, int max)
        {
            if (current <= 0)
                StartCoroutine(PlayFallen(fighter));
        }

        void OnDeathPrevented(BattleStatus preventedBy)
        {
            HandleDeathPrevented(fighter, preventedBy);
        }

        fighter.HealthChanged += OnHealthChanged;
        fighter.DeathPrevented += OnDeathPrevented;
        unsubscribers.Add(() =>
        {
            fighter.HealthChanged -= OnHealthChanged;
            fighter.DeathPrevented -= OnDeathPrevented;
        });
    }

    // A fighter who falls while the battle goes on: out of reach of cards, HP bar and intent gone,
    // and the sprite fades away. The others then regroup.
    private IEnumerator PlayFallen(EnemyCharacter fighter)
    {
        if (!fallen.Add(fighter))
            yield break;

        foreach (Collider2D collider in fighter.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        if (encounter.HpBars.TryGetValue(fighter, out HealthBarUI hpBar))
            hpBar.gameObject.SetActive(false);
        if (encounter.IntentViews.TryGetValue(fighter, out EnemyIntentView intentView))
            intentView.gameObject.SetActive(false);

        if (fighter.TryGetComponent(out SpriteRenderer sprite))
        {
            Color color = sprite.color;
            float startAlpha = color.a;
            for (float elapsed = 0f; elapsed < defeatFadeDuration; elapsed += Time.deltaTime)
            {
                color.a = Mathf.Lerp(startAlpha, 0f, elapsed / defeatFadeDuration);
                sprite.color = color;
                yield return null;
            }

            color.a = 0f;
            sprite.color = color;
        }

        RequestRegroup();
    }

    // E.g. Seal of Immortality: announce it, and since the fighter may now fight differently
    // (a second phase), plan again what it will really do, unless it has already acted this round.
    private void HandleDeathPrevented(EnemyCharacter fighter, BattleStatus preventedBy)
    {
        if (preventedBy is DeathWardStatus ward && battleTurnUI != null && !string.IsNullOrWhiteSpace(ward.Announcement))
            StartCoroutine(battleTurnUI.PlayAnnouncement(ward.Announcement));

        if (turnManager == null || fighter.Brain == null)
            return;

        bool playerTurn = turnManager.CurrentTurn == TurnOwner.Player;
        if (!playerTurn && actedThisRound.Contains(fighter))
            return;

        fighter.Brain.PlanNextAction();
        if (playerTurn && encounter.IntentViews.TryGetValue(fighter, out EnemyIntentView intentView))
            intentView.RevealCurrentPlan();
    }

    private void RevealIntents()
    {
        foreach (KeyValuePair<EnemyCharacter, EnemyIntentView> intent in encounter.IntentViews)
        {
            if (intent.Key.IsAlive)
                intent.Value.RevealCurrentPlan();
        }
    }

    private void OnDestroy()
    {
        foreach (Action unsubscribe in unsubscribers)
            unsubscribe();
        unsubscribers.Clear();
    }

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager);
    }
}
