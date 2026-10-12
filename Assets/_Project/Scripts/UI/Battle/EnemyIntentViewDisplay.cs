using UnityEngine;

internal sealed class EnemyIntentDisplay
{
    private const string NoPlanPlaceholder = "\u2014";

    private readonly EnemyIntentPresenter presenter;

    public EnemyIntentDisplay(EnemyIntentPresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Refresh(EnemyBrain brain, bool keepVisualHidden)
    {
        if (!presenter.HasLabel)
            return;

        if (brain == null)
        {
            presenter.ShowEmpty();
            return;
        }

        string text = brain.CurrentPlan.HasAbility
            ? brain.CurrentPlan.GetIntentLabel()
            : NoPlanPlaceholder;

        // Who it will hit (plain ASCII: the label font has no arrow glyph).
        string targetName = brain.CurrentPlan.HasAbility ? brain.GetIntentTargetName() : null;
        if (!string.IsNullOrWhiteSpace(targetName))
            text += " -> " + targetName;

        Sprite icon = brain.CurrentPlan.Ability != null ? brain.CurrentPlan.Ability.icon : null;
        presenter.ShowIntent(text, icon, keepVisualHidden);
    }
}
