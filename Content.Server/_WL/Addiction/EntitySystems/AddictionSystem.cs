using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Nutrition.Events;
using Content.Shared._WL.Addiction.Events;
using Content.Shared._WL.Addiction.Components;
using Content.Shared._WL.Addiction.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
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
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, BeforeIngestDrinkEvent>(OnBeforeIngestDrink);
        SubscribeLocalEvent<AddictionComponent, CheckAddictionSatisfactionEvent>(OnCheckAddictionSatisfaction);
    }

    protected override void OnMapInit(EntityUid uid, AddictionComponent component, MapInitEvent args)
    {
        base.OnMapInit(uid, component, args);

        component.NextUpdateTime = _timing.CurTime + component.UpdateRate;
        component.NextEmoteTime = _timing.CurTime;

        DirtyFields(uid, component, null,
            nameof(AddictionComponent.NextUpdateTime),
            nameof(AddictionComponent.NextEmoteTime));
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

            addiction.NextUpdateTime = _timing.CurTime + addiction.UpdateRate;
            DirtyField(uid, addiction, nameof(AddictionComponent.NextUpdateTime));

            var oldState = addiction.CurrentState;
            UpdateAddictionState(uid, addiction);

            if (addiction.CurrentState == AddictionState.Withdrawal &&
                _timing.CurTime >= addiction.NextEmoteTime)
            {
                _autoEmote.AddEmote(uid, addictionProto.WithdrawalEmote);
                addiction.NextEmoteTime = _timing.CurTime + addictionProto.WithdrawalEmoteInterval;
                DirtyField(uid, addiction, nameof(AddictionComponent.NextEmoteTime));
            }
        }
    }

    private void OnBeforeIngestDrink(EntityUid uid, AddictionComponent component, ref BeforeIngestDrinkEvent args)
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

        // Проверяем каждый реагент в растворе
        foreach (var reagentQuantity in solution.Contents)
        {
            if (!_prototype.TryIndex<ReagentPrototype>(reagentQuantity.Reagent.ToString(), out var reagentProto))
                continue;

            if (!CanSatisfyAddiction(component, reagentQuantity.Reagent))
                continue;

            var reagentAmount = reagentQuantity.Quantity.Float() * (amount / solution.Volume.Float());

            if (reagentAmount >= addictionProto.MinimumSatisfyingAmount)
            {
                SatisfyAddiction(uid, component);

                var satisfiedEvent = new AddictionSatisfiedEvent(addictionProto.ID, uid);
                RaiseLocalEvent(uid, ref satisfiedEvent);

                if (TryComp(uid, out MovementSpeedModifierComponent? movementComponent))
                {
                    _movement.RefreshMovementSpeedModifiers(uid, movementComponent);
                }

                break;
            }
        }
    }
}
