using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared._WL.Addiction.Events;
using Content.Shared._WL.Addiction.Components;
using Content.Shared._WL.Addiction.EntitySystems;
using Content.Shared._WL.Addiction.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._WL.Addiction.EntitySystems;

/// <summary>
/// Серверная логика системы зависимостей.
/// </summary>
public sealed class AddictionSystem : SharedAddictionSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, BeforeIngestingDrinkEvent>(OnBeforeIngestDrink);
        SubscribeLocalEvent<AddictionComponent, CheckAddictionSatisfactionEvent>(OnCheckAddictionSatisfaction);
    }

    protected override void OnMapInit(EntityUid uid, AddictionComponent component, MapInitEvent args)
    {
        base.OnMapInit(uid, component, args);

        component.NextUpdateTime = _timing.CurTime;
        component.NextEmoteTime = _timing.CurTime;

        DirtyFields(uid, component, null,
            nameof(AddictionComponent.NextUpdateTime),
            nameof(AddictionComponent.NextEmoteTime));
}

    /// <summary>
    /// Проверяет, может ли данный реагент удовлетворить зависимость.
    /// </summary>
    private AddictionState GetAddictionState(AddictionComponent component, AddictionPrototype addictionProto)
    {
        var timeSinceLastSatisfied = _timing.CurTime - component.LastSatisfiedTime;

        if (timeSinceLastSatisfied >= addictionProto.WithdrawalTime)
            return AddictionState.Withdrawal;

        if (timeSinceLastSatisfied >= addictionProto.CravingTime)
            return AddictionState.Craving;

        return AddictionState.Satisfied;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AddictionComponent>();
        while (query.MoveNext(out var uid, out var addiction))
        {
            if (_timing.CurTime < addiction.NextUpdateTime)
                continue;

            if (!_prototype.TryIndex(addiction.AddictionType, out var addictionProto))
                continue;

            addiction.NextUpdateTime += addiction.UpdateRate;

            var newState = GetAddictionState(addiction, addictionProto);

            if (newState != addiction.CurrentState)
            {
                addiction.CurrentState = newState;
                DirtyField(uid, addiction, nameof(AddictionComponent.CurrentState));
            }

            if (addiction.CurrentState == AddictionState.Withdrawal &&
                _timing.CurTime >= addiction.NextEmoteTime)
            {
                _autoEmote.AddEmote(uid, addictionProto.WithdrawalEmote);
                addiction.NextEmoteTime = _timing.CurTime + addictionProto.WithdrawalEmoteInterval;
                DirtyField(uid, addiction, nameof(AddictionComponent.NextEmoteTime));
            }
        }
    }

    private void OnBeforeIngestDrink(EntityUid uid, AddictionComponent component, ref BeforeIngestingDrinkEvent args)
    {
        CheckSolutionForAddictionSatisfaction(uid, component, args.Solution, args.Solution.Volume.Float());
    }

    private void OnCheckAddictionSatisfaction(EntityUid uid, AddictionComponent component, ref CheckAddictionSatisfactionEvent args)
    {
        CheckSolutionForAddictionSatisfaction(uid, component, args.Solution, args.Amount);
    }

    private void CheckSolutionForAddictionSatisfaction(EntityUid uid, AddictionComponent component, Solution solution, float amount)
    {
        if (!_prototype.TryIndex(component.AddictionType, out var addictionProto))
            return;

        // Проверяем каждый реагент в раствоерер
        foreach (var reagentQuantity in solution.Contents)
        {
            if (!_prototype.TryIndex<ReagentPrototype>(reagentQuantity.Reagent.ToString(), out var reagentProto))
                continue;

            if (!CanSatisfyAddiction(component, reagentQuantity.Reagent))
                continue;

            // Считаем, сколько данного реагента попадет в организм.
            var reagentAmount = reagentQuantity.Quantity.Float() * (amount / solution.Volume.Float());
            if (reagentAmount >= addictionProto.MinimumSatisfyingAmount)
            {
                SatisfyAddiction(uid, component);

                var satisfiedEvent = new AddictionSatisfiedEvent(addictionProto.ID, uid);
                RaiseLocalEvent(uid, ref satisfiedEvent);
                break;
            }
        }
    }
}
