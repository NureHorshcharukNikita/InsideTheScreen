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

        Sprite icon = brain.CurrentPlan.Ability != null ? brain.CurrentPlan.Ability.icon : null;
        presenter.ShowIntent(text, icon, keepVisualHidden);
    }
}
