using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class BattleHpBarBaselineAligner : MonoBehaviour
{
    [SerializeField] private HealthBarUI playerHpBar;
    [SerializeField] private HealthBarUI enemyHpBar;
    [SerializeField] private EnemyCharacter enemy;

    [SerializeField] private float acceptableErrorPixels = 2f;
    [SerializeField] private float probeWorldUnits = 0.35f;

    private bool _done;
    private RectTransform _playerRt;
    private RectTransform _enemyRt;

    private void OnEnable()
    {
        _done = false;
        _playerRt = playerHpBar != null ? playerHpBar.GetComponent<RectTransform>() : null;
        _enemyRt = enemyHpBar != null ? enemyHpBar.GetComponent<RectTransform>() : null;
    }

    private void LateUpdate()
    {
        if (_done || playerHpBar == null || enemyHpBar == null || enemy == null)
            return;

        RectTransform playerRt = _playerRt;
        RectTransform enemyRt = _enemyRt;
        if (playerRt == null || enemyRt == null)
            return;

        float err = playerRt.anchoredPosition.y - enemyRt.anchoredPosition.y;
        if (Mathf.Abs(err) <= acceptableErrorPixels)
        {
            _done = true;
            return;
        }

        Transform t = enemy.transform;
        Vector3 baseLocal = t.localPosition;

        t.localPosition = baseLocal + Vector3.up * probeWorldUnits;
        enemyHpBar.SnapToSpriteWorldAnchor();
        float errProbe = playerRt.anchoredPosition.y - enemyRt.anchoredPosition.y;

        t.localPosition = baseLocal;
        enemyHpBar.SnapToSpriteWorldAnchor();

        float slope = (errProbe - err) / Mathf.Max(1e-5f, probeWorldUnits);
        if (Mathf.Abs(slope) < 1e-4f)
        {
            _done = true;
            return;
        }

        float correction = -err / slope;
        t.localPosition = baseLocal + Vector3.up * correction;
        enemyHpBar.SnapToSpriteWorldAnchor();

        _done = true;
    }
}
