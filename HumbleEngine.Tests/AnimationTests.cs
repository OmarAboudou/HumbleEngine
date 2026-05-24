using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class AnimationTests
{
    // ── Tween<T> ───────────────────────────────────────────────────────────────

    [Test]
    public void Tween_InitialValue_IsCompleteAndEqualsInitial()
    {
        var tween = new Tween<float>(5f, Lerp.Float);
        Assert.That(tween.IsComplete, Is.True);
        Assert.That(tween.Value,      Is.EqualTo(5f));
    }

    [Test]
    public void Tween_To_StartsAnimation()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f);
        tween.To(10f);
        Assert.That(tween.IsComplete, Is.False);
        Assert.That(tween.Value,      Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void Tween_Advance_InterpolatesCorrectly()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f, easing: Easing.Linear);
        tween.To(10f);
        tween.Advance(0.5);
        Assert.That(tween.Value, Is.EqualTo(5f).Within(0.01f));
    }

    [Test]
    public void Tween_Advance_CompletesAtDuration()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f);
        tween.To(10f);
        tween.Advance(1.0);
        Assert.That(tween.IsComplete, Is.True);
        Assert.That(tween.Value,      Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void Tween_To_SameTarget_IsIdempotent()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f);
        tween.To(10f);
        tween.Advance(0.3);
        float valueBefore = tween.Value;
        tween.To(10f); // même cible — ne doit pas redémarrer
        Assert.That(tween.Value, Is.EqualTo(valueBefore).Within(0.001f));
    }

    [Test]
    public void Tween_To_InterruptsFromCurrentPosition()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f, easing: Easing.Linear);
        tween.To(10f);
        tween.Advance(0.5); // valeur courante ≈ 5
        float mid = tween.Value;
        tween.To(20f, duration: 1f);   // interrompt et part de ~5
        Assert.That(tween.Value, Is.EqualTo(mid).Within(0.01f)); // _from = ~5
        tween.Advance(1.0);
        Assert.That(tween.Value, Is.EqualTo(20f).Within(0.001f));
    }

    [Test]
    public void Tween_AdvanceBeyondDuration_ClampsAtOne()
    {
        var tween = new Tween<float>(0f, Lerp.Float, duration: 1f);
        tween.To(10f);
        tween.Advance(99.0);
        Assert.That(tween.IsComplete, Is.True);
        Assert.That(tween.Value,      Is.EqualTo(10f).Within(0.001f));
    }

    // ── Easing ────────────────────────────────────────────────────────────────

    [Test]
    public void Easing_Linear_Boundaries()
    {
        Assert.That(Easing.Linear(0f), Is.EqualTo(0f).Within(0.001f));
        Assert.That(Easing.Linear(1f), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Easing_EaseOut_StartsQuickAndSlows()
    {
        // À t=0.5, EaseOut doit être > 0.5 (avance vite en début)
        Assert.That(Easing.EaseOut(0.5f), Is.GreaterThan(0.5f));
        Assert.That(Easing.EaseOut(0f),   Is.EqualTo(0f).Within(0.001f));
        Assert.That(Easing.EaseOut(1f),   Is.EqualTo(1f).Within(0.001f));
    }

    // ── Lerp ──────────────────────────────────────────────────────────────────

    [Test]
    public void Lerp_Float_Midpoint()
    {
        Assert.That(Lerp.Float(0f, 10f, 0.5f), Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void Lerp_Color_Midpoint()
    {
        var a   = new Color(0, 0, 0, 255);
        var b   = new Color(100, 200, 50, 255);
        var mid = Lerp.Color(a, b, 0.5f);
        Assert.That(mid.R, Is.EqualTo(50).Within(1));
        Assert.That(mid.G, Is.EqualTo(100).Within(1));
        Assert.That(mid.B, Is.EqualTo(25).Within(1));
    }

    // ── ImplicitAnimation<T> ──────────────────────────────────────────────────

    [Test]
    public void ImplicitAnimation_InitialValue_Complete()
    {
        var prop = new Property<float>(5f);
        var anim = new ImplicitAnimation<float>(prop.AsReadOnly(), 1f, Lerp.Float);
        Assert.That(anim.IsComplete, Is.True);
        Assert.That(anim.Value,      Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void ImplicitAnimation_PropertyChange_StartsAnimation()
    {
        var prop = new Property<float>(0f);
        var anim = new ImplicitAnimation<float>(prop.AsReadOnly(), 1f, Lerp.Float, Easing.Linear);
        prop.Value = 10f;
        Assert.That(anim.IsComplete, Is.False);
        Assert.That(anim.Value,      Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void ImplicitAnimation_Advance_ReachesTarget()
    {
        var prop = new Property<float>(0f);
        var anim = new ImplicitAnimation<float>(prop.AsReadOnly(), 1f, Lerp.Float, Easing.Linear);
        prop.Value = 10f;
        anim.Advance(1.0);
        Assert.That(anim.Value,      Is.EqualTo(10f).Within(0.001f));
        Assert.That(anim.IsComplete, Is.True);
    }

    [Test]
    public void ImplicitAnimation_Interruption_StartsFromCurrentValue()
    {
        var prop = new Property<float>(0f);
        var anim = new ImplicitAnimation<float>(prop.AsReadOnly(), 1f, Lerp.Float, Easing.Linear);
        prop.Value = 10f;
        anim.Advance(0.5); // ≈ 5
        float mid = anim.Value;
        prop.Value = 20f; // interrompt depuis ~5
        Assert.That(anim.Value, Is.EqualTo(mid).Within(0.01f));
        anim.Advance(1.0);
        Assert.That(anim.Value, Is.EqualTo(20f).Within(0.001f));
    }

    // ── UINode.AdvanceAnimations ───────────────────────────────────────────────

    private sealed class AnimatedNode : UINode
    {
        public readonly Tween<float> Tween = new(0f, Lerp.Float, duration: 1f, easing: Easing.Linear);
        public int DirtyCount { get; private set; }

        public AnimatedNode() => AddAnimation(Tween);

        protected override RenderElement Render() => new TestLeaf();

        private record TestLeaf : RenderElement { }
    }

    [Test]
    public void UINode_AdvanceAnimations_MarksNodeDirty_WhenRunning()
    {
        var node = new AnimatedNode();
        node.GetElement(); // réinitialise dirty
        node.Tween.To(10f);
        node.AdvanceAnimations(0.1);
        Assert.That(node.IsDirty, Is.True);
    }

    [Test]
    public void UINode_AdvanceAnimations_DoesNotMarkDirty_WhenComplete()
    {
        var node = new AnimatedNode();
        node.GetElement();                  // dirty = false
        // Le tween est complet à la création — AdvanceAnimations ne doit pas marquer dirty
        node.AdvanceAnimations(0.1);
        Assert.That(node.IsDirty, Is.False);
    }

    [Test]
    public void UINode_AdvanceAnimations_StopsDirtyingAfterCompletion()
    {
        var node = new AnimatedNode();
        node.GetElement();
        node.Tween.To(10f);
        node.AdvanceAnimations(1.5); // dépasse la durée
        node.GetElement();           // remet dirty à false
        node.AdvanceAnimations(0.1); // animation terminée — ne doit plus dirtier
        Assert.That(node.IsDirty, Is.False);
    }
}
